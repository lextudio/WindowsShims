using System.Windows;
using System.Windows.Controls;

namespace MS.Internal;

internal static class Helper
{
    internal static void InvalidateMeasureOnPath(
        DependencyObject pathStartElement,
        DependencyObject pathEndElement,
        bool duringMeasure,
        bool includePathEnd)
    {
        UIElement? start = pathStartElement as UIElement;
        if (start is not null)
        {
            start.InvalidateMeasure();
        }

        if (includePathEnd && pathEndElement is UIElement end && !ReferenceEquals(start, end))
        {
            end.InvalidateMeasure();
        }
    }

    internal static void CheckTemplateAndTemplateSelector(
        string name,
        DependencyProperty templateProperty,
        DependencyProperty templateSelectorProperty,
        DependencyObject d) { }

    internal static bool HasDefaultValue(DependencyObject d, DependencyProperty dp)
        => d.ReadLocalValue(dp) == Microsoft.UI.Xaml.DependencyProperty.UnsetValue;
}
