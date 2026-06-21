using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using WinColor = Windows.UI.Color;

namespace System.Windows.Media
{
    /// <summary>
    /// A <see cref="DrawingContext"/> that actually paints, by translating each WPF-shaped draw
    /// call onto a Win2D <see cref="CanvasDrawingSession"/>. On Uno desktop the session is backed
    /// by SkiaSharp (LeXtudio.Win2D.UnoCompat); on WinUI 3 by real Win2D (DirectWrite). Shared
    /// editor render code (ported AvalonEdit, Florence) keeps calling DrawText/DrawRectangle/…
    /// unchanged and is handed one of these to render for real.
    /// </summary>
    public sealed class CanvasDrawingContext : DrawingContext
    {
        readonly CanvasDrawingSession _session;
        static readonly WinColor Transparent = WinColor.FromArgb(0, 0, 0, 0);

        public CanvasDrawingContext(CanvasDrawingSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public override void DrawText(FormattedText formattedText, Point origin)
        {
            base.DrawText(formattedText, origin); // keep the recorded operation log
            if (formattedText is null || string.IsNullOrEmpty(formattedText.Text))
                return;

            using CanvasTextFormat format = formattedText.CreateCanvasTextFormat();
            WinColor color = ToColor(formattedText.Foreground, WinColor.FromArgb(255, 0, 0, 0));
            _session.DrawText(formattedText.Text, (float)origin.X, (float)origin.Y, color, format);
        }

        public override void DrawRectangle(Brush brush, Pen pen, Rect rect)
        {
            base.DrawRectangle(brush, pen, rect);
            if (TryGetColor(brush, out WinColor fill))
                _session.FillRectangle((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height, fill);
            if (pen is not null && TryGetColor(pen.Brush, out WinColor stroke))
                _session.DrawRectangle((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height, stroke, (float)pen.Thickness);
        }

        public override void DrawRoundedRectangle(Brush brush, Pen pen, Rect rect, double radiusX, double radiusY)
        {
            base.DrawRoundedRectangle(brush, pen, rect, radiusX, radiusY);
            if (TryGetColor(brush, out WinColor fill))
                _session.FillRoundedRectangle((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height, (float)radiusX, (float)radiusY, fill);
            if (pen is not null && TryGetColor(pen.Brush, out WinColor stroke))
                _session.DrawRoundedRectangle((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height, (float)radiusX, (float)radiusY, stroke, (float)pen.Thickness);
        }

        public override void DrawLine(Pen pen, Point point0, Point point1)
        {
            base.DrawLine(pen, point0, point1);
            if (pen is not null && TryGetColor(pen.Brush, out WinColor stroke))
                _session.DrawLine((float)point0.X, (float)point0.Y, (float)point1.X, (float)point1.Y, stroke, (float)pen.Thickness);
        }

        static bool TryGetColor(Brush brush, out WinColor color)
        {
            if (brush is Microsoft.UI.Xaml.Media.SolidColorBrush solid)
            {
                color = solid.Color;
                return color.A != 0;
            }
            color = Transparent;
            return false;
        }

        static WinColor ToColor(Brush brush, WinColor fallback)
            => brush is Microsoft.UI.Xaml.Media.SolidColorBrush solid ? solid.Color : fallback;
    }
}
