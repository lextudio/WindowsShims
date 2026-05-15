namespace System.Windows
{
    public interface IWeakEventListener
    {
        bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e);
    }

    /// <summary>Portable shim for System.Windows.FontStyles.</summary>
    public static class FontStyles
    {
        public static FontStyle Normal => global::Windows.UI.Text.FontStyle.Normal;
        public static FontStyle Italic => global::Windows.UI.Text.FontStyle.Italic;
        public static FontStyle Oblique => global::Windows.UI.Text.FontStyle.Oblique;
    }

    /// <summary>Portable shim for System.Windows.FontWeights (WPF static class).</summary>
    public static class FontWeights
    {
        public static FontWeight Thin       => new() { Weight = 100 };
        public static FontWeight ExtraLight => new() { Weight = 200 };
        public static FontWeight Light      => new() { Weight = 300 };
        public static FontWeight Normal     => new() { Weight = 400 };
        public static FontWeight Medium     => new() { Weight = 500 };
        public static FontWeight SemiBold   => new() { Weight = 600 };
        public static FontWeight Bold       => new() { Weight = 700 };
        public static FontWeight ExtraBold  => new() { Weight = 800 };
        public static FontWeight Black      => new() { Weight = 900 };
    }

    public enum BaselineAlignment
    {
        Top, Center, Baseline, Bottom, TextTop, TextBottom, Subscript, Superscript
    }

    public readonly struct Vector : IEquatable<Vector>
    {
        public double X { get; }
        public double Y { get; }

        public Vector(double x, double y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(Vector other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object? obj) => obj is Vector other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public static bool operator ==(Vector left, Vector right) => left.Equals(right);
        public static bool operator !=(Vector left, Vector right) => !left.Equals(right);
    }

    /// <summary>Compiler shim for DataObject clipboard transfer object.</summary>
    public class DataObject
    {
        readonly Collections.Generic.Dictionary<string, object> _data =
            new Collections.Generic.Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public DataObject() { }
        public DataObject(string format, object data) { _data[format] = data; }

        public void SetData(string format, object data) => _data[format] = data;
        public object GetData(string format) =>
            _data.TryGetValue(format, out object v) ? v : null;
        public bool GetDataPresent(string format) => _data.ContainsKey(format);
    }

    /// <summary>Compiler shim for DataFormats clipboard format constants.</summary>
    public static class DataFormats
    {
        public const string Text        = "Text";
        public const string UnicodeText = "UnicodeText";
        public const string OemText     = "OemText";
        public const string Rtf         = "Rtf";
        public const string Html        = "Html";
    }

    public class PresentationSource
    {
        public Media.CompositionTarget CompositionTarget { get; set; }

        public static PresentationSource FromVisual(Media.Visual visual)
        {
            return visual?.PresentationSource;
        }
    }

    /// <summary>Portable shim for System.Windows.FontStretch.</summary>
    public readonly struct FontStretch : IEquatable<FontStretch>
    {
        readonly int _stretch; // 1=UltraCondensed … 9=Normal … 9=UltraExpanded (OpenType scale)
        FontStretch(int stretch) { _stretch = stretch; }
        public static FontStretch FromOpenTypeStretch(int stretch) => new FontStretch(stretch);
        public int ToOpenTypeStretch() => _stretch;
        public bool Equals(FontStretch other) => _stretch == other._stretch;
        public override bool Equals(object? obj) => obj is FontStretch fs && Equals(fs);
        public override int GetHashCode() => _stretch;
        public static bool operator ==(FontStretch a, FontStretch b) => a._stretch == b._stretch;
        public static bool operator !=(FontStretch a, FontStretch b) => a._stretch != b._stretch;
        public override string ToString() => _stretch switch {
            1 => "UltraCondensed", 2 => "ExtraCondensed", 3 => "Condensed",
            4 => "SemiCondensed",  5 => "Normal",          6 => "SemiExpanded",
            7 => "Expanded",       8 => "ExtraExpanded",   9 => "UltraExpanded",
            _ => _stretch.ToString()
        };
        public static readonly FontStretch Normal = new FontStretch(5);
    }

    /// <summary>Portable shim for System.Windows.FontStretches.</summary>
    public static class FontStretches
    {
        public static FontStretch UltraCondensed => FontStretch.FromOpenTypeStretch(1);
        public static FontStretch ExtraCondensed => FontStretch.FromOpenTypeStretch(2);
        public static FontStretch Condensed      => FontStretch.FromOpenTypeStretch(3);
        public static FontStretch SemiCondensed  => FontStretch.FromOpenTypeStretch(4);
        public static FontStretch Normal         => FontStretch.FromOpenTypeStretch(5);
        public static FontStretch Medium         => FontStretch.FromOpenTypeStretch(5);
        public static FontStretch SemiExpanded   => FontStretch.FromOpenTypeStretch(6);
        public static FontStretch Expanded       => FontStretch.FromOpenTypeStretch(7);
        public static FontStretch ExtraExpanded  => FontStretch.FromOpenTypeStretch(8);
        public static FontStretch UltraExpanded  => FontStretch.FromOpenTypeStretch(9);
    }
}

namespace System.Windows
{
    public static class SystemColors
    {
        public static Brush ControlTextBrush => System.Windows.Media.Brushes.Black;
        public static Brush WindowTextBrush => System.Windows.Media.Brushes.Black;
        public static Brush HighlightTextBrush => System.Windows.Media.Brushes.White;
        public static Brush GrayTextBrush => System.Windows.Media.Brushes.Gray;
        public static Brush ControlBrush => System.Windows.Media.Brushes.LightGray;
        public static Brush WindowBrush => System.Windows.Media.Brushes.White;
        public static Brush HighlightBrush => new SolidColorBrush(System.Windows.Media.Colors.Blue);
    }
}
