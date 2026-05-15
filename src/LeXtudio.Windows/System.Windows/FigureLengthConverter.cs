using System.ComponentModel;
using System.Globalization;

namespace System.Windows;

public class FigureLengthConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || sourceType == typeof(double) || base.CanConvertFrom(context, sourceType);

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
        destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        culture ??= CultureInfo.CurrentCulture;

        return value switch
        {
            double number => new FigureLength(number),
            string text => FromString(text, culture),
            _ => base.ConvertFrom(context, culture, value),
        };
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is FigureLength figureLength)
        {
            return ToString(figureLength, culture ?? CultureInfo.CurrentCulture);
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }

    internal static string ToString(FigureLength value, CultureInfo culture)
    {
        if (value.IsAuto)
        {
            return "Auto";
        }

        string number = value.Value.ToString(culture);
        return value.FigureUnitType switch
        {
            FigureUnitType.Column => number + "Column",
            FigureUnitType.Content => number + "Content",
            FigureUnitType.Page => number + "Page",
            _ => number,
        };
    }

    private static FigureLength FromString(string text, CultureInfo culture)
    {
        text = text.Trim();
        if (string.Equals(text, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            return new FigureLength(0, FigureUnitType.Auto);
        }

        return new FigureLength(double.Parse(text, NumberStyles.Float, culture));
    }
}

