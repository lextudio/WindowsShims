// WPF value-type surface that Windows.Foundation.Rect and the WinUI transform/geometry
// types do not carry. Shared across the port, not specific to one control family.
//
// Windows.Foundation.Rect is a struct, so the mutating WPF members (Offset, Inflate,
// Transform, Scale) are extension members on a `ref` receiver, matching WPF's semantics
// of mutating the rect in place.

using Microsoft.UI.Xaml.Media;

namespace System.Windows.Media;

public static class RectExtensions
{
    extension(ref Rect rect)
    {
        /// <summary>Moves the rect by the given offset, in place.</summary>
        public void Offset(double offsetX, double offsetY)
        {
            if (rect.IsEmpty)
            {
                return;
            }

            rect.X += offsetX;
            rect.Y += offsetY;
        }

        /// <summary>Moves the rect by the given vector, in place.</summary>
        public void Offset(Vector offsetVector) => rect.Offset(offsetVector.X, offsetVector.Y);

        /// <summary>
        /// Grows the rect by <paramref name="width"/> on the left and right and
        /// <paramref name="height"/> on the top and bottom, in place. Negative values
        /// shrink it; shrinking past zero collapses the rect to empty, as in WPF.
        /// </summary>
        public void Inflate(double width, double height)
        {
            if (rect.IsEmpty)
            {
                return;
            }

            var x = rect.X - width;
            var y = rect.Y - height;
            var w = rect.Width + width + width;
            var h = rect.Height + height + height;

            if (w < 0 || h < 0)
            {
                rect = Rect.Empty;
                return;
            }

            rect = new Rect(x, y, w, h);
        }

        /// <summary>Grows the rect by a size, in place.</summary>
        public void Inflate(Size size) => rect.Inflate(size.Width, size.Height);

        /// <summary>
        /// Replaces the rect with the axis-aligned bounding box of its four corners
        /// mapped through <paramref name="matrix"/>, in place.
        /// </summary>
        public void Transform(Matrix matrix)
        {
            if (rect.IsEmpty)
            {
                return;
            }

            rect = RectExtensions.TransformBounds(rect, matrix);
        }

        /// <summary>Scales the rect about the origin, in place. Negative factors flip it.</summary>
        public void Scale(double scaleX, double scaleY)
        {
            if (rect.IsEmpty)
            {
                return;
            }

            var x = rect.X * scaleX;
            var y = rect.Y * scaleY;
            var w = rect.Width * scaleX;
            var h = rect.Height * scaleY;

            if (w < 0)
            {
                x += w;
                w = -w;
            }

            if (h < 0)
            {
                y += h;
                h = -h;
            }

            rect = new Rect(x, y, w, h);
        }
    }

    extension(Rect rect)
    {
        public Point TopLeft => new(rect.Left, rect.Top);

        public Point TopRight => new(rect.Right, rect.Top);

        public Point BottomLeft => new(rect.Left, rect.Bottom);

        public Point BottomRight => new(rect.Right, rect.Bottom);

        /// <summary>True when <paramref name="other"/> lies entirely inside this rect.</summary>
        public bool Contains(Rect other)
        {
            if (rect.IsEmpty || other.IsEmpty)
            {
                return false;
            }

            return rect.Left <= other.Left
                && rect.Top <= other.Top
                && rect.Right >= other.Right
                && rect.Bottom >= other.Bottom;
        }

        /// <summary>True when the two rects share any area. Empty rects intersect nothing.</summary>
        public bool IntersectsWith(Rect other)
        {
            if (rect.IsEmpty || other.IsEmpty)
            {
                return false;
            }

            return other.Left <= rect.Right
                && other.Right >= rect.Left
                && other.Top <= rect.Bottom
                && other.Bottom >= rect.Top;
        }
    }

    /// <summary>The axis-aligned bounding box of <paramref name="rect"/> mapped through a matrix.</summary>
    public static Rect TransformBounds(Rect rect, Matrix matrix)
    {
        if (rect.IsEmpty)
        {
            return rect;
        }

        var p1 = matrix.Transform(new Point(rect.Left, rect.Top));
        var p2 = matrix.Transform(new Point(rect.Right, rect.Top));
        var p3 = matrix.Transform(new Point(rect.Left, rect.Bottom));
        var p4 = matrix.Transform(new Point(rect.Right, rect.Bottom));

        var left = Math.Min(Math.Min(p1.X, p2.X), Math.Min(p3.X, p4.X));
        var top = Math.Min(Math.Min(p1.Y, p2.Y), Math.Min(p3.Y, p4.Y));
        var right = Math.Max(Math.Max(p1.X, p2.X), Math.Max(p3.X, p4.X));
        var bottom = Math.Max(Math.Max(p1.Y, p2.Y), Math.Max(p3.Y, p4.Y));

        return new Rect(left, top, right - left, bottom - top);
    }

    /// <summary>WPF's <c>new Rect(Size)</c> — a rect at the origin with the given size.</summary>
    public static Rect FromSize(Size size) => size.IsEmpty ? Rect.Empty : new Rect(new Point(), size);
}

public static class WinUITransformExtensions
{
    extension(Transform)
    {
        /// <summary>WPF's <c>Transform.Identity</c>.</summary>
        public static Transform Identity => new MatrixTransform { Matrix = Matrix.Identity.ToWinUI() };
    }

    extension(Transform transform)
    {
        /// <summary>WPF's <c>Transform.Value</c> — the transform as a WPF matrix.</summary>
        public Matrix Value => transform switch
        {
            MatrixTransform matrixTransform => matrixTransform.Matrix.ToWpf(),
            null => Matrix.Identity,
            _ => transform.TransformPointsToMatrix(),
        };
    }

    extension(System.Windows.Media.GeneralTransform generalTransform)
    {
        /// <summary>
        /// WPF's <c>GeneralTransform.AffineTransform</c>, which returns null for a
        /// non-affine transform. Every transform reachable through this shim is affine,
        /// so the receiver's own point mapping is sampled into a matrix.
        /// </summary>
        public Transform? AffineTransform
        {
            get
            {
                var origin = generalTransform.Transform(new Point(0, 0));
                var unitX = generalTransform.Transform(new Point(1, 0));
                var unitY = generalTransform.Transform(new Point(0, 1));

                return new MatrixTransform
                {
                    Matrix = new Microsoft.UI.Xaml.Media.Matrix(
                        unitX.X - origin.X,
                        unitX.Y - origin.Y,
                        unitY.X - origin.X,
                        unitY.Y - origin.Y,
                        origin.X,
                        origin.Y),
                };
            }
        }
    }

    /// <summary>
    /// Recovers a matrix from an arbitrary WinUI transform by mapping the origin and the
    /// two unit vectors. Correct for any affine transform, which is all WPF's Transform
    /// hierarchy produces.
    /// </summary>
    private static Matrix TransformPointsToMatrix(this Transform transform)
    {
        var origin = transform.TransformPoint(new Point(0, 0));
        var unitX = transform.TransformPoint(new Point(1, 0));
        var unitY = transform.TransformPoint(new Point(0, 1));

        return new Matrix
        {
            M11 = unitX.X - origin.X,
            M12 = unitX.Y - origin.Y,
            M21 = unitY.X - origin.X,
            M22 = unitY.Y - origin.Y,
            OffsetX = origin.X,
            OffsetY = origin.Y,
        };
    }

    /// <summary>Converts a WPF matrix to the WinUI one.</summary>
    public static Microsoft.UI.Xaml.Media.Matrix ToWinUI(this Matrix matrix)
        => new(matrix.M11, matrix.M12, matrix.M21, matrix.M22, matrix.OffsetX, matrix.OffsetY);

    /// <summary>Converts a WinUI matrix to the WPF one.</summary>
    public static Matrix ToWpf(this Microsoft.UI.Xaml.Media.Matrix matrix)
        => new()
        {
            M11 = matrix.M11,
            M12 = matrix.M12,
            M21 = matrix.M21,
            M22 = matrix.M22,
            OffsetX = matrix.OffsetX,
            OffsetY = matrix.OffsetY,
        };
}
