namespace System.Windows.Media
{
    /// <summary>Portable shim for System.Windows.Media.FormattedText.</summary>
    public class FormattedText
    {
        readonly double _emSize;

        public FormattedText(string textToFormat, System.Globalization.CultureInfo culture, FlowDirection flowDirection,
            Typeface typeface, double emSize, Brush foreground)
        {
            _emSize = emSize > 0 ? emSize : 12.0;
            double charCount = (textToFormat ?? string.Empty).Length;
            Width = charCount * _emSize * 0.6;
            WidthIncludingTrailingWhitespace = Width;
            Height = _emSize * 1.4;
            Baseline = _emSize * 1.1;
        }

        public double Width { get; }
        public double Height { get; }
        public double Baseline { get; }
        public double WidthIncludingTrailingWhitespace { get; }
    }
}
