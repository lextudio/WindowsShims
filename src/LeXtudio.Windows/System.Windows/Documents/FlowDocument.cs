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
}
