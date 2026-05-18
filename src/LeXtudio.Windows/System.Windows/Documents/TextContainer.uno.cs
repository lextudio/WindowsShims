namespace System.Windows.Documents;

// Uno-specific additions to WPF TextContainer.
internal partial class TextContainer
{
    // LayoutHost is used by the Uno rendering pipeline to invalidate layout when content changes.
    internal ITextLayoutHost? LayoutHost { get; set; }
}
