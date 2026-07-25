// Shims supporting the upstream WPF Xaml↔RTF conversion subsystem
// (XamlRtfConverter, XamlToRtfParser, XamlToRtfWriter, RtfToXamlReader, RtfToXamlLexer,
//  and supporting Rtf*/Xaml* helper files).

namespace MS.Internal.Globalization
{
    // Empty stub: upstream XamlToRtfWriter.cs has `using MS.Internal.Globalization;`
    // but does not reference any members from it.
    internal static class _Marker { }
}

namespace System.Windows.Media
{
    // Mirrors Microsoft.UI.Xaml.Media.Stretch members used by the converters.
    public enum Stretch
    {
        None = 0,
        Fill = 1,
        Uniform = 2,
        UniformToFill = 3,
    }
}

namespace System.Windows.Controls
{
    // Mirrors Microsoft.UI.Xaml.Controls.StretchDirection members.
    public enum StretchDirection
    {
        UpOnly = 0,
        DownOnly = 1,
        Both = 2,
    }

    public static class Viewbox
    {
        // Scale-factor computation used by the RTF writer to size embedded images.
        // The neutral 1x1 fallback keeps the writer emitting valid (if unscaled) markup.
        public static global::Windows.Foundation.Size ComputeScaleFactor(
            global::Windows.Foundation.Size availableSize,
            global::Windows.Foundation.Size contentSize,
            System.Windows.Media.Stretch stretch,
            StretchDirection stretchDirection) => new global::Windows.Foundation.Size(1, 1);
    }
}

namespace System.Windows.Media
{
    // Symbol-font detection only; full glyph table not modeled.
    public sealed class GlyphTypeface
    {
        public bool Symbol => false;
    }
}

namespace MS.Internal
{
    // Metafile→image conversion is a niche WMF path; stubbed as no-op.
    internal static class SystemDrawingHelper
    {
        internal static void SaveMetafileToImageStream(System.IO.Stream metafileStream, System.IO.Stream imageStream)
        {
        }
    }
}

namespace System.Windows.Media
{
    // WPF's Color has ToString(IFormatProvider); WinUI's Windows.UI.Color does not.
    // Used by the RTF writer to emit invariant-culture color strings.
    internal static class ColorFormattingExtensions
    {
        internal static string ToString(this global::Windows.UI.Color color, System.IFormatProvider provider)
            => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}

// ── OPC / XamlPackage shims (PackageStore, ParserContext) live in ImagingShims.cs ──
