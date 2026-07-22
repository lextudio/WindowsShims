namespace System.Windows.Controls;

// Fluent visual tokens adapted from WPF's PresentationFramework.Fluent
// Light/Dark dictionaries. Keep these in one place because the shim's runtime
// ControlTemplates cannot reliably resolve library Generic.xaml resources from
// every Uno consumer.
internal static class DataGridFluentTheme
{
    internal static Microsoft.UI.Xaml.Media.Brush GridBackground
        => GridBackgroundFor(null);

    internal static Microsoft.UI.Xaml.Media.Brush GridBackgroundFor(Microsoft.UI.Xaml.FrameworkElement? element)
        => Resolve(element, "CardBackgroundFillColorDefaultBrush", 0xB3, 0xFF, 0xFF, 0xFF, 0x0D, 0xFF, 0xFF, 0xFF);

    internal static Microsoft.UI.Xaml.Media.Brush HeaderBackground
        => HeaderBackgroundFor(null);

    internal static Microsoft.UI.Xaml.Media.Brush HeaderBackgroundFor(Microsoft.UI.Xaml.FrameworkElement? element)
        => Resolve(element, "SubtleFillColorTertiaryBrush", 0x06, 0x00, 0x00, 0x00, 0x0A, 0xFF, 0xFF, 0xFF);

    internal static Microsoft.UI.Xaml.Media.Brush RowHeaderBackground
        => RowHeaderBackgroundFor(null);

    internal static Microsoft.UI.Xaml.Media.Brush RowHeaderBackgroundFor(Microsoft.UI.Xaml.FrameworkElement? element)
        => HeaderBackgroundFor(element);

    internal static Microsoft.UI.Xaml.Media.Brush GridLine
        => GridLineFor(null);

    internal static Microsoft.UI.Xaml.Media.Brush GridLineFor(Microsoft.UI.Xaml.FrameworkElement? element)
        => Resolve(element, "ControlStrokeColorSecondaryBrush", 0x29, 0x00, 0x00, 0x00, 0x18, 0xFF, 0xFF, 0xFF);

    internal static Microsoft.UI.Xaml.Media.Brush OuterBorder
        => OuterBorderFor(null);

    internal static Microsoft.UI.Xaml.Media.Brush OuterBorderFor(Microsoft.UI.Xaml.FrameworkElement? element)
        => Resolve(element, "ControlStrokeColorDefaultBrush", 0x0F, 0x00, 0x00, 0x00, 0x12, 0xFF, 0xFF, 0xFF);

    internal static Microsoft.UI.Xaml.Media.Brush PrimaryText
        => PrimaryTextFor(null);

    internal static Microsoft.UI.Xaml.Media.Brush PrimaryTextFor(Microsoft.UI.Xaml.FrameworkElement? element)
        => Resolve(element, "TextFillColorPrimaryBrush", 0xE4, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF);

    internal static Microsoft.UI.Xaml.Media.Brush SecondaryText
        => SecondaryTextFor(null);

    internal static Microsoft.UI.Xaml.Media.Brush SecondaryTextFor(Microsoft.UI.Xaml.FrameworkElement? element)
        => Resolve(element, "TextFillColorSecondaryBrush", 0x9E, 0x00, 0x00, 0x00, 0xC5, 0xFF, 0xFF, 0xFF);

    internal static Microsoft.UI.Xaml.Media.Brush Selection
        => SelectionFor(null);

    internal static Microsoft.UI.Xaml.Media.Brush SelectionFor(Microsoft.UI.Xaml.FrameworkElement? element)
        => ResolveTint(element, "SystemAccentColor", 0xCC, 0x00, 0x78, 0xD4);

    internal static Microsoft.UI.Xaml.Media.Brush RowHover
        => RowHoverFor(null);

    internal static Microsoft.UI.Xaml.Media.Brush RowHoverFor(Microsoft.UI.Xaml.FrameworkElement? element)
        => Resolve(element, "SubtleFillColorSecondaryBrush", 0x09, 0x00, 0x00, 0x00, 0x0F, 0xFF, 0xFF, 0xFF);

    internal static Microsoft.UI.Xaml.Media.Brush AlternatingRow
        => AlternatingRowFor(null);

    internal static Microsoft.UI.Xaml.Media.Brush AlternatingRowFor(Microsoft.UI.Xaml.FrameworkElement? element)
        => Resolve(element, "SubtleFillColorSecondaryBrush", 0x09, 0x00, 0x00, 0x00, 0x0F, 0xFF, 0xFF, 0xFF);

    private static Microsoft.UI.Xaml.Media.Brush Resolve(
        Microsoft.UI.Xaml.FrameworkElement? element,
        string key,
        byte lightAlpha,
        byte lightRed,
        byte lightGreen,
        byte lightBlue,
        byte darkAlpha,
        byte darkRed,
        byte darkGreen,
        byte darkBlue)
    {
        if (Microsoft.UI.Xaml.Application.Current?.Resources.TryGetValue(key, out var value) == true
            && value is Microsoft.UI.Xaml.Media.Brush brush)
        {
            return brush;
        }

        var dark = IsDark(element);
        return new Microsoft.UI.Xaml.Media.SolidColorBrush(
            dark
                ? global::Windows.UI.Color.FromArgb(darkAlpha, darkRed, darkGreen, darkBlue)
                : global::Windows.UI.Color.FromArgb(lightAlpha, lightRed, lightGreen, lightBlue));
    }

    private static Microsoft.UI.Xaml.Media.Brush ResolveTint(
        Microsoft.UI.Xaml.FrameworkElement? element,
        string key,
        byte alpha,
        byte fallbackRed,
        byte fallbackGreen,
        byte fallbackBlue)
    {
        var color = Microsoft.UI.Xaml.Application.Current?.Resources.TryGetValue(key, out var value) == true
            ? value switch
            {
                global::Windows.UI.Color resourceColor => resourceColor,
                Microsoft.UI.Xaml.Media.SolidColorBrush resourceBrush => resourceBrush.Color,
                _ => global::Windows.UI.Color.FromArgb(0xFF, fallbackRed, fallbackGreen, fallbackBlue),
            }
            : global::Windows.UI.Color.FromArgb(0xFF, fallbackRed, fallbackGreen, fallbackBlue);

        return new Microsoft.UI.Xaml.Media.SolidColorBrush(
            global::Windows.UI.Color.FromArgb(alpha, color.R, color.G, color.B));
    }

    private static bool IsDark(Microsoft.UI.Xaml.FrameworkElement? element)
    {
        if (element?.RequestedTheme == Microsoft.UI.Xaml.ElementTheme.Dark
            || element?.ActualTheme == Microsoft.UI.Xaml.ElementTheme.Dark)
        {
            return true;
        }

        return false;
    }
}
