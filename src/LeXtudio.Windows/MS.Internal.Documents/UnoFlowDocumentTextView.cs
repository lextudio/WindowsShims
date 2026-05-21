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

    private static readonly string _logPath =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "rtb-template.log");

    private static void Log(string msg)
    {
        try { System.IO.File.AppendAllText(_logPath,
            $"{DateTime.Now:HH:mm:ss.fff}  [TextView] {msg}\n"); } catch { }
    }

    internal UnoFlowDocumentTextView(FlowDocumentView owner)
    {
        _owner = owner;
    }

    internal void OnLayoutUpdated()
    {
        _isValid = true;
        if (_owner.Page != null)
        {
            var tc = _owner.Document?.StructuralCache.TextContainer;
            Log($"[Layout] {_owner.Page.Lines.Count} lines, IMECharCount={tc?.IMECharCount}");
            foreach (var ln in _owner.Page.Lines)
                Log($"[Layout]   line[{ln.StartOffset}..{ln.EndOffset}] y={ln.Y:F1} runs={ln.Runs.Count} text='{ln.FullText}'");
        }
        Updated?.Invoke(this, EventArgs.Empty);
        // After a reflow (e.g. window resize), the Uno caret Rectangle has stale pixel
        // coordinates. Recompute from the current selection so the caret tracks the text.
        RefreshCaretAfterLayout();
    }

    private void RefreshCaretAfterLayout()
    {
        try
        {
            var tc = _owner.Document?.StructuralCache.TextContainer;
            var position = tc?.TextSelection?.MovingPosition;
            if (position == null) return;

            // Clamp to last visible offset (same rule as UpdateCaretFromSelection).
            if (_owner.Page != null && _owner.Page.Lines.Count > 0)
            {
                int lastOffset = _owner.Page.Lines[^1].EndOffset;
                if (position.CharOffset > lastOffset)
                    position = tc!.CreatePointerAtCharOffset(lastOffset, LogicalDirection.Backward);
            }

            _owner.SetCaretAt(position);
            Log($"[Layout] caret refreshed to offset={position.CharOffset} dir={position.LogicalDirection}");
        }
        catch (Exception ex)
        {
            Log($"[Layout] RefreshCaretAfterLayout THREW {ex.GetType().Name}: {ex.Message}");
        }
    }

    internal ITextPointer GetTextPositionFromPoint(Windows.Foundation.Point point, bool snapToText)
        => ((ITextView)this).GetTextPositionFromPoint(new Point(point.X, point.Y), snapToText);

    internal Rect GetRectangleFromTextPosition(ITextPointer position)
        => ((ITextView)this).GetRectangleFromTextPosition(position);

    internal ITextPointer NormalizeToVisiblePosition(ITextPointer position)
    {
        var tc = _owner.Document?.StructuralCache.TextContainer;
        if (tc == null || _owner.Page == null || _owner.Page.Lines.Count == 0)
            return position;

        int lastOffset = _owner.Page.Lines[^1].EndOffset;
        if (position.CharOffset <= lastOffset)
            return position;

        return tc.CreatePointerAtCharOffset(lastOffset, LogicalDirection.Backward);
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

        int raw = HitTestCharOffset(hit, point.X);
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
        position = NormalizeToVisiblePosition(position);
        if (_owner.Page == null) return Rect.Empty;
        int offset = position.CharOffset;
        var line = FindLineForPosition(position);
        if (line == null)
        {
            Log($"[GetRect] offset={offset} dir={position.LogicalDirection} → no line found → Empty");
            return Rect.Empty;
        }

        double x = GetPixelXForOffset(line, offset);
        double lineH = line.Height > 0 ? line.Height : 14;
        var rect = new Rect(x, line.Y, 1, lineH);
        Log($"[GetRect] offset={offset} dir={position.LogicalDirection} → line[{line.StartOffset}..{line.EndOffset}] x={x:F1} y={line.Y:F1}");
        return rect;
    }

    Rect ITextView.GetRawRectangleFromTextPosition(ITextPointer position, out Transform transform)
    {
        transform = new MatrixTransform();
        return ((ITextView)this).GetRectangleFromTextPosition(position);
    }

    Geometry ITextView.GetTightBoundingGeometryFromTextPositions(ITextPointer startPosition, ITextPointer endPosition)
    {
        if (_owner.Page == null)
            return Geometry.Empty;

        int start = Math.Min(startPosition.CharOffset, endPosition.CharOffset);
        int end = Math.Max(startPosition.CharOffset, endPosition.CharOffset);
        if (start == end)
            return Geometry.Empty;

        var geometry = new GeometryGroup();
        foreach (var line in _owner.Page.Lines)
        {
            int lineStart = line.StartOffset;
            int lineEnd = line.EndOffset;
            int segmentStart = Math.Max(start, lineStart);
            int segmentEnd = Math.Min(end, lineEnd);
            if (segmentStart >= segmentEnd)
                continue;

            double x1 = GetPixelXForOffset(line, segmentStart);
            double x2 = GetPixelXForOffset(line, segmentEnd);
            double height = line.Height > 0 ? line.Height : 14;
            geometry.Children.Add(new RectangleGeometry
            {
                Rect = new Rect(x1, line.Y, Math.Max(1, x2 - x1), height)
            });
        }

        return geometry.Children.Count == 0 ? Geometry.Empty : geometry;
    }

    // ── Line navigation ─────────────────────────────────────────────────────

    ITextPointer ITextView.GetPositionAtNextLine(ITextPointer position, double suggestedX,
        int count, out double newSuggestedX, out int linesMoved)
    {
        newSuggestedX = suggestedX;
        linesMoved = 0;
        position = NormalizeToVisiblePosition(position);

        var tc = _owner.Document?.StructuralCache.TextContainer;
        if (tc == null || _owner.Page == null)
            return position;

        var lines = _owner.Page.Lines;
        int currentIndex = FindLineIndexForPosition(position);
        int targetIndex = currentIndex + count;
        targetIndex = Math.Clamp(targetIndex, 0, lines.Count - 1);
        linesMoved = targetIndex - currentIndex;

        var targetLine = lines[targetIndex];
        int newOffset = Math.Clamp(HitTestCharOffset(targetLine, suggestedX), 0, tc.IMECharCount);
        newSuggestedX = GetPixelXForOffset(targetLine, newOffset);
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
        position = NormalizeToVisiblePosition(position);
        int offset = position.CharOffset;
        int newOffset;

        if (direction == LogicalDirection.Forward)
        {
            int maxOffset = _owner.Page != null && _owner.Page.Lines.Count > 0
                ? _owner.Page.Lines[^1].EndOffset
                : tc.IMECharCount;
            newOffset = Math.Min(offset + 1, maxOffset);
        }
        else
        {
            newOffset = Math.Max(offset - 1, 0);
        }
        Log($"[GetNextCaretUnit] offset={offset} dir={direction} → {newOffset} (IMECharCount={tc.IMECharCount})");
        return tc.CreatePointerAtCharOffset(newOffset, direction);
    }

    ITextPointer ITextView.GetBackspaceCaretUnitPosition(ITextPointer position)
    {
        var tc = _owner.Document?.StructuralCache.TextContainer;
        if (tc == null) return position;
        position = NormalizeToVisiblePosition(position);
        int newOffset = Math.Max(position.CharOffset - 1, 0);
        return tc.CreatePointerAtCharOffset(newOffset, LogicalDirection.Backward);
    }

    TextSegment ITextView.GetLineRange(ITextPointer position)
    {
        position = NormalizeToVisiblePosition(position);
        if (_owner.Page == null) return TextSegment.Null;
        var line = FindLineForPosition(position);
        if (line == null) return TextSegment.Null;
        var tc = _owner.Document?.StructuralCache.TextContainer;
        if (tc == null) return TextSegment.Null;
        var start = tc.CreatePointerAtCharOffset(line.StartOffset, LogicalDirection.Forward);
        var end   = tc.CreatePointerAtCharOffset(line.EndOffset,   LogicalDirection.Backward);
        Log($"[GetLineRange] query offset={position.CharOffset} dir={position.LogicalDirection} → line[{line.StartOffset}..{line.EndOffset}]");
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

    private FlorenceLine? FindLineForPosition(ITextPointer position)
    {
        if (_owner.Page == null) return null;
        int index = FindLineIndexForPosition(position);
        return index >= 0 && index < _owner.Page.Lines.Count ? _owner.Page.Lines[index] : null;
    }

    private int FindLineIndexForPosition(ITextPointer position)
    {
        if (_owner.Page == null) return 0;

        position = NormalizeToVisiblePosition(position);
        int offset = position.CharOffset;
        var lines = _owner.Page.Lines;
        bool preferNextLineAtSharedBoundary = position.LogicalDirection == LogicalDirection.Forward;

        for (int i = 0; i < lines.Count; i++)
        {
            bool sharedBoundaryWithNext = i + 1 < lines.Count
                && lines[i].EndOffset == offset
                && lines[i + 1].StartOffset == offset;

            if (preferNextLineAtSharedBoundary && sharedBoundaryWithNext)
                continue;

            if (offset >= lines[i].StartOffset && offset <= lines[i].EndOffset)
                return i;
        }

        if (preferNextLineAtSharedBoundary)
        {
            for (int i = 0; i < lines.Count; i++)
                if (lines[i].StartOffset == offset)
                    return i;
        }

        return lines.Count - 1;
    }

    private static int HitTestCharOffset(FlorenceLine line, double x)
    {
        if (line.Runs.Count == 0) return line.StartOffset;
        foreach (var run in line.Runs)
        {
            if (x >= run.X && x < run.X + run.Width)
            {
                // Binary search across the run's text using actual TextBlock-measured
                // prefix widths — keeps caret on variable-width / mixed-script text
                // (e.g. CJK + Latin) aligned with what BuildLineTextBlock renders.
                double target = x - run.X;
                int lo = 0;
                int hi = run.Length;
                while (lo < hi)
                {
                    int mid = (lo + hi + 1) / 2;
                    double w = MeasurePrefixWidth(run, mid);
                    if (w <= target) lo = mid;
                    else hi = mid - 1;
                }
                if (lo < run.Length)
                {
                    double left  = MeasurePrefixWidth(run, lo);
                    double right = MeasurePrefixWidth(run, lo + 1);
                    if (Math.Abs(target - right) < Math.Abs(target - left))
                        lo += 1;
                }
                return run.StartOffset + Math.Clamp(lo, 0, run.Length);
            }
        }
        // Past the end of the line.
        var last = line.Runs[^1];
        return last.EndOffset;
    }

    internal static double GetPixelXForOffset(FlorenceLine line, int offset)
    {
        foreach (var run in line.Runs)
        {
            if (offset >= run.StartOffset && offset <= run.EndOffset)
            {
                int charInRun = offset - run.StartOffset;
                if (run.Length <= 0 || charInRun <= 0)
                    return run.X;
                if (charInRun >= run.Length)
                    return run.X + run.Width;
                // Use the same TextBlock measurement engine the renderer uses
                // (see FlowDocumentView.BuildLineTextBlock) so the caret's X
                // matches the rendered pixel position of that character,
                // even with proportional fonts and font fallback.
                return run.X + MeasurePrefixWidth(run, charInRun);
            }
        }
        return line.Runs.Count > 0 ? line.Runs[^1].X + line.Runs[^1].Width : 0;
    }

    // Delegate to the same TextMeasurer that Florence used during layout, so caret X
    // is calculated with the same sentinel-corrected widths used to place runs.
    // This is critical for trailing whitespace: TextBlock.DesiredSize.Width strips
    // trailing spaces, but TextMeasurer.MeasureWidth compensates via a sentinel char.
    private static double MeasurePrefixWidth(FlorenceRun run, int charCount)
        => MS.Internal.Florence.TextMeasurer.MeasurePrefixWidth(run, charCount);

    private static ITextPointer NullStart()
        => new MS.Internal.Florence.FlorenceTextPointer(
            new MS.Internal.Florence.FlorenceTextContainer(null!),
            LogicalDirection.Forward);
}
#endif
