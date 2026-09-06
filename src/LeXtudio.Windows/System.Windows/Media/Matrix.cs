namespace System.Windows.Media
{
    public struct Matrix
    {
        public double M11 { get; set; }
        public double M12 { get; set; }
        public double M21 { get; set; }
        public double M22 { get; set; }
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }

        public static Matrix Identity => new Matrix { M11 = 1, M22 = 1 };

        public readonly bool IsIdentity
            => M11 == 1 && M12 == 0 && M21 == 0 && M22 == 1 && OffsetX == 0 && OffsetY == 0;

        public readonly global::Windows.Foundation.Point Transform(global::Windows.Foundation.Point point)
            => new(point.X * M11 + point.Y * M21 + OffsetX, point.X * M12 + point.Y * M22 + OffsetY);

        /// <summary>Concatenation: applies <paramref name="left"/> first, then <paramref name="right"/>.</summary>
        public static Matrix Multiply(Matrix left, Matrix right) => new()
        {
            M11 = left.M11 * right.M11 + left.M12 * right.M21,
            M12 = left.M11 * right.M12 + left.M12 * right.M22,
            M21 = left.M21 * right.M11 + left.M22 * right.M21,
            M22 = left.M21 * right.M12 + left.M22 * right.M22,
            OffsetX = left.OffsetX * right.M11 + left.OffsetY * right.M21 + right.OffsetX,
            OffsetY = left.OffsetX * right.M12 + left.OffsetY * right.M22 + right.OffsetY,
        };

        public static Matrix operator *(Matrix left, Matrix right) => Multiply(left, right);
    }
}
