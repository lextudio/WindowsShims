using System.Collections.Generic;
using System.Linq;

namespace System.Windows.Media
{
    public static class FontCompatibilityExtensions
    {
        extension(FontFamily fontFamily)
        {
            public IEnumerable<string> FamilyNames
            {
                get => string.IsNullOrEmpty(fontFamily?.Source)
                    ? Enumerable.Empty<string>()
                    : new[] { fontFamily.Source };
            }
        }
    }
}

namespace System.Windows
{
    public static class FontWeightCompatibilityExtensions
    {
        extension(FontWeight fontWeight)
        {
            public static FontWeight FromOpenTypeWeight(int weightValue)
            {
                return new FontWeight { Weight = (ushort)weightValue };
            }

            public int ToOpenTypeWeight()
            {
                return fontWeight.Weight;
            }
        }
    }
}
