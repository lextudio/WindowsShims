#if HAS_UNO
using System.Windows.Documents;
using MS.Internal.Florence;

namespace MS.Internal.Documents;

internal class FlowDocumentView : Microsoft.UI.Xaml.Controls.Panel, IServiceProvider
{
    private FlowDocument? _document;
    private FlorencePage? _page;
    private FlorencePage? _arrangedPage;
    private UnoFlowDocumentTextView? _textView;
    private double _lastMeasureWidth = -1;
    private double _lastMeasureHeight = -1;
    private readonly List<Microsoft.UI.Xaml.Shapes.Rectangle> _selectionRects = [];
    private readonly List<Microsoft.UI.Xaml.Controls.TextBlock> _lineBlocks = [];

    // Caret overlay. The visual lives here, but hit-testing and geometry come
    // from the WPF-facing ITextView adapter.
    private readonly Microsoft.UI.Xaml.Shapes.Rectangle _caret;
    private DispatcherTimer? _blinkTimer;
    private bool _caretVisible;
    private Rect _caretRect = Rect.Empty;

    internal FlowDocumentView()
    {
        _caret = new Microsoft.UI.Xaml.Shapes.Rectangle
        {
            Width = 1,
            Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black),
            Visibility = Microsoft.UI.Xaml.Visibility.Collapsed,
            IsHitTestVisible = false,
        };

        _blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(530) };
        _blinkTimer.Tick += (_, _) =>
        {
            _caretVisible = !_caretVisible;
            _caret.Opacity = _caretVisible ? 1 : 0;
        };
    }

    // ── Document ────────────────────────────────────────────────────────────

    internal FlowDocument? Document
    {
        get => _document;
        set
        {
            _document = value;
            _page = null;
            _arrangedPage = null;
            _lastMeasureWidth = -1;
            _lastMeasureHeight = -1;
            _textView = null;
            InvalidateMeasure();
        }
    }

    internal bool OverridesDefaultStyle { get; set; }

    // ── IServiceProvider ────────────────────────────────────────────────────

    object IServiceProvider.GetService(Type serviceType)
    {
        if (serviceType == typeof(ITextView))
            return _textView ??= new UnoFlowDocumentTextView(this);
        if (serviceType == typeof(ITextContainer))
            return _document?.StructuralCache.TextContainer;
        return null!;
    }

    internal UnoFlowDocumentTextView? TextView => _textView;
    internal FlorencePage? Page => _page;

    // ── Measure / Arrange ───────────────────────────────────────────────────

    protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
    {
        if (_document == null)
            return new Windows.Foundation.Size(0, 0);

        double w = double.IsInfinity(availableSize.Width) ? 600 : availableSize.Width;
        double h = availableSize.Height;

        if (_page == null || Math.Abs(w - _lastMeasureWidth) > 0.5 || Math.Abs(h - _lastMeasureHeight) > 0.5)
        {
            _page = FlorenceLayoutEngine.Format(_document, new Windows.Foundation.Size(w, h));
            _lastMeasureWidth = w;
            _lastMeasureHeight = h;
            _textView?.OnLayoutUpdated();
        }

        double totalH = _page.Lines.Count > 0
            ? _page.Lines[^1].Y + _page.Lines[^1].Height
            : 0;

        return new Windows.Foundation.Size(
            Math.Min(w, double.IsInfinity(availableSize.Width) ? w : availableSize.Width),
            Math.Min(totalH, double.IsInfinity(availableSize.Height) ? totalH : availableSize.Height));
    }

    protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
    {
        if (_page == null)
            return finalSize;

        if (!ReferenceEquals(_page, _arrangedPage))
        {
            RebuildLineBlocks();
            _arrangedPage = _page;
        }

        RefreshSelection();

        var lines = _page.Lines;
        for (int i = 0; i < _lineBlocks.Count && i < lines.Count; i++)
            _lineBlocks[i].Arrange(new Windows.Foundation.Rect(0, lines[i].Y, finalSize.Width, lines[i].Height));

        foreach (var rect in _selectionRects)
        {
            if (rect.Tag is Rect selectionRect)
            {
                rect.Arrange(new Windows.Foundation.Rect(selectionRect.X, selectionRect.Y, selectionRect.Width, selectionRect.Height));
            }
        }

        if (!_caretRect.IsEmpty)
        {
            double h = _caretRect.Height > 0 ? _caretRect.Height : 14;
            _caret.Arrange(new Windows.Foundation.Rect(_caretRect.X, _caretRect.Y, 1, h));
        }
        else
        {
            _caret.Arrange(new Windows.Foundation.Rect(-2, 0, 1, 0));
        }

        return finalSize;
    }

    // ── Caret ───────────────────────────────────────────────────────────────

    internal void SetCaretAt(Windows.Foundation.Point clickPoint)
    {
        var textView = _textView ??= new UnoFlowDocumentTextView(this);
        var position = textView.GetTextPositionFromPoint(clickPoint, snapToText: true);
        SetCaretAt(position);
    }

    internal void SetCaretAt(ITextPointer position)
    {
        var textView = _textView ??= new UnoFlowDocumentTextView(this);
        position = textView.NormalizeToVisiblePosition(position);
        var rect = textView.GetRectangleFromTextPosition(position);
        if (rect.IsEmpty)
            return;

        _caretRect = rect;
        _caret.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        _caretVisible = true;
        _caret.Opacity = 1;
        _blinkTimer?.Start();
        InvalidateArrange();
    }

    internal void RefreshSelection()
    {
        foreach (var rect in _selectionRects)
            Children.Remove(rect);
        _selectionRects.Clear();

        if (_page == null || _document?.StructuralCache?.TextContainer?.TextSelection is not ITextSelection selection)
        {
            EnsureCaretVisualOnTop();
            return;
        }

        int start = Math.Min(selection.Start.CharOffset, selection.End.CharOffset);
        int end = Math.Max(selection.Start.CharOffset, selection.End.CharOffset);
        if (start == end)
        {
            EnsureCaretVisualOnTop();
            return;
        }

        foreach (var line in _page.Lines)
        {
            int segmentStart = Math.Max(start, line.StartOffset);
            int segmentEnd = Math.Min(end, line.EndOffset);
            if (segmentStart >= segmentEnd)
                continue;

            double x1 = UnoFlowDocumentTextView.GetPixelXForOffset(line, segmentStart);
            double x2 = UnoFlowDocumentTextView.GetPixelXForOffset(line, segmentEnd);
            double height = line.Height > 0 ? line.Height : 14;

            var rect = new Microsoft.UI.Xaml.Shapes.Rectangle
            {
                Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DodgerBlue),
                Opacity = 0.35,
                IsHitTestVisible = false,
                Tag = new Rect(x1, line.Y, Math.Max(1, x2 - x1), height),
            };

            _selectionRects.Add(rect);
            Children.Add(rect);
        }

        ReorderVisuals();
        InvalidateArrange();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private void RebuildLineBlocks()
    {
        foreach (var block in _lineBlocks)
            Children.Remove(block);
        _lineBlocks.Clear();

        if (_page == null)
        {
            ReorderVisuals();
            return;
        }

        foreach (var line in _page.Lines)
        {
            var block = BuildLineTextBlock(line);
            _lineBlocks.Add(block);
            Children.Add(block);
        }

        ReorderVisuals();
    }

    private void EnsureCaretVisualOnTop()
    {
        ReorderVisuals();
        InvalidateArrange();
    }

    private void ReorderVisuals()
    {
        Children.Clear();
        foreach (var rect in _selectionRects)
            Children.Add(rect);
        foreach (var block in _lineBlocks)
            Children.Add(block);
        Children.Add(_caret);
    }

    private static Microsoft.UI.Xaml.Controls.TextBlock BuildLineTextBlock(FlorenceLine line)
    {
        var tb = new Microsoft.UI.Xaml.Controls.TextBlock
        {
            TextWrapping = Microsoft.UI.Xaml.TextWrapping.NoWrap
        };

        if (line.Runs.Count == 0)
        {
            tb.Text = line.FullText;
            return tb;
        }

        var first = line.Runs[0];
        if (first.FontSize > 0) tb.FontSize = first.FontSize;
        if (first.FontFamily is not null) tb.FontFamily = first.FontFamily;

        foreach (var run in line.Runs)
        {
            var inlineRun = new Microsoft.UI.Xaml.Documents.Run { Text = run.Text };
            if (run.Bold)   inlineRun.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
            if (run.Italic) inlineRun.FontStyle  = Windows.UI.Text.FontStyle.Italic;
            // Per-run FontFamily mirrors WPF PTS TextRunProperties.Typeface.FontFamily
            // so mixed-script content (e.g. CJK + Latin) renders each Run with its
            // intended typeface — matching what UnoFlowDocumentTextView measures
            // when computing caret X.
            if (run.FontFamily is not null) inlineRun.FontFamily = run.FontFamily;
            tb.Inlines.Add(inlineRun);
        }
        return tb;
    }
}
#endif
