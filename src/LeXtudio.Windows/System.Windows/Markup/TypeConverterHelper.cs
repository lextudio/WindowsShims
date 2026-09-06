// Upstream Shared/System/Windows/Markup/TypeConverterHelper.cs cannot be linked: its
// converter registry pulls in ReflectionHelper, TypeTypeConverter, DateTimeConverter2 and
// System.Xaml, none of which exist on Uno. The fixed-document sources only ever read
// InvariantEnglishUS, so that is all this file carries.

using System.Globalization;

namespace System.Windows.Markup
{
    internal static class TypeConverterHelper
    {
        /// <summary>
        /// The culture WPF parses and formats markup values with. Upstream builds it from
        /// the invariant culture, which is what markup round-tripping depends on.
        /// </summary>
        internal static CultureInfo InvariantEnglishUS { get; } = CultureInfo.InvariantCulture;
    }
}
