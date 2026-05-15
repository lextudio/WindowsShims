namespace System.Windows.Documents;

using Media;

/// <summary>
/// Extension properties for TextElement providing WPF-compatible font styling.
/// Uses C# 14 extension members so callers can write <c>element.FontWeight = ...</c>.
/// </summary>
public static class TextElementFontExtensions
{
    private static readonly Microsoft.UI.Xaml.DependencyProperty FontWeightProperty =
        Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(
            "FontWeight",
            typeof(FontWeight),
            typeof(TextElement),
            new System.Windows.FrameworkPropertyMetadata(FontWeights.Normal));

    private static readonly Microsoft.UI.Xaml.DependencyProperty FontStyleProperty =
        Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(
            "FontStyle",
            typeof(FontStyle),
            typeof(TextElement),
            new System.Windows.FrameworkPropertyMetadata(FontStyles.Normal));

    extension(TextElement element)
    {
        public FontWeight FontWeight
        {
            get
            {
                ArgumentNullException.ThrowIfNull(element);
                var value = element.GetValue(FontWeightProperty);
                return value is FontWeight fw ? fw : FontWeights.Normal;
            }
            set
            {
                ArgumentNullException.ThrowIfNull(element);
                element.SetValue(FontWeightProperty, value);
            }
        }

        public FontStyle FontStyle
        {
            get
            {
                ArgumentNullException.ThrowIfNull(element);
                var value = element.GetValue(FontStyleProperty);
                return value is FontStyle fs ? fs : FontStyles.Normal;
            }
            set
            {
                ArgumentNullException.ThrowIfNull(element);
                element.SetValue(FontStyleProperty, value);
            }
        }
    }
}
