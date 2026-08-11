// Uno-specific value-to-string conversion for DPTypeDescriptorContext.
//
// WinRT-aliased structs (FontWeight, FontStyle, FontStretch) and the shim's
// WinUI-backed property types don't carry [TypeConverter] attributes that
// TypeDescriptor picks up, so the upstream WPF path (GetConverter +
// CanConvertTo(string)) reports failure. This partial supplies the converter
// routing; the linked upstream file calls it before falling back to
// TypeDescriptor. Kept out of the linked file so the linked file stays
// pristine (guard budget: scripts/count-guards.py).

namespace System.Windows.Documents;

internal partial class DPTypeDescriptorContext
{
    /// <summary>
    /// Attempts to produce the WPF string form for a property value whose
    /// TypeConverter is not visible to TypeDescriptor under Uno.
    /// </summary>
    private static bool TryGetShimStringValue(
        DependencyProperty property,
        object propertyValue,
        out string? stringValue)
    {
        if (property == TextElement.FontWeightProperty)
        {
            stringValue = new System.Windows.Media.FontWeightConverter().ConvertToInvariantString(propertyValue);
            return true;
        }
        if (property == TextElement.FontStyleProperty)
        {
            stringValue = new System.Windows.Media.FontStyleConverter().ConvertToInvariantString(propertyValue);
            return true;
        }
        if (property == TextElement.FontStretchProperty)
        {
            stringValue = ((FontStretch)propertyValue).ToString();
            return true;
        }
        if (property == TextElement.FontFamilyProperty)
        {
            stringValue = propertyValue is Microsoft.UI.Xaml.Media.FontFamily ff
                ? ff.Source ?? string.Empty
                : null;
            return true;
        }
        if (property == TextElement.ForegroundProperty
            || property == TextElement.BackgroundProperty
            || property == Block.BorderBrushProperty
            || property == ListItem.BorderBrushProperty)
        {
            if (propertyValue is Microsoft.UI.Xaml.Media.SolidColorBrush scb)
            {
                stringValue = FormatColor(scb.Color);
                return true;
            }
        }
        if (property.PropertyType == typeof(Thickness))
        {
            // The Thickness struct's [TypeConverter] attribute is not picked
            // up by TypeDescriptor under Uno, so the default converter returns
            // ToString() ("[Thickness: ...]") instead of "left,top,right,bottom".
            // Emit the comma-separated form that WriteXaml consumers
            // (e.g. XamlToRtfWriter.ConvertToThickness) expect.
            if (propertyValue is Thickness thickness)
            {
                var culture = System.Globalization.CultureInfo.InvariantCulture;
                stringValue = string.Join(",",
                    thickness.Left.ToString(culture),
                    thickness.Top.ToString(culture),
                    thickness.Right.ToString(culture),
                    thickness.Bottom.ToString(culture));
                return true;
            }
        }
        if (property == TableColumn.WidthProperty)
        {
            // WinUI's GridLength converter emits "100px"; WPF's
            // GridLengthConverter emits the bare number for Pixel/Star
            // ("100", "100*") and "Auto". Emit the WPF form so
            // XamlToRtfWriter.ConvertToX / StringToDouble can parse it.
            if (propertyValue is GridLength gridLength)
            {
                if (gridLength.IsAuto)
                {
                    stringValue = "Auto";
                }
                else if (gridLength.IsStar)
                {
                    stringValue = gridLength.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + "*";
                }
                else
                {
                    stringValue = gridLength.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                return true;
            }
        }
        stringValue = null;
        return false;
    }

    private static string FormatColor(global::Windows.UI.Color color)
    {
        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
