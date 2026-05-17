#if WINUI_BRIDGE
namespace System.Windows;

// Stub for WPF's IETF language tag converter (PresentationCore).
// Returns CanConvertTo=false so DPTypeDescriptorContext skips CultureInfo serialization.
internal class CultureInfoIetfLanguageTagConverter : System.ComponentModel.TypeConverter
{
    public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext? context, Type? destinationType)
        => false;
}
#endif
