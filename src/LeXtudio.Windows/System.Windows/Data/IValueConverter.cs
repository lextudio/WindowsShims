namespace System.Windows.Data;

// WPF-shaped IValueConverter (uses CultureInfo). Distinct from
// Microsoft.UI.Xaml.Data.IValueConverter (which uses 'string language'),
// so upstream WPF converters compile against this shim without modification.
public interface IValueConverter
{
    object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture);
    object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture);
}
