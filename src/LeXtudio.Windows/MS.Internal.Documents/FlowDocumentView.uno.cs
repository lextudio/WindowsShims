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
    private UnoFlowDocumentTextView? _textView;

    // ── Document ────────────────────────────────────────────────────────────

    internal FlowDocument? Document
    {
        get => _document;
        set
        {
            _document = value;
            _page = null;
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
        _page = FlorenceLayoutEngine.Format(_document, new Windows.Foundation.Size(w, availableSize.Height));

        double totalH = _page.Lines.Count > 0
            ? _page.Lines[^1].Y + _page.Lines[^1].Height
            : 0;

        _textView?.OnLayoutUpdated();

        return new Windows.Foundation.Size(
            Math.Min(w, double.IsInfinity(availableSize.Width) ? w : availableSize.Width),
            Math.Min(totalH, double.IsInfinity(availableSize.Height) ? totalH : availableSize.Height));
    }

    protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
    {
        Children.Clear();

        if (_page == null)
            return finalSize;

        foreach (var line in _page.Lines)
        {
            var tb = BuildLineTextBlock(line);
            Children.Add(tb);
            tb.Arrange(new Windows.Foundation.Rect(0, line.Y, finalSize.Width, line.Height));
        }

        return finalSize;
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
