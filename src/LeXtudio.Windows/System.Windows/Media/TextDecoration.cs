namespace System.Windows.Media
{
    /// <summary>Portable shim for System.Windows.TextDecoration.</summary>
    public class TextDecoration
    {
        public TextDecorationLocation Location { get; }
        public TextDecoration() { Location = TextDecorationLocation.Underline; }
        public TextDecoration(TextDecorationLocation location) { Location = location; }
    }
}
