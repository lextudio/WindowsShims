// WPF Geometry operations WinUI does not expose.
//
// WinUI's Geometry hierarchy is data only: it has Bounds and nothing else. WPF's
// PathGeometry.CreateFromGeometry, Geometry.Combine and Geometry.GetArea are all backed by
// MilCore's path engine, which has no cross-platform counterpart. These are bounding-box
// approximations, which is exact for the rectangular geometries the fixed-document
// hyperlink matching actually deals with and an over-estimate for anything curved.

using Microsoft.UI.Xaml.Media;

namespace System.Windows.Media
{
    public static class GeometryExtensions
    {
        extension(PathGeometry)
        {
            /// <summary>
            /// WPF's PathGeometry.CreateFromGeometry. A PathGeometry is returned unchanged;
            /// anything else becomes a single rectangular figure covering its bounds.
            /// </summary>
            public static PathGeometry CreateFromGeometry(Geometry? geometry)
            {
                if (geometry is PathGeometry path)
                {
                    return path;
                }

                var result = new PathGeometry();
                if (geometry is null)
                {
                    return result;
                }

                result.Figures.Add(RectangleFigure(geometry.Bounds));
                return result;
            }
        }

        extension(PathGeometry path)
        {
            /// <summary>
            /// WPF's PathGeometry.AddGeometry — folds another geometry's outline into this
            /// path. Figures are copied across from a PathGeometry; anything else
            /// contributes a rectangle covering its bounds.
            /// </summary>
            public void AddGeometry(Geometry? geometry)
            {
                if (geometry is null)
                {
                    return;
                }

                if (geometry is PathGeometry other)
                {
                    // Figures belong to one collection at a time, so copy rather than move.
                    foreach (var figure in other.Figures)
                    {
                        path.Figures.Add(figure);
                    }

                    return;
                }

                path.Figures.Add(RectangleFigure(geometry.Bounds));
            }
        }

        extension(Geometry)
        {
            /// <summary>
            /// WPF's Geometry.Combine. Only Intersect and Union are reachable from the
            /// fixed-document sources, and both are computed on bounding boxes.
            /// </summary>
            public static Geometry Combine(
                Geometry? geometry1,
                Geometry? geometry2,
                GeometryCombineMode mode,
                Transform? transform)
            {
                var first = geometry1?.Bounds ?? Rect.Empty;
                var second = geometry2?.Bounds ?? Rect.Empty;

                Rect combined;
                switch (mode)
                {
                    case GeometryCombineMode.Intersect:
                        combined = first;
                        combined.Intersect(second);
                        break;
                    case GeometryCombineMode.Union:
                        combined = first;
                        combined.Union(second);
                        break;
                    case GeometryCombineMode.Exclude:
                        combined = first;
                        break;
                    default:
                        combined = first;
                        combined.Union(second);
                        break;
                }

                var result = new RectangleGeometry { Rect = combined.IsEmpty ? default : combined };
                if (transform is not null)
                {
                    result.Transform = transform;
                }

                return result;
            }
        }

        extension(Geometry? geometry)
        {
            /// <summary>
            /// WPF's Geometry.GetArea — the area the geometry's fill covers. Computed from
            /// the bounding box, so it over-reports for a non-rectangular outline. The
            /// fixed-document caller compares two areas from the same source, so the ratio
            /// it tests stays meaningful.
            /// </summary>
            public double GetArea()
            {
                if (geometry is null)
                {
                    return 0.0;
                }

                var bounds = geometry.Bounds;
                return bounds.IsEmpty ? 0.0 : bounds.Width * bounds.Height;
            }
        }

        private static PathFigure RectangleFigure(Rect bounds)
        {
            var figure = new PathFigure
            {
                StartPoint = new Point(bounds.Left, bounds.Top),
                IsClosed = true,
                IsFilled = true,
            };

            var segment = new PolyLineSegment();
            segment.Points.Add(new Point(bounds.Right, bounds.Top));
            segment.Points.Add(new Point(bounds.Right, bounds.Bottom));
            segment.Points.Add(new Point(bounds.Left, bounds.Bottom));
            figure.Segments.Add(segment);

            return figure;
        }
    }
}
