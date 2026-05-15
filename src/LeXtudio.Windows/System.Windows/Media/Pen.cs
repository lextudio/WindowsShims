namespace System.Windows.Media
{
    /// <summary>Portable shim for System.Windows.Media.Pen (brush + thickness).</summary>
    public sealed class Pen
    {
        public Pen() { }
        public Pen(Brush brush, double thickness) { Brush = brush; Thickness = thickness; }
        public Brush Brush { get; set; }
        public double Thickness { get; set; }
    }
}
