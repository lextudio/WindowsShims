namespace System.Windows.Controls;

// Fluent visual tokens adapted from WCT v7's DataGrid.xaml. Keep these in one
// place because the shim's runtime ControlTemplates cannot reliably resolve a
// library Generic.xaml from every Uno consumer.
internal static class DataGridFluentTheme
{
    internal static Microsoft.UI.Xaml.Media.Brush GridBackground
        => Resolve("CardBackgroundFillColorDefaultBrush", 0xFF, 0xFF, 0xFF, 0xFF);

    internal static Microsoft.UI.Xaml.Media.Brush HeaderBackground
        => Resolve("SubtleFillColorSecondaryBrush", 0xFF, 0xF3, 0xF3, 0xF3);

    internal static Microsoft.UI.Xaml.Media.Brush RowHeaderBackground
        => Resolve("SubtleFillColorTertiaryBrush", 0xFF, 0xF8, 0xF8, 0xF8);

    internal static Microsoft.UI.Xaml.Media.Brush GridLine
        => Resolve("ControlStrokeColorSecondaryBrush", 0x24, 0x00, 0x00, 0x00);

    internal static Microsoft.UI.Xaml.Media.Brush OuterBorder
        => Resolve("ControlStrokeColorDefaultBrush", 0x33, 0x00, 0x00, 0x00);

    internal static Microsoft.UI.Xaml.Media.Brush PrimaryText
        => Resolve("TextFillColorPrimaryBrush", 0xE4, 0x00, 0x00, 0x00);

    internal static Microsoft.UI.Xaml.Media.Brush SecondaryText
        => Resolve("TextFillColorSecondaryBrush", 0x9E, 0x00, 0x00, 0x00);

    internal static Microsoft.UI.Xaml.Media.Brush Selection
        => Resolve("AccentFillColorSecondaryBrush", 0x33, 0x00, 0x78, 0xD4);

    internal static Microsoft.UI.Xaml.Media.Brush RowHover
        => Resolve("SubtleFillColorSecondaryBrush", 0x0F, 0x00, 0x00, 0x00);

    internal static Microsoft.UI.Xaml.Media.Brush AlternatingRow
        => Resolve("SubtleFillColorTransparentBrush", 0x08, 0x00, 0x00, 0x00);

    private static Microsoft.UI.Xaml.Media.Brush Resolve(
        string key,
        byte alpha,
        byte red,
        byte green,
        byte blue)
    {
        if (Microsoft.UI.Xaml.Application.Current?.Resources.TryGetValue(key, out var value) == true
            && value is Microsoft.UI.Xaml.Media.Brush brush)
        {
            return brush;
        }

        return new Microsoft.UI.Xaml.Media.SolidColorBrush(
            global::Windows.UI.Color.FromArgb(alpha, red, green, blue));
    }
}
