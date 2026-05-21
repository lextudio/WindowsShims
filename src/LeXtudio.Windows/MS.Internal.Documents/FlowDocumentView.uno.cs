#if HAS_UNO
using System.Windows.Documents;
using MS.Internal.Florence;

namespace MS.Internal.Documents;

/// <summary>
/// Uno render scope for RichTextBox.  Runs FlorenceLayoutEngine over the FlowDocument
/// and renders each visual line as a Uno TextBlock child.  Implements IServiceProvider
/// so the WPF TextEditor can obtain a UnoFlowDocumentTextView (ITextView) for cursor
/// placement and editing.
/// </summary>
internal class FlowDocumentView : Microsoft.UI.Xaml.Controls.Panel, IServiceProvider
{
    private FlowDocument? _document;
    private FlorencePage? _page;
    private FlorencePage? _arrangedPage;
    private UnoFlowDocumentTextView? _textView;
    private double _lastMeasureWidth = -1;
    private double _lastMeasureHeight = -1;

    // Caret overlay
    private readonly Microsoft.UI.Xaml.Shapes.Rectangle _caret;
    private ITextSelection? _selection;
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
        Children.Add(_caret);
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

    // Override so RichTextBox.CreateRenderScope can set it after construction.
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

        // Rebuild line children when the page changed (skip the caret overlay at index 0).
        if (!ReferenceEquals(_page, _arrangedPage))
        {
            // Remove all children except the caret (index 0).
            while (Children.Count > 1)
                Children.RemoveAt(1);
            foreach (var line in _page.Lines)
                Children.Add(BuildLineTextBlock(line));
            _arrangedPage = _page;
        }

        // Arrange line children (skip caret at index 0).
        var lines = _page.Lines;
        int lineCount = Children.Count - 1; // subtract caret
        for (int i = 0; i < lineCount && i < lines.Count; i++)
            Children[i + 1].Arrange(new Windows.Foundation.Rect(0, lines[i].Y, finalSize.Width, lines[i].Height));

        // Position caret from stored rect, or hide it off-screen.
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

    // ── Caret ────────────────────────────────────────────────────────────────

    internal void AttachSelection(ITextSelection selection)
    {
        if (_selection != null)
            _selection.Changed -= OnSelectionChanged;
        _selection = selection;
        _selection.Changed += OnSelectionChanged;

        _blinkTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(530) };
        _blinkTimer.Tick += (_, _) =>
        {
            _caretVisible = !_caretVisible;
            _caret.Opacity = _caretVisible ? 1 : 0;
        };
    }

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        UpdateCaretPosition();
    }

    private void UpdateCaretPosition()
    {
        if (_textView == null || _selection == null) return;

        var caretPos = _selection.Start;
        var rect = ((ITextView)_textView).GetRectangleFromTextPosition(caretPos);

        if (rect.IsEmpty)
        {
            _caretRect = Rect.Empty;
            _caret.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            _blinkTimer?.Stop();
            InvalidateArrange();
            return;
        }

        _caretRect = rect;
        _caret.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        _caretVisible = true;
        _caret.Opacity = 1;
        _blinkTimer?.Start();
        InvalidateArrange();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

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

        foreach (var run in line.Runs)
        {
            var inlineRun = new Microsoft.UI.Xaml.Documents.Run { Text = run.Text };
            if (run.Bold)   inlineRun.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
            if (run.Italic) inlineRun.FontStyle  = Windows.UI.Text.FontStyle.Italic;
            tb.Inlines.Add(inlineRun);
        }
        return tb;
    }
}
#endif
