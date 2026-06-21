using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;

namespace System.Windows.Media
{
    /// <summary>
    /// Portable shim for System.Windows.Media.FormattedText. Unlike a pure compiler stub, this
    /// retains the text/typeface/foreground so the Win2D-backed <see cref="DrawingContext"/>
    /// adapter can actually paint it, and derives Width/Height/Baseline from real glyph metrics
    /// (via <see cref="CanvasTextLayout"/>) instead of a fixed-advance approximation.
    /// </summary>
    public class FormattedText
    {
        readonly double _emSize;
        bool _measured;
        double _width;
        double _height;
        double _baseline;
        double _widthIncludingTrailingWhitespace;

        public FormattedText(string textToFormat, System.Globalization.CultureInfo culture, FlowDirection flowDirection,
            Typeface typeface, double emSize, Brush foreground)
        {
            Text = textToFormat ?? string.Empty;
            Culture = culture;
            FlowDirection = flowDirection;
            Typeface = typeface;
            _emSize = emSize > 0 ? emSize : 12.0;
            Foreground = foreground;
        }

        /// <summary>The text being formatted.</summary>
        public string Text { get; }

        /// <summary>The culture supplied at construction (may be null).</summary>
        public System.Globalization.CultureInfo Culture { get; }

        public FlowDirection FlowDirection { get; }

        /// <summary>The typeface used to measure and draw the text (may be null).</summary>
        public Typeface Typeface { get; }

        /// <summary>The em size (font size, in DIPs) used to measure and draw the text.</summary>
        public double EmSize => _emSize;

        /// <summary>The foreground brush used to draw the text (may be null).</summary>
        public Brush Foreground { get; set; }

        public double Width { get { EnsureMeasured(); return _width; } }
        public double Height { get { EnsureMeasured(); return _height; } }
        public double Baseline { get { EnsureMeasured(); return _baseline; } }
        public double WidthIncludingTrailingWhitespace { get { EnsureMeasured(); return _widthIncludingTrailingWhitespace; } }

        /// <summary>Builds the Win2D text format describing this run (font family + size).</summary>
        internal CanvasTextFormat CreateCanvasTextFormat()
            => new CanvasTextFormat
            {
                FontFamily = Typeface?.FontFamily?.Source,
                FontSize = (float)_emSize,
            };

        void EnsureMeasured()
        {
            if (_measured)
                return;
            _measured = true;

            try
            {
                using CanvasTextFormat format = CreateCanvasTextFormat();
                using var layout = new CanvasTextLayout(CanvasDevice.GetSharedDevice(), Text, format, float.PositiveInfinity, float.PositiveInfinity);
                var bounds = layout.LayoutBounds;
                _width = bounds.Width;
                _height = bounds.Height;
                var lineMetrics = layout.LineMetrics;
                _baseline = lineMetrics.Length > 0 ? lineMetrics[0].Baseline : _emSize * 0.8;
                _widthIncludingTrailingWhitespace = _width;
            }
            catch
            {
                // Fall back to a coarse estimate if layout cannot be created (e.g. no device).
                double charCount = Text.Length;
                _width = charCount * _emSize * 0.6;
                _widthIncludingTrailingWhitespace = _width;
                _height = _emSize * 1.4;
                _baseline = _emSize * 1.1;
            }
        }
    }
}
