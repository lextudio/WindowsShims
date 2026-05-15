namespace System.Windows.Documents;

/// <summary>
/// WPF-internal node tracking a TextElement's position in the text tree.
/// In the shim, this is a lightweight wrapper carrying the owning TextElement.
/// </summary>
public sealed class TextElementNode
{
    public TextElement? Element { get; }

    public TextElementNode(TextElement? element)
    {
        Element = element;
    }
}
