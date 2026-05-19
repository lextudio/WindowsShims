namespace System.Windows.Documents;

// Minimal stub. Upstream TextEditorCharacters.cs is not yet promoted; only the
// constants TextRangeEdit references are exposed here.
internal static class TextEditorCharacters
{
    internal const double OneFontPoint = 1.0;
    internal const double MaxFontPoint = 32767.0;

    internal static void _RegisterClassHandlers(System.Type controlType, bool registerEventListeners) { }

    internal static void _OnApplyProperty(object sender, System.Windows.Input.ExecutedRoutedEventArgs e) { }

    internal static void _OnApplyProperty(Documents.TextEditor editor, DependencyProperty property, object value, bool applyToParagraphs) { }
}
