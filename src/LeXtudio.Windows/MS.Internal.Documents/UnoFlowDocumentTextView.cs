#if HAS_UNO
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using MS.Internal.Florence;

namespace MS.Internal.Documents;

/// <summary>
/// ITextView implementation for Uno Platform, backed by FlorenceLayoutEngine output.
/// The WPF TextEditor uses this interface for hit-testing (mouse click → text position),
/// caret geometry (text position → pixel rect), and line navigation (arrow keys).
/// </summary>
internal sealed class UnoFlowDocumentTextView : ITextView
{
    private readonly FlowDocumentView _owner;
    private bool _isValid;

    internal UnoFlowDocumentTextView(FlowDocumentView owner)
    {
        _owner = owner;
    }

    internal void OnLayoutUpdated()
    {
        _isValid = true;
        Updated?.Invoke(this, EventArgs.Empty);
    }

    // ── ITextView properties ────────────────────────────────────────────────

    UIElement ITextView.RenderScope => _owner;

    ITextContainer ITextView.TextContainer
        => _owner.Document?.StructuralCache.TextContainer!;

    bool ITextView.IsValid => _isValid;

    bool ITextView.RendersOwnSelection => false;

    ReadOnlyCollection<TextSegment> ITextView.TextSegments
        => new(new List<TextSegment> { TextSegment.Null });

    // ── Hit-testing ─────────────────────────────────────────────────────────

    ITextPointer ITextView.GetTextPositionFromPoint(Point point, bool snapToText)
    {
        var tc = _owner.Document?.StructuralCache.TextContainer;
        if (tc == null || _owner.Page == null)
            return tc?.Start ?? NullStart();

        var lines = _owner.Page.Lines;
        if (lines.Count == 0)
            return tc.Start;

        // Find the line whose Y band contains the point.
        FlorenceLine? hit = null;
        foreach (var line in lines)
        {
            if (point.Y >= line.Y && point.Y < line.Y + line.Height)
            {
                hit = line;
                break;
            }
        }
        hit ??= snapToText ? lines[^1] : null;
        if (hit == null)
            return tc.Start;

        // Within the line, find the character offset from the X position.
        int raw = HitTestX(hit, point.X);
        int charOffset = Math.Clamp(raw, 0, tc.IMECharCount);
        System.IO.File.AppendAllText(
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "rtb-template.log"),
            $"{DateTime.Now:HH:mm:ss.fff}  [HitTest] point=({point.X:F1},{point.Y:F1}) " +
            $"hit.Y={hit.Y:F1} hit.H={hit.Height:F1} hit.Start={hit.StartOffset} hit.End={hit.EndOffset} " +
            $"raw={raw} IMECharCount={tc.IMECharCount} clamped={charOffset}\n");
        return tc.CreatePointerAtCharOffset(charOffset, LogicalDirection.Forward);
    }

    Rect ITextView.GetRectangleFromTextPosition(ITextPointer position)
    {
        if (_owner.Page == null) return Rect.Empty;
        int offset = position.CharOffset;
        var line = FindLineForOffset(offset);
        if (line == null) return Rect.Empty;

        double x = GetXForOffset(line, offset);
        double lineH = line.Height > 0 ? line.Height : 14;
        return new Rect(x, line.Y, 1, lineH);
    }

    Rect ITextView.GetRawRectangleFromTextPosition(ITextPointer position, out Transform transform)
    {
        transform = new MatrixTransform();
        return ((ITextView)this).GetRectangleFromTextPosition(position);
    }

    Geometry ITextView.GetTightBoundingGeometryFromTextPositions(ITextPointer startPosition, ITextPointer endPosition)
        => Geometry.Empty;

    // ── Line navigation ─────────────────────────────────────────────────────

    ITextPointer ITextView.GetPositionAtNextLine(ITextPointer position, double suggestedX,
        int count, out double newSuggestedX, out int linesMoved)
    {
        newSuggestedX = suggestedX;
        linesMoved = 0;

        var tc = _owner.Document?.StructuralCache.TextContainer;
        if (tc == null || _owner.Page == null)
            return position;

        var lines = _owner.Page.Lines;
        int currentOffset = position.CharOffset;
        int currentIndex = FindLineIndexForOffset(currentOffset);
        int targetIndex = currentIndex + count;
        targetIndex = Math.Clamp(targetIndex, 0, lines.Count - 1);
        linesMoved = targetIndex - currentIndex;

        var targetLine = lines[targetIndex];
        int newOffset = Math.Clamp(HitTestX(targetLine, suggestedX), 0, tc.IMECharCount);
        newSuggestedX = GetXForOffset(targetLine, newOffset);
        return tc.CreatePointerAtCharOffset(newOffset, LogicalDirection.Forward);
    }

    ITextPointer ITextView.GetPositionAtNextPage(ITextPointer position, Point suggestedOffset,
        int count, out Point newSuggestedOffset, out int pagesMoved)
    {
        newSuggestedOffset = suggestedOffset;
        pagesMoved = 0;
        return position;
    }

    // ── Caret unit navigation ───────────────────────────────────────────────

    bool ITextView.IsAtCaretUnitBoundary(ITextPointer position) => true;

    ITextPointer ITextView.GetNextCaretUnitPosition(ITextPointer position, LogicalDirection direction)
    {
        var tc = _owner.Document?.StructuralCache.TextContainer;
        if (tc == null) return position;
        int offset = position.CharOffset;
        int newOffset = direction == LogicalDirection.Forward
            ? Math.Min(offset + 1, tc.IMECharCount)
            : Math.Max(offset - 1, 0);
        return tc.CreatePointerAtCharOffset(newOffset, direction);
    }

    ITextPointer ITextView.GetBackspaceCaretUnitPosition(ITextPointer position)
    {
        var tc = _owner.Document?.StructuralCache.TextContainer;
        if (tc == null) return position;
        int newOffset = Math.Max(position.CharOffset - 1, 0);
        return tc.CreatePointerAtCharOffset(newOffset, LogicalDirection.Backward);
    }

    TextSegment ITextView.GetLineRange(ITextPointer position)
    {
        if (_owner.Page == null) return TextSegment.Null;
        var line = FindLineForOffset(position.CharOffset);
        if (line == null) return TextSegment.Null;
        var tc = _owner.Document?.StructuralCache.TextContainer;
        if (tc == null) return TextSegment.Null;
        var start = tc.CreatePointerAtCharOffset(line.StartOffset, LogicalDirection.Forward);
        var end   = tc.CreatePointerAtCharOffset(line.EndOffset,   LogicalDirection.Backward);
        return new TextSegment(start, end);
    }

    ReadOnlyCollection<GlyphRun> ITextView.GetGlyphRuns(ITextPointer start, ITextPointer end)
        => new(new List<GlyphRun>());

    bool ITextView.Contains(ITextPointer position) => true;

    // ── Async Bring-Into-View (fire-and-forget) ─────────────────────────────

    void ITextView.BringPositionIntoViewAsync(ITextPointer position, object userState)
        => BringPositionIntoViewCompleted?.Invoke(this,
            new BringPositionIntoViewCompletedEventArgs(position, true, null, false, userState));

    void ITextView.BringPointIntoViewAsync(Point point, object userState)
        => BringPointIntoViewCompleted?.Invoke(this,
            new BringPointIntoViewCompletedEventArgs(point, null!, true, null, false, userState));

    void ITextView.BringLineIntoViewAsync(ITextPointer position, double suggestedX, int count, object userState)
        => BringLineIntoViewCompleted?.Invoke(this,
            new BringLineIntoViewCompletedEventArgs(position, suggestedX, count,
                position, suggestedX, 0, true, null, false, userState));

    void ITextView.BringPageIntoViewAsync(ITextPointer position, Point suggestedOffset, int count, object userState)
        => BringPageIntoViewCompleted?.Invoke(this,
            new BringPageIntoViewCompletedEventArgs(position, suggestedOffset, count,
                position, suggestedOffset, 0, true, null, false, userState));

    void ITextView.CancelAsync(object userState) { }

    bool ITextView.Validate() { _isValid = true; return true; }
    bool ITextView.Validate(Point point) { _isValid = true; return true; }
    bool ITextView.Validate(ITextPointer position) { _isValid = true; return true; }

    void ITextView.ThrottleBackgroundTasksForUserInput() { }

    // ── Events ──────────────────────────────────────────────────────────────

    public event BringPositionIntoViewCompletedEventHandler? BringPositionIntoViewCompleted;
    public event BringPointIntoViewCompletedEventHandler?    BringPointIntoViewCompleted;
    public event BringLineIntoViewCompletedEventHandler?     BringLineIntoViewCompleted;
    public event BringPageIntoViewCompletedEventHandler?     BringPageIntoViewCompleted;
    public event EventHandler? Updated;

    // ── Private helpers ─────────────────────────────────────────────────────

    private FlorenceLine? FindLineForOffset(int offset)
    {
        if (_owner.Page == null) return null;
        foreach (var line in _owner.Page.Lines)
            if (offset >= line.StartOffset && offset <= line.EndOffset)
                return line;
        return _owner.Page.Lines.Count > 0 ? _owner.Page.Lines[^1] : null;
    }

    private int FindLineIndexForOffset(int offset)
    {
        if (_owner.Page == null) return 0;
        var lines = _owner.Page.Lines;
        for (int i = 0; i < lines.Count; i++)
            if (offset >= lines[i].StartOffset && offset <= lines[i].EndOffset)
                return i;
        return lines.Count - 1;
    }

    private static int HitTestX(FlorenceLine line, double x)
    {
        if (line.Runs.Count == 0) return line.StartOffset;
        foreach (var run in line.Runs)
        {
            if (x >= run.X && x < run.X + run.Width)
            {
                // Proportional split within the run.
                double fraction = (x - run.X) / Math.Max(run.Width, 1);
                int charInRun = (int)Math.Round(fraction * run.Length);
                charInRun = Math.Clamp(charInRun, 0, run.Length);
                return run.StartOffset + charInRun;
            }
        }
        // Past the end of the line.
        var last = line.Runs[^1];
        return last.EndOffset;
    }

    private static double GetXForOffset(FlorenceLine line, int offset)
    {
        foreach (var run in line.Runs)
        {
            if (offset >= run.StartOffset && offset <= run.EndOffset)
            {
                int charInRun = offset - run.StartOffset;
                return run.X + (run.Length > 0 ? run.CharWidth * charInRun : 0);
            }
        }
        return line.Runs.Count > 0 ? line.Runs[^1].X + line.Runs[^1].Width : 0;
    }

    private static ITextPointer NullStart()
        => new MS.Internal.Florence.FlorenceTextPointer(
            new MS.Internal.Florence.FlorenceTextContainer(null!),
            LogicalDirection.Forward);
}
#endif
