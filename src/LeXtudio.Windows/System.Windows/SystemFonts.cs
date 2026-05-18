namespace System.Windows;

public static class SystemFonts
{
    public static Microsoft.UI.Xaml.Media.FontFamily MessageFontFamily =>
        new Microsoft.UI.Xaml.Media.FontFamily("Segoe UI");
    public static global::Windows.UI.Text.FontStyle MessageFontStyle =>
        global::Windows.UI.Text.FontStyle.Normal;
    public static global::Windows.UI.Text.FontWeight MessageFontWeight =>
        new global::Windows.UI.Text.FontWeight { Weight = 400 };
    public static double MessageFontSize => 14d;
    public static double ThemeMessageFontSize => 14d;
}
