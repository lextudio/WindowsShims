#if WINUI_BRIDGE
using Microsoft.UI.Xaml.Controls;

namespace System.Windows.Controls
{
    // Shim for WPF's System.Windows.Controls.Image used as the child of
    // InlineUIContainer/BlockUIContainer. Holds a shim BitmapSource so the
    // document model, XAML serialization (data-URI sources), and RTF \pict
    // conversion can carry actual image pixels.
    public class Image : Microsoft.UI.Xaml.FrameworkElement
    {
        public ImageSource Source { get; set; }

        public double Height { get; set; }
        public double Width { get; set; }

        public DpiScale GetDpi() => new(1.0, 1.0);
    }
}
#endif
