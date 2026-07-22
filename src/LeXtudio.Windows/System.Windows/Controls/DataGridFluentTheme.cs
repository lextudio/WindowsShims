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
        => Solid(element, 0xB3, 0xFF, 0xFF, 0xFF, 0x0F, 0xFF, 0xFF, 0xFF);

    internal static Microsoft.UI.Xaml.Media.Brush HeaderBackground
        => HeaderBackgroundFor(null);

    internal static Microsoft.UI.Xaml.Media.Brush HeaderBackgroundFor(Microsoft.UI.Xaml.FrameworkElement? element)
        => Solid(element, 0x00, 0xFF, 0xFF, 0xFF, 0x00, 0xFF, 0xFF, 0xFF);

    internal static Microsoft.UI.Xaml.Media.Brush RowHeaderBackground
        => RowHeaderBackgroundFor(null);

    internal static Microsoft.UI.Xaml.Media.Brush RowHeaderBackgroundFor(Microsoft.UI.Xaml.FrameworkElement? element)
        => Solid(element, 0x06, 0x00, 0x00, 0x00, 0x0A, 0xFF, 0xFF, 0xFF);

    internal static Microsoft.UI.Xaml.Media.Brush GridLine
        => GridLineFor(null);

    internal static Microsoft.UI.Xaml.Media.Brush GridLineFor(Microsoft.UI.Xaml.FrameworkElement? element)
        => Solid(element, 0x29, 0x00, 0x00, 0x00, 0x18, 0xFF, 0xFF, 0xFF);

    internal static Microsoft.UI.Xaml.Media.Brush OuterBorder
        => OuterBorderFor(null);

    internal static Microsoft.UI.Xaml.Media.Brush OuterBorderFor(Microsoft.UI.Xaml.FrameworkElement? element)
        => Solid(element, 0x0F, 0x00, 0x00, 0x00, 0x12, 0xFF, 0xFF, 0xFF);

    internal static Microsoft.UI.Xaml.Media.Brush PrimaryText
        => PrimaryTextFor(null);

    internal static Microsoft.UI.Xaml.Media.Brush PrimaryTextFor(Microsoft.UI.Xaml.FrameworkElement? element)
        => Solid(element, 0xE4, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF);

    internal static Microsoft.UI.Xaml.Media.Brush SecondaryText
        => SecondaryTextFor(null);

    internal static Microsoft.UI.Xaml.Media.Brush SecondaryTextFor(Microsoft.UI.Xaml.FrameworkElement? element)
        => Solid(element, 0x9E, 0x00, 0x00, 0x00, 0xC5, 0xFF, 0xFF, 0xFF);

    internal static Microsoft.UI.Xaml.Media.Brush Selection
        => SelectionFor(null);

    internal static Microsoft.UI.Xaml.Media.Brush SelectionFor(Microsoft.UI.Xaml.FrameworkElement? element)
        => Accent(element);

    internal static Microsoft.UI.Xaml.Media.Brush SelectionForegroundFor(Microsoft.UI.Xaml.FrameworkElement? element)
        => Solid(element, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00);

    internal static Microsoft.UI.Xaml.Media.Brush RowHover
        => RowHoverFor(null);

    internal static Microsoft.UI.Xaml.Media.Brush RowHoverFor(Microsoft.UI.Xaml.FrameworkElement? element)
        => Solid(element, 0x09, 0x00, 0x00, 0x00, 0x0F, 0xFF, 0xFF, 0xFF);

    internal static Microsoft.UI.Xaml.Media.Brush AlternatingRow
        => AlternatingRowFor(null);

    internal static Microsoft.UI.Xaml.Media.Brush AlternatingRowFor(Microsoft.UI.Xaml.FrameworkElement? element)
        => Solid(element, 0x09, 0x00, 0x00, 0x00, 0x0F, 0xFF, 0xFF, 0xFF);

    private static Microsoft.UI.Xaml.Media.Brush Solid(
        Microsoft.UI.Xaml.FrameworkElement? element,
        byte lightAlpha,
        byte lightRed,
        byte lightGreen,
        byte lightBlue,
        byte darkAlpha,
        byte darkRed,
        byte darkGreen,
        byte darkBlue)
    {
        var dark = IsDark(element);
        return new Microsoft.UI.Xaml.Media.SolidColorBrush(
            dark
                ? global::Windows.UI.Color.FromArgb(darkAlpha, darkRed, darkGreen, darkBlue)
                : global::Windows.UI.Color.FromArgb(lightAlpha, lightRed, lightGreen, lightBlue));
    }

    private static Microsoft.UI.Xaml.Media.Brush Accent(Microsoft.UI.Xaml.FrameworkElement? element)
    {
        var dark = IsDark(element);
        var key = dark ? "SystemAccentColorLight3" : "SystemAccentColorDark1";
        var fallback = dark
            ? global::Windows.UI.Color.FromArgb(0xFF, 0x99, 0xEB, 0xFF)
            : global::Windows.UI.Color.FromArgb(0xFF, 0x00, 0x5A, 0x9E);
        var color = Microsoft.UI.Xaml.Application.Current?.Resources.TryGetValue(key, out var value) == true
            ? value switch
            {
                global::Windows.UI.Color resourceColor => resourceColor,
                Microsoft.UI.Xaml.Media.SolidColorBrush resourceBrush => resourceBrush.Color,
                _ => fallback,
            }
            : fallback;

        return new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
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
