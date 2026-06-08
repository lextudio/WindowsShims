using System.Windows;
using System.Windows.Controls;

namespace MS.Internal.Controls;

// WPF-internal helper used by HeaderedItemsControl / HeaderedContentControl to validate that
// both a Template and a TemplateSelector are not set simultaneously. On HAS_UNO we simply no-op
// the validation — the XAML runtime will behave sensibly either way.
internal static class Helper
{
    internal static void CheckTemplateAndTemplateSelector(
        string name,
        DependencyProperty templateProperty,
        DependencyProperty templateSelectorProperty,
        DependencyObject d) { }

    internal static bool HasDefaultValue(DependencyObject d, DependencyProperty dp)
        => d.ReadLocalValue(dp) == Microsoft.UI.Xaml.DependencyProperty.UnsetValue;
}
