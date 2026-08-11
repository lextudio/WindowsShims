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
    private readonly List<Microsoft.UI.Xaml.Shapes.Rectangle> _borderRects = [];
    private readonly List<Microsoft.UI.Xaml.Shapes.Rectangle> _cellRects = [];
    private readonly List<(Adorner Adorner, int ZOrder)> _adorners = [];
    private bool _selectionDirty = true;
    private ITextSelection? _trackedSelection;
    private int _imeCompositionStart = -1;
    private int _imeCompositionLength = -1;
    private readonly List<Microsoft.UI.Xaml.Shapes.Line> _imeUnderlineLines = [];
    private bool _spellCheckEnabled;
    private readonly List<Microsoft.UI.Xaml.Shapes.Polyline> _spellCheckLines = [];
    private readonly AdornerLayer _adornerLayer;

    // Caret overlay. The visual lives here, but hit-testing and geometry come
    // from the WPF-facing ITextView adapter.
    private readonly Microsoft.UI.Xaml.Shapes.Rectangle _caret;
    private readonly Microsoft.UI.Xaml.Shapes.Rectangle _dropCaret;
    private DispatcherTimer? _blinkTimer;
    private bool _caretVisible;
    private Rect _caretRect = Rect.Empty;

    internal bool ReadOnly
    {
        get => _readOnly;
        set
        {
            _readOnly = value;
            if (value)
            {
                _caret.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                _caretVisible = false;
                _blinkTimer?.Stop();
                ClearSelectionVisuals();
            }
            else if (!_caretRect.IsEmpty)
            {
                _caret.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                _caretVisible = true;
                _caret.Opacity = 1;
                _blinkTimer?.Start();
            }
        }
    }
    private bool _readOnly;

    // Number of paragraph border side rectangles currently in the visual tree
    // (used by integration tests to verify paragraph borders render).
    internal int ParagraphBorderRectCount => _borderRects.Count;

    // Number of cell background/border rectangles currently in the visual tree.
    internal int CellVisualRectCount => _cellRects.Count;

    // Layout of the page's cell boxes as "X:Width" pairs joined by commas
    // (used by integration tests to verify column widths drive cell layout).
    internal string CellBoxLayout => string.Join(",", _page?.CellBoxes.Select(b => $"{b.X.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}:{b.Width.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}") ?? []);

    // ── Spell Check ─────────────────────────────────────────────────────────

    internal void SetSpellCheckEnabled(bool enabled)
    {
        if (_spellCheckEnabled == enabled)
            return;
        _spellCheckEnabled = enabled;
        InvalidateArrange();
    }

    private static readonly Lazy<Microsoft.UI.Xaml.Documents.ISpellCheckingService?> SpellCheckingService = new(() =>
    {
        // The Hunspell service registers itself via a generated module initializer in the
        // Uno.WinUI.SpellChecking add-in. Loading the assembly by name (present only when
        // the SpellChecking UnoFeature is enabled) guarantees that initializer runs before
        // ApiExtensibility is asked for the instance.
        try { System.Reflection.Assembly.Load("Uno.WinUI.SpellChecking"); } catch { }

        return Uno.Foundation.Extensibility.ApiExtensibility.CreateInstance<Microsoft.UI.Xaml.Documents.ISpellCheckingService>(
            typeof(FlowDocumentView), out var service)
            ? service
            : null;
    });

    internal Microsoft.UI.Xaml.TextWrapping TextWrapping { get; set; } = Microsoft.UI.Xaml.TextWrapping.Wrap;
    internal Microsoft.UI.Xaml.Media.Brush? InheritedForeground { get; set; }
    internal Microsoft.UI.Xaml.Media.Brush? InheritedBackground { get; set; }
    internal Microsoft.UI.Xaml.Media.FontFamily? InheritedFontFamily { get; set; }
    internal double InheritedFontSize { get; set; }
    internal FontWeight InheritedFontWeight { get; set; } = global::System.Windows.FontWeights.Normal;
    internal Windows.UI.Text.FontStyle InheritedFontStyle { get; set; } = Windows.UI.Text.FontStyle.Normal;

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

        _dropCaret = new Microsoft.UI.Xaml.Shapes.Rectangle
        {
            Width = 2,
            Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black),
            Visibility = Microsoft.UI.Xaml.Visibility.Collapsed,
            IsHitTestVisible = false,
        };
        Children.Add(_dropCaret);

        _blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(530) };
        _blinkTimer.Tick += (_, _) =>
        {
            _caretVisible = !_caretVisible;
            _caret.Opacity = _caretVisible ? 1 : 0;
        };

        for (int i = 0; i < 16; i++)
        {
            var line = new Microsoft.UI.Xaml.Shapes.Line
            {
                Stroke = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black),
                StrokeThickness = 1,
                IsHitTestVisible = false,
                Opacity = 0,
            };
            _imeUnderlineLines.Add(line);
            Children.Add(line);
        }
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
        if (TextWrapping == Microsoft.UI.Xaml.TextWrapping.NoWrap)
            w = double.PositiveInfinity;
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

        RefreshImeCompositionUnderline();
        RefreshSpellCheckSquiggles();

        var lines = _page.Lines;
        for (int i = 0; i < _lineBlocks.Count && i < lines.Count; i++)
            _lineBlocks[i].Arrange(new Windows.Foundation.Rect(0, lines[i].Y, finalSize.Width, lines[i].Height));

        var paragraphBorders = _page.ParagraphBorders;
        int borderRectIndex = 0;
        foreach (var paragraphBorder in paragraphBorders)
        {
            var t = paragraphBorder.Thickness;
            double bx = paragraphBorder.X;
            double by = paragraphBorder.Y;
            double bw = paragraphBorder.Width;
            double bh = paragraphBorder.Height;
            if (t.Top > 0)
                _borderRects[borderRectIndex++].Arrange(new Windows.Foundation.Rect(bx, by, bw, t.Top));
            if (t.Bottom > 0)
                _borderRects[borderRectIndex++].Arrange(new Windows.Foundation.Rect(bx, by + bh - t.Bottom, bw, t.Bottom));
            if (t.Left > 0)
                _borderRects[borderRectIndex++].Arrange(new Windows.Foundation.Rect(bx, by, t.Left, bh));
            if (t.Right > 0)
                _borderRects[borderRectIndex++].Arrange(new Windows.Foundation.Rect(bx + bw - t.Right, by, t.Right, bh));
        }

        var cellBoxes = _page.CellBoxes;
        int cellRectIndex = 0;
        foreach (var cellBox in cellBoxes)
        {
            if (cellBox.Background is not null)
                _cellRects[cellRectIndex++].Arrange(new Windows.Foundation.Rect(cellBox.X, cellBox.Y, cellBox.Width, cellBox.Height));
            var ct = cellBox.BorderThickness;
            if (cellBox.BorderBrush is not null)
            {
                if (ct.Top > 0)
                    _cellRects[cellRectIndex++].Arrange(new Windows.Foundation.Rect(cellBox.X, cellBox.Y, cellBox.Width, ct.Top));
                if (ct.Bottom > 0)
                    _cellRects[cellRectIndex++].Arrange(new Windows.Foundation.Rect(cellBox.X, cellBox.Y + cellBox.Height - ct.Bottom, cellBox.Width, ct.Bottom));
                if (ct.Left > 0)
                    _cellRects[cellRectIndex++].Arrange(new Windows.Foundation.Rect(cellBox.X, cellBox.Y, ct.Left, cellBox.Height));
                if (ct.Right > 0)
                    _cellRects[cellRectIndex++].Arrange(new Windows.Foundation.Rect(cellBox.X + cellBox.Width - ct.Right, cellBox.Y, ct.Right, cellBox.Height));
            }
        }

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
        if (ReadOnly)
        {
            _caret.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            _caretVisible = false;
            _blinkTimer?.Stop();
            return;
        }

        var textView = _textView ??= new UnoFlowDocumentTextView(this);
        position = textView.NormalizeToVisiblePosition(position);
        var rect = textView.GetRectangleFromTextPosition(position);
        if (rect.IsEmpty)
            return;

        _caretRect = rect;
        _caret.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        _caretVisible = true;
        _caret.Opacity = 1;

        // Hide caret during active selection
        if (_document?.StructuralCache?.TextContainer?.TextSelection is ITextSelection sel && !sel.IsEmpty)
        {
            _caret.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            _caretVisible = false;
        }

        _blinkTimer?.Start();
        InvalidateArrange();
        StartBringIntoView();
    }

    internal void SetDropCaretAt(Windows.Foundation.Point point)
    {
        var textView = _textView ??= new UnoFlowDocumentTextView(this);
        var position = textView.GetTextPositionFromPoint(point, snapToText: true);
        position = textView.NormalizeToVisiblePosition(position);
        var rect = textView.GetRectangleFromTextPosition(position);
        if (rect.IsEmpty)
            return;

        _dropCaret.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        _dropCaret.Arrange(new Windows.Foundation.Rect(rect.X, rect.Y, 2, rect.Height > 0 ? rect.Height : 14));
        InvalidateArrange();
    }

    internal void ClearDropCaret()
    {
        _dropCaret.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        InvalidateArrange();
    }

    internal void RefreshSelection()
    {
        if (ReadOnly)
        {
            ClearSelectionVisuals();
            _caret.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            _caretVisible = false;
            return;
        }

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

        // Hide caret during active selection
        if (!selection.IsEmpty)
        {
            _caret.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            _caretVisible = false;
        }
        else if (!_caretRect.IsEmpty)
        {
            _caret.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            _caretVisible = true;
            _caret.Opacity = 1;
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

    private static IEnumerable<Microsoft.UI.Xaml.Shapes.Rectangle> BuildParagraphBorderSides(MS.Internal.Florence.FlorenceParagraphBorder border)
    {
        return BuildBorderSides(border.X, border.Y, border.Width, border.Height, border.Brush, border.Thickness);
    }

    private static IEnumerable<Microsoft.UI.Xaml.Shapes.Rectangle> BuildCellVisuals(MS.Internal.Florence.FlorenceCellBox box)
    {
        if (box.Background is not null)
        {
            var fill = new Microsoft.UI.Xaml.Shapes.Rectangle
            {
                Width = box.Width,
                Height = box.Height,
                Fill = box.Background,
                IsHitTestVisible = false,
            };
            Microsoft.UI.Xaml.Controls.Canvas.SetLeft(fill, box.X);
            Microsoft.UI.Xaml.Controls.Canvas.SetTop(fill, box.Y);
            yield return fill;
        }

        if (box.BorderBrush is not null)
        {
            foreach (var side in BuildBorderSides(box.X, box.Y, box.Width, box.Height, box.BorderBrush, box.BorderThickness))
                yield return side;
        }
    }

    private static IEnumerable<Microsoft.UI.Xaml.Shapes.Rectangle> BuildBorderSides(
        double x, double y, double w, double h, Microsoft.UI.Xaml.Media.Brush brush, Microsoft.UI.Xaml.Thickness t)
    {
        Microsoft.UI.Xaml.Shapes.Rectangle Side(double left, double top, double width, double height)
        {
            var rect = new Microsoft.UI.Xaml.Shapes.Rectangle
            {
                Width = width,
                Height = height,
                Fill = brush,
                Stroke = null,
                IsHitTestVisible = false,
            };
            Microsoft.UI.Xaml.Controls.Canvas.SetLeft(rect, left);
            Microsoft.UI.Xaml.Controls.Canvas.SetTop(rect, top);
            return rect;
        }

        if (t.Top > 0)
            yield return Side(x, y, w, t.Top);
        if (t.Bottom > 0)
            yield return Side(x, y + h - t.Bottom, w, t.Bottom);
        if (t.Left > 0)
            yield return Side(x, y, t.Left, h);
        if (t.Right > 0)
            yield return Side(x + w - t.Right, y, t.Right, h);
    }

    private void RebuildLineBlocks()
    {
        foreach (var block in _lineBlocks)
            Children.Remove(block);
        _lineBlocks.Clear();

        foreach (var border in _borderRects)
            Children.Remove(border);
        _borderRects.Clear();

        foreach (var cell in _cellRects)
            Children.Remove(cell);
        _cellRects.Clear();

        if (_page == null)
            return;

        foreach (var paragraphBorder in _page.ParagraphBorders)
        {
            foreach (var side in BuildParagraphBorderSides(paragraphBorder))
            {
                _borderRects.Add(side);
                Children.Add(side);
            }
        }

        foreach (var cellBox in _page.CellBoxes)
        {
            foreach (var visual in BuildCellVisuals(cellBox))
            {
                _cellRects.Add(visual);
                Children.Add(visual);
            }
        }

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

    // ── IME Composition Underline ───────────────────────────────────────────

    internal int ImeUnderlineLineCount => _imeUnderlineLines.Count(l => l.Opacity > 0);

    /// <summary>Visible spell-check squiggle polylines (test observability).</summary>
    internal int SpellCheckSquiggleCount => _spellCheckLines.Count(l => l.Visibility == Microsoft.UI.Xaml.Visibility.Visible);

    internal void SetImeCompositionRange(int start, int length)
    {
        _imeCompositionStart = start;
        _imeCompositionLength = length;
    }

    private void RefreshImeCompositionUnderline()
    {
        if (_imeCompositionStart < 0 || _imeCompositionLength <= 0 || _page == null)
        {
            foreach (var line in _imeUnderlineLines)
                line.Opacity = 0;
            return;
        }

        int end = _imeCompositionStart + _imeCompositionLength;
        int lineIndex = 0;

        foreach (var pageLine in _page.Lines)
        {
            int segStart = Math.Max(_imeCompositionStart, pageLine.StartOffset);
            int segEnd = Math.Min(end, pageLine.EndOffset);
            if (segStart >= segEnd)
                continue;

            double x1 = UnoFlowDocumentTextView.GetPixelXForOffset(pageLine, segStart);
            double x2 = UnoFlowDocumentTextView.GetPixelXForOffset(pageLine, segEnd);
            double baseline = pageLine.Baseline;

            if (lineIndex >= _imeUnderlineLines.Count)
                break;

            var underline = _imeUnderlineLines[lineIndex++];
            underline.X1 = x1;
            underline.X2 = x2;
            underline.Y1 = baseline + 2;
            underline.Y2 = baseline + 2;
            underline.Opacity = 1;
        }

        for (int i = lineIndex; i < _imeUnderlineLines.Count; i++)
            _imeUnderlineLines[i].Opacity = 0;
    }

    // ── Spell Check Squiggles ───────────────────────────────────────────────
    //
    // Misspelled words are underlined with a red wavy XAML Polyline, mirroring the
    // WPF/Word squiggle. The Hunspell-backed service is resolved through Uno's
    // ApiExtensibility (Uno.WinUI.SpellChecking add-in); when it is unavailable
    // (e.g. the feature is off) spelling simply renders nothing.
    //
    // Each FlorenceLine is spell-checked independently against its own text, then
    // correction ranges are mapped to pixels via the same TextBlock-measured widths
    // the caret/hit-testing use, so squiggles line up with the rendered glyphs.

    private void RefreshSpellCheckSquiggles()
    {
        int used = 0;

        if (_spellCheckEnabled && _page is not null && SpellCheckingService.Value is { } service)
        {
            foreach (var pageLine in _page.Lines)
            {
                if (string.IsNullOrEmpty(pageLine.FullText))
                    continue;

                var boundaries = GetWords(pageLine.FullText);
                if (boundaries.Count == 0)
                    continue;

                var corrections = service.SpellCheck(boundaries, pageLine.FullText);
                for (int i = 0; i < corrections.Count && i < boundaries.Count; i++)
                {
                    if (corrections[i] is not (int correctionStart, int correctionEnd) || correctionEnd <= correctionStart)
                        continue;

                    int wordStart = i == 0 ? 0 : boundaries[i - 1];
                    double x1 = UnoFlowDocumentTextView.GetPixelXForOffset(pageLine, pageLine.StartOffset + wordStart + correctionStart);
                    double x2 = UnoFlowDocumentTextView.GetPixelXForOffset(pageLine, pageLine.StartOffset + wordStart + correctionEnd);

                    var squiggle = AcquireSpellCheckLine(used++);
                    squiggle.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    squiggle.Points = BuildSquigglePoints(x1, x2, pageLine.Baseline + 2);
                }
            }
        }

        for (int i = used; i < _spellCheckLines.Count; i++)
            _spellCheckLines[i].Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    private Microsoft.UI.Xaml.Shapes.Polyline AcquireSpellCheckLine(int index)
    {
        while (_spellCheckLines.Count <= index)
        {
            var squiggle = new Microsoft.UI.Xaml.Shapes.Polyline
            {
                Stroke = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red),
                StrokeThickness = 1,
                IsHitTestVisible = false,
                Visibility = Microsoft.UI.Xaml.Visibility.Collapsed,
            };
            _spellCheckLines.Add(squiggle);
            Children.Add(squiggle);
        }
        return _spellCheckLines[index];
    }

    private static Microsoft.UI.Xaml.Media.PointCollection BuildSquigglePoints(double x1, double x2, double baseY)
    {
        var points = new Microsoft.UI.Xaml.Media.PointCollection();
        double width = x2 - x1;
        if (width <= 0.5)
            return points;

        const double step = 2.5;
        int n = Math.Max(1, (int)Math.Ceiling(width / step));
        for (int i = 0; i <= n; i++)
        {
            double x = x1 + width * i / n;
            double y = baseY + Math.Sin(i * Math.PI / 2) * 1.25;
            points.Add(new Windows.Foundation.Point(x, y));
        }
        return points;
    }

    /// <summary>
    /// Word boundary end offsets for the given text, mirroring how Uno's own
    /// UnicodeText computes boundaries (whitespace-delimited words, with adjacent
    /// single punctuation/symbol runs folded into the neighboring word). The
    /// spell-check service consumes this same boundary format.
    /// </summary>
    private static List<int> GetWords(string text)
    {
        var tokens = new List<int>();
        int i = 0;
        while (i < text.Length)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                i++;
                continue;
            }
            while (i < text.Length && !char.IsWhiteSpace(text[i]))
                i++;
            tokens.Add(i);
        }

        if (tokens.Count == 0)
            return [];

        var ret = new List<int> { tokens[0] };
        for (int index = 1; index < tokens.Count; index++)
        {
            int boundary = tokens[index];
            if (boundary - ret[^1] == 1 &&
                (char.IsPunctuation(text[boundary - 1]) || char.IsSymbol(text[boundary - 1])) &&
                (char.IsPunctuation(text[ret[^1] - 1]) || char.IsSymbol(text[ret[^1] - 1])))
            {
                ret.RemoveAt(ret.Count - 1);
            }
            ret.Add(boundary);
        }
        return ret;
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

    private Microsoft.UI.Xaml.FrameworkElement BuildLineVisual(FlorenceLine line)
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
            if (run.EmbeddedElement is not null)
            {
                // Parent the embedded UIElement directly into the canvas
                var ee = run.EmbeddedElement;
                ee.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
                ee.Arrange(new Windows.Foundation.Rect(0, 0, ee.DesiredSize.Width, ee.DesiredSize.Height));
                canvas.Children.Add(ee);
                Microsoft.UI.Xaml.Controls.Canvas.SetLeft(ee, run.X);
                Microsoft.UI.Xaml.Controls.Canvas.SetTop(ee, 0);
                continue;
            }

            for (int i = 0; i < run.Text.Length; i++)
            {
                var tb = new Microsoft.UI.Xaml.Controls.TextBlock
                {
                    Text = run.Text[i].ToString(),
                    TextWrapping = Microsoft.UI.Xaml.TextWrapping.NoWrap
                };

                ApplyRunFormatting(tb, run);

                canvas.Children.Add(tb);
                Microsoft.UI.Xaml.Controls.Canvas.SetLeft(
                    tb,
                    run.X + MS.Internal.Florence.TextMeasurer.MeasurePrefixWidth(run, i));
                Microsoft.UI.Xaml.Controls.Canvas.SetTop(tb, 0);
            }
        }

        var localBaseline = line.Baseline - line.Y;
        foreach (var run in line.Runs)
        {
            AddDecorationVisuals(canvas, run, localBaseline);
        }

        return canvas;
    }

    private void ApplyRunFormatting(Microsoft.UI.Xaml.Controls.TextBlock tb, FlorenceRun run)
    {
        if (run.FontSize > 0) tb.FontSize = run.FontSize;
        else if (InheritedFontSize > 0) tb.FontSize = InheritedFontSize;
        if (run.Bold) tb.FontWeight = global::System.Windows.FontWeights.Bold;
        else tb.FontWeight = InheritedFontWeight;
        if (run.Italic) tb.FontStyle = Windows.UI.Text.FontStyle.Italic;
        else tb.FontStyle = InheritedFontStyle;
        if (run.FontFamily is not null) tb.FontFamily = run.FontFamily;
        else if (InheritedFontFamily is not null) tb.FontFamily = InheritedFontFamily;
        if (run.Foreground is not null) tb.Foreground = run.Foreground;
        else if (InheritedForeground is not null) tb.Foreground = InheritedForeground;
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
