namespace System.Windows.Media
{
    /// <summary>Portable shim for System.Windows.Media.Color.</summary>
    public readonly struct Color : IEquatable<Color>
    {
        public byte A { get; }
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        Color(byte a, byte r, byte g, byte b) { A = a; R = r; G = g; B = b; }

        public static Color FromArgb(byte a, byte r, byte g, byte b) => new Color(a, r, g, b);
        public static Color FromRgb(byte r, byte g, byte b) => new Color(255, r, g, b);

        public bool Equals(Color other) => A == other.A && R == other.R && G == other.G && B == other.B;
        public override bool Equals(object? obj) => obj is Color c && Equals(c);
        public override int GetHashCode() => HashCode.Combine(A, R, G, B);
        public static bool operator ==(Color a, Color b) => a.Equals(b);
        public static bool operator !=(Color a, Color b) => !a.Equals(b);
        public static implicit operator global::Windows.UI.Color(Color color) => global::Windows.UI.Color.FromArgb(color.A, color.R, color.G, color.B);
        public override string ToString() =>
            A == 255
                ? string.Format(Globalization.CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", R, G, B)
                : string.Format(Globalization.CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}{3:X2}", A, R, G, B);
    }
}
