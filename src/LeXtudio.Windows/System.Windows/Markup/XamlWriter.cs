namespace System.Windows.Markup;

// Stub: WPF's XamlWriter serializes a UIElement to a XAML string.
// TextTreeDeleteContentUndoUnit uses this to snapshot embedded objects for undo.
// On Uno the undo path is dormant, so a no-op implementation is sufficient.
public static class XamlWriter
{
    public static string Save(object obj) => string.Empty;

    public static void Save(object obj, System.IO.TextWriter writer) { }

    public static void Save(object obj, System.Xml.XmlWriter xmlWriter) { }

    public static void Save(object obj, XamlDesignerSerializationManager manager) { }
}
