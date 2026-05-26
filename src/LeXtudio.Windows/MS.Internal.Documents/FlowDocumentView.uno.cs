#if HAS_UNO
using System.Windows.Documents;
using MS.Internal.Florence;

namespace MS.Internal.Documents;

internal class FlowDocumentView : Microsoft.UI.Xaml.Controls.Panel, IServiceProvider, IUnoAdornerLayerHost, ITextLayoutHost
{
    private static Microsoft.UI.Input.InputCursor? _hyperlinkCursor;
    private static Microsoft.UI.Input.InputCursor HyperlinkCursor =>
        _hyperlinkCursor ??= Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Hand);

    private FlowDocument? _document;
    private FlorencePage? _page;
    private FlorencePage? _arrangedPage;
    private UnoFlowDocumentTextView? _textView;
    private double _lastMeasureWidth = -1;
    private double _lastMeasureHeight = -1;
    private uint _lastFormattedGeneration;
    private readonly List<Microsoft.UI.Xaml.Shapes.Rectangle> _selectionRects = [];
    private readonly List<Microsoft.UI.Xaml.FrameworkElement> _lineBlocks = [];
    private readonly List<(Adorner Adorner, int ZOrder)> _adorners = [];
    private bool _selectionDirty = true;
    private ITextSelection? _trackedSelection;
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
            UnhookSelectionChanged();
            if (_document != null && ReferenceEquals(_document.TextLayoutHost, this))
            {
                _document.TextLayoutHost = null;
            }

            _document = value;
            _textView = null;
            ClearSelectionVisuals();
            if (_document != null)
            {
                _document.TextLayoutHost = this;
            }

            HookSelectionChanged();
            InvalidateDocumentLayout();
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
    object ITextLayoutHost.RenderScope => this;
    bool ITextLayoutHost.IsLayoutValid => _page != null;
    double ITextLayoutHost.ViewportWidth => ActualWidth;
    double ITextLayoutHost.ViewportHeight => ActualHeight;
    double ITextLayoutHost.ExtentHeight => _page?.Lines.Count > 0
        ? _page.Lines[^1].Y + _page.Lines[^1].Height
        : 0;

    void ITextLayoutHost.InvalidateLayout() => InvalidateDocumentLayout();

    internal void InvalidateDocumentLayout()
    {
        _page = null;
        _arrangedPage = null;
        _lastMeasureWidth = -1;
        _lastMeasureHeight = -1;
        _lastFormattedGeneration = 0;
        _selectionDirty = true;
        _textView?.OnLayoutInvalidated();
        InvalidateMeasure();
        InvalidateArrange();
    }

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
            _lastFormattedGeneration = _document.StructuralCache.TextContainer.Generation;
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

    private void HookSelectionChanged()
    {
        _trackedSelection = _document?.StructuralCache?.TextContainer?.TextSelection;
        if (_trackedSelection != null)
        {
            _trackedSelection.Changed += OnTrackedSelectionChanged;
        }
    }

    private void UnhookSelectionChanged()
    {
        if (_trackedSelection != null)
        {
            _trackedSelection.Changed -= OnTrackedSelectionChanged;
            _trackedSelection = null;
        }
    }

    private void OnTrackedSelectionChanged(object? sender, EventArgs e)
    {
        var generation = _document?.StructuralCache?.TextContainer?.Generation ?? 0;
        if (generation != _lastFormattedGeneration)
        {
            InvalidateDocumentLayout();
            return;
        }

        _selectionDirty = true;
        InvalidateArrange();
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
            var block = BuildLineVisual(line);
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

    internal System.Windows.Documents.Hyperlink? GetHyperlinkAt(Windows.Foundation.Point point)
    {
        if (_page == null)
            return null;

        foreach (var line in _page.Lines)
        {
            if (point.Y < line.Y || point.Y > line.Y + line.Height)
                continue;

            foreach (var run in line.Runs)
            {
                if (run.Hyperlink is null)
                    continue;

                if (point.X >= run.X && point.X <= run.X + run.Width)
                    return run.Hyperlink;
            }
        }

        return null;
    }

    internal void UpdatePointerCursor(Windows.Foundation.Point point)
    {
        ProtectedCursor = GetHyperlinkAt(point) is null ? null : HyperlinkCursor;
    }

    internal void ClearPointerCursor()
    {
        ProtectedCursor = null;
    }

    internal void ActivateHyperlink(System.Windows.Documents.Hyperlink hyperlink)
    {
        hyperlink.RaiseClick();

        if (hyperlink.NavigateUri is { } uri)
        {
            try
            {
                _ = Windows.System.Launcher.LaunchUriAsync(uri);
            }
            catch
            {
                // Keep hyperlink activation non-fatal so host handlers can still respond to Click.
            }
        }
    }

    private static Microsoft.UI.Xaml.FrameworkElement BuildLineVisual(FlorenceLine line)
    {
        if (line.Runs.Count == 0)
        {
            var tb = new Microsoft.UI.Xaml.Controls.TextBlock
            {
                Text = line.FullText,
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.NoWrap
            };
            return tb;
        }

        var canvas = new Microsoft.UI.Xaml.Controls.Canvas();

        foreach (var run in line.Runs)
        {
            var tb = new Microsoft.UI.Xaml.Controls.TextBlock
            {
                Text = run.Text,
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.NoWrap
            };

            if (run.FontSize > 0) tb.FontSize = run.FontSize;
            if (run.Bold) tb.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
            if (run.Italic) tb.FontStyle = Windows.UI.Text.FontStyle.Italic;
            if (run.FontFamily is not null) tb.FontFamily = run.FontFamily;
            if (run.Foreground is not null) tb.Foreground = run.Foreground;

            canvas.Children.Add(tb);
            Microsoft.UI.Xaml.Controls.Canvas.SetLeft(tb, run.X);
            Microsoft.UI.Xaml.Controls.Canvas.SetTop(tb, 0);
        }

        var localBaseline = line.Baseline - line.Y;
        foreach (var run in line.Runs)
        {
            AddDecorationVisuals(canvas, run, localBaseline);
        }

        return canvas;
    }

    private static void AddDecorationVisuals(
        Microsoft.UI.Xaml.Controls.Canvas canvas,
        FlorenceRun run,
        double baseline)
    {
        if (run.TextDecorations == Windows.UI.Text.TextDecorations.None) return;

        var brush = CloneBrush(run.Foreground)
            ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black);
        var fontSize = run.FontSize > 0 ? run.FontSize : 12.0;

        if ((run.TextDecorations & Windows.UI.Text.TextDecorations.Strikethrough) != 0)
        {
            var strikeY = baseline - fontSize * 0.26;
            var strike = new Microsoft.UI.Xaml.Shapes.Line
            {
                X1 = run.X,
                X2 = run.X + run.Width,
                Y1 = strikeY,
                Y2 = strikeY,
                Stroke = brush,
                StrokeThickness = 1,
            };
            canvas.Children.Add(strike);
        }

        if ((run.TextDecorations & Windows.UI.Text.TextDecorations.Underline) != 0)
        {
            var underlineY = baseline + fontSize * 0.10;
            var underline = new Microsoft.UI.Xaml.Shapes.Line
            {
                X1 = run.X,
                X2 = run.X + run.Width,
                Y1 = underlineY,
                Y2 = underlineY,
                Stroke = CloneBrush(brush) ?? brush,
                StrokeThickness = 1,
            };
            canvas.Children.Add(underline);
        }
    }

    private static Microsoft.UI.Xaml.Media.Brush? CloneBrush(Microsoft.UI.Xaml.Media.Brush? brush) => brush switch
    {
        null => null,
        Microsoft.UI.Xaml.Media.SolidColorBrush scb => new Microsoft.UI.Xaml.Media.SolidColorBrush(scb.Color) { Opacity = scb.Opacity },
        _ => brush
    };

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
