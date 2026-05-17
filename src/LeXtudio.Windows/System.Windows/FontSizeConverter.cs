#if WINUI_BRIDGE
using System;
using System.ComponentModel;
using System.Globalization;

namespace System.Windows
{
    public sealed class FontSizeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || sourceType == typeof(double) || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is double d)
            {
                return d;
            }

            if (value is string s && double.TryParse(s, NumberStyles.Float, culture ?? CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}
#endif
