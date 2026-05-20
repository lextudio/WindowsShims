namespace System.Windows.Documents;

public sealed class FlowDocument : TextElement
{
    private readonly BlockCollection _blocks;
    private System.Windows.Controls.RichTextBox? _owner;

    public FlowDocument()
    {
        _blocks = new BlockCollection(this, true);
    }

    public new object? Parent { get; set; }

    public BlockCollection Blocks => _blocks;

    public ITextLayoutHost? TextLayoutHost
    {
        get => LayoutHost;
        set => SetLayoutHostRecursive(value);
    }

    internal System.Windows.Controls.RichTextBox? Owner
    {
        get => _owner;
        set => _owner = value;
    }

    public static readonly DependencyProperty FlowDirectionProperty =
        Microsoft.UI.Xaml.FrameworkElement.FlowDirectionProperty;

    // WPF structural cache — not applicable on HAS_UNO.
    internal object? StructuralCache => null;

    // WPF DPI propagation — no-op on HAS_UNO.
    internal void SetDpi(System.Windows.Controls.DpiScale dpiScale) { }

    // WPF page-size notification — no-op event on HAS_UNO.
    internal event EventHandler? PageSizeChanged;

    // WPF text-wrapping property — no-op on HAS_UNO (wrapping is controlled via Uno's layout engine).
    public TextWrapping TextWrapping { get; set; }
}
