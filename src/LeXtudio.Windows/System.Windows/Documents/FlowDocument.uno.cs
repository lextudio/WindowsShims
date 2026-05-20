namespace System.Windows.Documents;

public partial class FlowDocument
{
    private ITextLayoutHost? _layoutHost;

    public ITextLayoutHost? TextLayoutHost
    {
        get => _layoutHost;
        set
        {
            _layoutHost = value;
            foreach (var block in Blocks)
                ((TextElement)block).SetLayoutHostRecursive(value);
        }
    }
}
