namespace System.Windows.Media
{
    /// <summary>Portable shim for System.Windows.Media.Typeface.</summary>
    public sealed class Typeface
    {
        public Typeface(string familyName)
        {
            FontFamily = new Windows.FontFamily(familyName ?? string.Empty);
            Style = Windows.FontStyle.Normal;
            Weight = Windows.FontWeight.Normal;
            Stretch = Windows.FontStretch.Normal;
        }

        public Typeface(Windows.FontFamily fontFamily, Windows.FontStyle style, Windows.FontWeight weight, Windows.FontStretch stretch)
        {
            FontFamily = fontFamily ?? new Windows.FontFamily(string.Empty);
            Style = style;
            Weight = weight;
            Stretch = stretch;
        }

        public Windows.FontFamily FontFamily { get; }
        public Windows.FontStyle Style { get; }
        public Windows.FontWeight Weight { get; }
        public Windows.FontStretch Stretch { get; }

        public override string ToString() => FontFamily.ToString();
    }
}
