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

    // Caret overlay — positioned purely from Florence data, no WPF TextContainer involved.
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
            while (Children.Count > 1)
                Children.RemoveAt(1);
            foreach (var line in _page.Lines)
                Children.Add(BuildLineTextBlock(line));
            _arrangedPage = _page;
        }

        var lines = _page.Lines;
        int lineCount = Children.Count - 1;
        for (int i = 0; i < lineCount && i < lines.Count; i++)
            Children[i + 1].Arrange(new Windows.Foundation.Rect(0, lines[i].Y, finalSize.Width, lines[i].Height));

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

    // ── Caret — driven purely by Florence hit-test, no WPF TextContainer ─────

    internal void SetCaretAt(Windows.Foundation.Point clickPoint)
    {
        if (_page == null) return;

        var lines = _page.Lines;

        // Find the line under the click.
        FlorenceLine? hit = null;
        foreach (var line in lines)
        {
            if (clickPoint.Y >= line.Y && clickPoint.Y < line.Y + line.Height)
            { hit = line; break; }
        }
        hit ??= lines.Count > 0 ? lines[^1] : null;
        if (hit == null) return;

        // Find X position within the line.
        double caretX = HitTestXToPixel(hit, clickPoint.X);
        double caretH = hit.Height > 0 ? hit.Height : 14;

        _caretRect = new Rect(caretX, hit.Y, 1, caretH);
        _caret.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        _caretVisible = true;
        _caret.Opacity = 1;
        _blinkTimer?.Start();
        InvalidateArrange();
    }

    /// <summary>Given a click X, return the pixel X of the nearest character boundary.</summary>
    private static double HitTestXToPixel(FlorenceLine line, double clickX)
    {
        if (line.Runs.Count == 0) return 0;

        foreach (var run in line.Runs)
        {
            if (clickX >= run.X && clickX < run.X + run.Width)
            {
                // Proportional split within the run.
                double fraction = (clickX - run.X) / Math.Max(run.Width, 1);
                int charInRun = (int)Math.Round(fraction * run.Length);
                charInRun = Math.Clamp(charInRun, 0, run.Length);
                // Return pixel X at that char boundary.
                double charWidth = run.Length > 0 ? run.Width / run.Length : 0;
                return run.X + charInRun * charWidth;
            }
        }

        // Past the end — return end of last run.
        var last = line.Runs[^1];
        return last.X + last.Width;
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
