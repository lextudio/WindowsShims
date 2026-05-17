namespace System.Windows.Media;

public static class WinUIMediaExtensions
{
    extension(Color)
    {
        public static Color FromRgb(byte r, byte g, byte b) => Color.FromArgb(0xFF, r, g, b);
    }

    extension(Geometry geometry)
    {
        public bool FillContains(Point point) => false;
    }

    extension(Transform transform)
    {
        public bool IsIdentity => false;
    }

    extension(FontFamily family)
    {
        public double LineSpacing => 1.0;
    }
}
