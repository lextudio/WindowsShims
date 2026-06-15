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

        public readonly global::Windows.Foundation.Point Transform(global::Windows.Foundation.Point point)
            => new(point.X * M11 + point.Y * M21 + OffsetX, point.X * M12 + point.Y * M22 + OffsetY);
    }
}
