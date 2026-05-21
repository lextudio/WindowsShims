#if HAS_UNO
using System.Windows.Documents;
using MS.Internal.Florence;

namespace MS.Internal.Documents;

internal class FlowDocumentView : Microsoft.UI.Xaml.Controls.Panel, IServiceProvider, IUnoAdornerLayerHost
{
    private FlowDocument? _document;
    private FlorencePage? _page;
    private FlorencePage? _arrangedPage;
    private UnoFlowDocumentTextView? _textView;
    private double _lastMeasureWidth = -1;
    private double _lastMeasureHeight = -1;
    private readonly List<Microsoft.UI.Xaml.Shapes.Rectangle> _selectionRects = [];
    private readonly List<Microsoft.UI.Xaml.Controls.TextBlock> _lineBlocks = [];
    private readonly List<(Adorner Adorner, int ZOrder)> _adorners = [];
    private bool _selectionDirty = true;
    private readonly AdornerLayer _adornerLayer;

    // Caret overlay. The visual lives here, but hit-testing and geometry come
    // from the WPF-facing ITextView adapter.
    private readonly Microsoft.UI.Xaml.Shapes.Rectangle _caret;
    private DispatcherTimer? _blinkTimer;
    private bool _caretVisible;
    private Rect _caretRect = Rect.Empty;

    internal FlowDocumentView()
    {
        IsHitTestVisible = true;
        _adornerLayer = new AdornerLayer();

        _caret = new Microsoft.UI.Xaml.Shapes.Rectangle
        {
            Width = 1,
            Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black),
            Visibility = Microsoft.UI.Xaml.Visibility.Collapsed,
            IsHitTestVisible = false,
        };
        Children.Add(_caret);

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
            _selectionDirty = true;
            ClearSelectionVisuals();
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
    AdornerLayer IUnoAdornerLayerSource.AdornerLayer => _adornerLayer;
    Visual IUnoAdornerLayerHost.AdornerScope => this;

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
            _selectionDirty = true;
            _textView?.OnLayoutUpdated();
            _document?.StructuralCache?.TextContainer?.TextSelection?.UpdateCaretAndHighlight();
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

        if (_selectionDirty)
        {
            RefreshSelection();
        }

        var lines = _page.Lines;
        for (int i = 0; i < _lineBlocks.Count && i < lines.Count; i++)
            _lineBlocks[i].Arrange(new Windows.Foundation.Rect(0, lines[i].Y, finalSize.Width, lines[i].Height));

        foreach (var (adorner, _) in _adorners)
        {
            adorner.Arrange(new Windows.Foundation.Rect(0, 0, finalSize.Width, finalSize.Height));
        }

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
        if (_adorners.Count > 0)
        {
            ClearSelectionVisuals();
            return;
        }

        _selectionDirty = false;

        if (_page == null || _document?.StructuralCache?.TextContainer?.TextSelection is not ITextSelection selection)
        {
            ClearSelectionVisuals();
            return;
        }

        int start = Math.Min(selection.Start.CharOffset, selection.End.CharOffset);
        int end = Math.Max(selection.Start.CharOffset, selection.End.CharOffset);
        if (start == end)
        {
            ClearSelectionVisuals();
            return;
        }

        int rectIndex = 0;
        foreach (var line in _page.Lines)
        {
            int segmentStart = Math.Max(start, line.StartOffset);
            int segmentEnd = Math.Min(end, line.EndOffset);
            if (segmentStart >= segmentEnd)
                continue;

            double x1 = UnoFlowDocumentTextView.GetPixelXForOffset(line, segmentStart);
            double x2 = UnoFlowDocumentTextView.GetPixelXForOffset(line, segmentEnd);
            double height = line.Height > 0 ? line.Height : 14;

            var rect = GetOrCreateSelectionRect(rectIndex++);
            rect.Tag = new Rect(x1, line.Y, Math.Max(1, x2 - x1), height);
            rect.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        }

        for (int i = rectIndex; i < _selectionRects.Count; i++)
        {
            _selectionRects[i].Tag = Rect.Empty;
            _selectionRects[i].Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private void RebuildLineBlocks()
    {
        foreach (var block in _lineBlocks)
            Children.Remove(block);
        _lineBlocks.Clear();

        if (_page == null)
            return;

        foreach (var line in _page.Lines)
        {
            var block = BuildLineTextBlock(line);
            _lineBlocks.Add(block);
            Children.Add(block);
        }

        foreach (var (adorner, _) in _adorners)
        {
            if (!Children.Contains(adorner))
            {
                Children.Add(adorner);
            }
        }
    }

    private Microsoft.UI.Xaml.Shapes.Rectangle GetOrCreateSelectionRect(int index)
    {
        while (_selectionRects.Count <= index)
        {
            var rect = new Microsoft.UI.Xaml.Shapes.Rectangle
            {
                Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DodgerBlue),
                Opacity = 0.35,
                IsHitTestVisible = false,
                Visibility = Microsoft.UI.Xaml.Visibility.Collapsed,
            };
            _selectionRects.Add(rect);
            int caretIndex = Math.Max(0, Children.IndexOf(_caret));
            Children.Insert(caretIndex, rect);
        }

        return _selectionRects[index];
    }

    private void ClearSelectionVisuals()
    {
        foreach (var rect in _selectionRects)
        {
            rect.Tag = Rect.Empty;
            rect.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }
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

    void IUnoAdornerLayerHost.AddAdorner(Adorner adorner, int zOrder)
    {
        if (_adorners.Any(entry => ReferenceEquals(entry.Adorner, adorner)))
        {
            ((IUnoAdornerLayerHost)this).SetAdornerZOrder(adorner, zOrder);
            return;
        }

        int index = _adorners.FindIndex(entry => zOrder < entry.ZOrder);
        if (index < 0)
        {
            _adorners.Add((adorner, zOrder));
            Children.Add(adorner);
        }
        else
        {
            _adorners.Insert(index, (adorner, zOrder));
            int childIndex = Children.IndexOf(_adorners[index + 1].Adorner);
            if (childIndex < 0)
            {
                Children.Add(adorner);
            }
            else
            {
                Children.Insert(childIndex, adorner);
            }
        }

        _selectionDirty = true;
        InvalidateArrange();
    }

    void IUnoAdornerLayerHost.RemoveAdorner(Adorner adorner)
    {
        int index = _adorners.FindIndex(entry => ReferenceEquals(entry.Adorner, adorner));
        if (index < 0)
        {
            return;
        }

        _adorners.RemoveAt(index);
        Children.Remove(adorner);
        _selectionDirty = true;
        InvalidateArrange();
    }

    void IUnoAdornerLayerHost.SetAdornerZOrder(Adorner adorner, int zOrder)
    {
        ((IUnoAdornerLayerHost)this).RemoveAdorner(adorner);
        ((IUnoAdornerLayerHost)this).AddAdorner(adorner, zOrder);
    }
}
#endif
