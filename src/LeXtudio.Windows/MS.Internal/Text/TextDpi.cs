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

    // Returns the effective LineHeight for a DependencyObject.
    // On HAS_UNO, LineHeight 'Auto' (NaN) is represented as 0 (no explicit line spacing).
    internal static double GetLineHeightValue(Microsoft.UI.Xaml.DependencyObject d)
    {
        double lineHeight = (double)d.GetValue(System.Windows.Documents.Block.LineHeightProperty);
        if (double.IsNaN(lineHeight))
        {
            // Auto: approximate from FontSize * default line spacing factor (1.2).
            double fontSize = (double)d.GetValue(System.Windows.Documents.TextElement.FontSizeProperty);
            lineHeight = fontSize * 1.2;
        }
        return lineHeight;
    }
}
