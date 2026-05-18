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

    extension(Microsoft.UI.Xaml.UIElement uiElement)
    {
        public GeneralTransform TransformToAncestor(Visual ancestor) => new IdentityGeneralTransform();
    }
}

public abstract class GeneralTransform
{
    public abstract Rect TransformBounds(Rect rect);
    public abstract Point Transform(Point point);
}

internal sealed class IdentityGeneralTransform : GeneralTransform
{
    public override Rect TransformBounds(Rect rect) => rect;
    public override Point Transform(Point point) => point;
}
