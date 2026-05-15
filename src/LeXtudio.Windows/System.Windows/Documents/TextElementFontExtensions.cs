namespace System.Windows.Documents;

using Media;

/// <summary>
/// Extension properties for TextElement to provide WPF-compatible font styling.
///
/// TextElement instances may need FontWeight and FontStyle for proper text rendering.
/// These extensions bridge the gap between WPF's TextElement and modern typography needs.
/// </summary>
internal static class TextElementFontExtensions
{
    // Use attached properties to store font styling on TextElement instances
    private static readonly System.Windows.DependencyProperty FontWeightProperty =
        System.Windows.DependencyProperty.Register(
            "FontWeight",
            typeof(FontWeight),
            typeof(TextElement),
            new System.Windows.FrameworkPropertyMetadata(FontWeights.Normal));

    private static readonly System.Windows.DependencyProperty FontStyleProperty =
        System.Windows.DependencyProperty.Register(
            "FontStyle",
            typeof(FontStyle),
            typeof(TextElement),
            new System.Windows.FrameworkPropertyMetadata(FontStyles.Normal));

    /// <summary>Gets the font weight for a TextElement.</summary>
    public static FontWeight GetFontWeight(this TextElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        var value = element.GetValue(FontWeightProperty);
        return value is FontWeight fw ? fw : FontWeights.Normal;
    }

    /// <summary>Sets the font weight for a TextElement.</summary>
    public static void SetFontWeight(this TextElement element, FontWeight value)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        element.SetValue(FontWeightProperty, value);
    }

    /// <summary>Gets the font style for a TextElement.</summary>
    public static FontStyle GetFontStyle(this TextElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        var value = element.GetValue(FontStyleProperty);
        return value is FontStyle fs ? fs : FontStyles.Normal;
    }

    /// <summary>Sets the font style for a TextElement.</summary>
    public static void SetFontStyle(this TextElement element, FontStyle value)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        element.SetValue(FontStyleProperty, value);
    }
}
