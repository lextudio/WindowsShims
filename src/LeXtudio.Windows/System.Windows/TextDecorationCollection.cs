namespace System.Windows;

public class TextDecorationCollection : System.Windows.Media.TextDecorationCollection
{
    public static new readonly TextDecorationCollection Empty = new();

    public TextDecorationCollection()
    {
    }

    public TextDecorationCollection(System.Collections.Generic.IEnumerable<System.Windows.Media.TextDecoration> items)
        : base(items)
    {
    }
}
