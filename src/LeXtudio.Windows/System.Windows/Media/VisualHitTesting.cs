// WPF visual hit-testing, over WinUI's element hit-testing.
//
// WPF's VisualTreeHelper.HitTest walks the retained-mode visual tree and reports the
// topmost Visual under a point. WinUI has no visual layer to walk; the nearest equivalent
// is FindElementsInHostCoordinates, which reports the elements under a point in the
// window's coordinate space. The point is transformed from the reference element into
// those coordinates first, so callers can keep passing element-relative points.

namespace System.Windows.Media
{
    /// <summary>The topmost element under a hit-test point.</summary>
    public class HitTestResult
    {
        public HitTestResult(Microsoft.UI.Xaml.DependencyObject? visualHit) => VisualHit = visualHit;

        public Microsoft.UI.Xaml.DependencyObject? VisualHit { get; }
    }

    public static class VisualHitTestingExtensions
    {
        extension(Microsoft.UI.Xaml.Media.VisualTreeHelper)
        {
            /// <summary>
            /// The topmost element under <paramref name="point"/>, which is relative to
            /// <paramref name="reference"/>. Returns null when nothing is hit.
            /// </summary>
            public static HitTestResult? HitTest(Microsoft.UI.Xaml.UIElement? reference, Point point)
            {
                if (reference is null)
                {
                    return null;
                }

                var xamlRoot = reference.XamlRoot;
                if (xamlRoot is null)
                {
                    // Not in a live tree: nothing can be hit.
                    return null;
                }

                Point hostPoint;
                try
                {
                    hostPoint = reference.TransformToVisual(null).TransformPoint(point);
                }
                catch (InvalidOperationException)
                {
                    // TransformToVisual throws when the element is not connected.
                    return null;
                }

                foreach (var element in Microsoft.UI.Xaml.Media.VisualTreeHelper
                    .FindElementsInHostCoordinates(hostPoint, reference))
                {
                    return new HitTestResult(element);
                }

                return null;
            }
        }
    }
}
