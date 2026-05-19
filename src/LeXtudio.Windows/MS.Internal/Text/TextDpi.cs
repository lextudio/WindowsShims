namespace MS.Internal.Text;

public static class TextDpi
{
    public const double MinWidth = 0d;
    public const double MaxWidth = 1_000_000d;
}

// WPF helper that reads inherited/animated property values from a DependencyObject.
// Used by TextEffectTarget.Enable/Disable/IsEnabled to get the TextEffects collection.
internal static class DynamicPropertyReader
{
    internal static System.Windows.Media.TextEffectCollection? GetTextEffects(Microsoft.UI.Xaml.DependencyObject element)
        => element.GetValue(System.Windows.Documents.TextElement.TextEffectsProperty) as System.Windows.Media.TextEffectCollection;
}
