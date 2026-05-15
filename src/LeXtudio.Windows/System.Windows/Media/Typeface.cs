namespace System.Windows.Media
{
    /// <summary>Portable shim for System.Windows.Media.Typeface.</summary>
    public sealed class Typeface
    {
        public Typeface(string familyName)
        {
            FontFamily = new FontFamily(familyName ?? string.Empty);
            Style = FontStyle.Normal;
            Weight = System.Windows.FontWeights.Normal;
            Stretch = Windows.FontStretch.Normal;
        }

        public Typeface(FontFamily fontFamily, FontStyle style, FontWeight weight, Windows.FontStretch stretch)
        {
            FontFamily = fontFamily ?? new FontFamily(string.Empty);
            Style = style;
            Weight = weight;
            Stretch = stretch;
        }

        public FontFamily FontFamily { get; }
        public FontStyle Style { get; }
        public FontWeight Weight { get; }
        public Windows.FontStretch Stretch { get; }

        public override string ToString() => FontFamily.ToString();
    }
}
