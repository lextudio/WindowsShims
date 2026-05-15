namespace System.Windows.Media
{
    /// <summary>
    /// Compiler shim for System.Windows.Media.DrawingContext.
    /// On Uno the actual drawing is performed through Uno's Skia canvas; this compatibility
    /// object records draw operations so shared rendering code keeps a meaningful contract.
    /// </summary>
    public class DrawingContext : IDisposable
    {
        readonly Collections.Generic.List<object> _operations = new Collections.Generic.List<object>();

        public sealed class DrawOperation
        {
            public DrawOperation(string kind, object? payload)
            {
                Kind = kind ?? string.Empty;
                Payload = payload;
            }

            public string Kind { get; }
            public object? Payload { get; }
        }

        public Collections.Generic.IReadOnlyList<object> Operations => _operations;

        public bool IsDisposed { get; private set; }

        public virtual void Record(string kind, object? payload)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(DrawingContext));
            _operations.Add(new DrawOperation(kind, payload));
        }

        public virtual void DrawText(FormattedText formattedText, Point origin)
            => Record("draw-text", new { FormattedText = formattedText, Origin = origin });

        public virtual void DrawRectangle(Brush brush, Pen pen, Rect rect)
            => Record("draw-rectangle", new { Brush = brush, Pen = pen, Rect = rect });

        public virtual void DrawLine(Pen pen, Point point0, Point point1)
            => Record("draw-line", new { Pen = pen, Point0 = point0, Point1 = point1 });

        public virtual void DrawGeometry(Brush brush, Pen pen, object geometry)
            => Record("draw-geometry", new { Brush = brush, Pen = pen, Geometry = geometry });

        public virtual void DrawRoundedRectangle(Brush brush, Pen pen, Rect rect, double radiusX, double radiusY)
            => Record("draw-rounded-rectangle", new { Brush = brush, Pen = pen, Rect = rect, RadiusX = radiusX, RadiusY = radiusY });

        public virtual void PushOpacity(double opacity)
            => Record("push-opacity", opacity);

        public virtual void Pop()
            => Record("pop", null);

        public virtual void Dispose()
        {
            IsDisposed = true;
        }
    }
}
