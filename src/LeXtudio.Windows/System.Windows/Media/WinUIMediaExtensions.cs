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
        public bool IsAncestorOf(DependencyObject descendant)
        {
            Microsoft.UI.Xaml.DependencyObject? current = descendant;

            while (current != null)
            {
                if (ReferenceEquals(current, uiElement))
                {
                    return true;
                }

                current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        public bool IsDescendantOf(Microsoft.UI.Xaml.UIElement ancestor) => ancestor.IsAncestorOf(uiElement);

        public GeneralTransform TransformToAncestor(Microsoft.UI.Xaml.UIElement ancestor) => new WinUIGeneralTransform(uiElement.TransformToVisual(ancestor));
    }
}

// PointHitTestResult: WPF's System.Windows.Media.PointHitTestResult. Not available in WinUI;
// used as base class for AdornerHitTestResult and by AdornerLayer.AdornerHitTest.
public class PointHitTestResult
{
    public PointHitTestResult(Visual visual, Point pt) { VisualHit = visual; PointHit = pt; }
    public Visual VisualHit { get; }
    public Point PointHit { get; }
}

public abstract class GeneralTransform
{
    public abstract Rect TransformBounds(Rect rect);
    public abstract Point Transform(Point point);
    public virtual bool TryTransform(Point inPoint, out Point result) { result = Transform(inPoint); return true; }
    public virtual GeneralTransform Inverse => new IdentityGeneralTransform();
}

internal sealed class IdentityGeneralTransform : GeneralTransform
{
    public override Rect TransformBounds(Rect rect) => rect;
    public override Point Transform(Point point) => point;
}

internal sealed class WinUIGeneralTransform(Microsoft.UI.Xaml.Media.GeneralTransform transform) : GeneralTransform
{
    public override Rect TransformBounds(Rect rect) => transform.TransformBounds(rect);
    public override Point Transform(Point point) => transform.TransformPoint(point);
}
