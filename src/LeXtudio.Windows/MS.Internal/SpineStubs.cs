using System.Collections;

namespace MS.Internal
{
    // XML data sources (XmlDataProvider/XPath binding) are not supported by
    // the Uno bridge; every probe reports "not an XML node".
    internal static class SystemXmlHelper
    {
        internal static bool IsXmlNode(object? item) => false;

        internal static object FindXmlNodeWithInnerText(IEnumerable items, object? innerText, out int index)
        {
            index = -1;
            return DependencyProperty.UnsetValue;
        }

        internal static string? GetInnerText(object? item) => null;
    }

    internal static class FrameworkAppContextSwitches
    {
        // WPF compatibility quirk switches default to off on modern targets.
        internal static bool SelectionPropertiesCanLagBehindSelectionChangedEvent => false;

        internal static bool UseAdornerForTextboxSelectionRendering => false;
    }
}

namespace MS.Internal.Controls
{
    // Subset of WPF's generator-host contract: only the member the selector
    // spine calls through the interface cast.
    internal interface IGeneratorHost
    {
        bool IsItemItsOwnContainer(object item);
    }
}
