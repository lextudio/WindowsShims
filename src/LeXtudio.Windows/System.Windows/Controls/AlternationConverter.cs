using System.Globalization;
using System.Windows.Data;

namespace System.Windows.Controls;

public sealed class AlternationConverter : List<object?>, IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (Count == 0)
        {
            return null;
        }

        var index = value is IConvertible convertible
            ? convertible.ToInt32(CultureInfo.InvariantCulture)
            : 0;
        return this[Math.Abs(index) % Count];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
