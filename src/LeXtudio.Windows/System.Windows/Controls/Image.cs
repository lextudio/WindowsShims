#if WINUI_BRIDGE
using Microsoft.UI.Xaml.Controls;

namespace System.Windows.Controls
{
    // Shim for WPF's System.Windows.Controls.Image used as the child of
    // InlineUIContainer/BlockUIContainer. Holds a shim BitmapSource for the
    // document model and serialization (data-URI sources, RTF \pict), and
    // renders through the WinUI Image control: the Source is re-encoded to
    // PNG and fed into a WinUI BitmapImage so FlowDocumentView's canvas shows
    // the actual pixels.
    //
    // Under Uno, Microsoft.UI.Xaml.Controls.Image is derivable, so the shim IS
    // a WinUI Image and consumers can pattern-match the container child on that
    // type. WinAppSDK seals it, so that target composes an inner Image instead
    // (see the WINDOWS_APP_SDK branch) and exposes it via PresentedImage.
#if WINDOWS_APP_SDK
    public partial class Image : Microsoft.UI.Xaml.Controls.Grid
    {
        private readonly Microsoft.UI.Xaml.Controls.Image _inner = new();
        private System.Windows.Media.Imaging.BitmapSource? _source;

        public Image() => Children.Add(_inner);

        // The WinUI Image actually painting the pixels. WinAppSDK-only, because
        // this is the target where the shim can't be an Image itself; code that
        // needs the rendering control should go through this.
        public Microsoft.UI.Xaml.Controls.Image PresentedImage => _inner;

        // Typed as object because the shim's BitmapSource/DrawingImage cannot derive from
        // Microsoft.UI.Xaml.Media.ImageSource on this target (see ImagingShims). Upstream
        // casts such as WpfPayload's (DrawingImage)image.Source still compile, and the
        // document model and serialization only ever set shim BitmapSources.
        public object? Source
        {
            get => _source;
            set
            {
                _source = value as System.Windows.Media.Imaging.BitmapSource;
                _inner.Source = ToWinUIBitmap(value);
            }
        }

        public DpiScale GetDpi() => new(1.0, 1.0);
    }
#else
    public partial class Image : Microsoft.UI.Xaml.Controls.Image
    {
        private System.Windows.Media.Imaging.BitmapSource? _source;

        // Typed as the base ImageSource (like WPF) so upstream casts such as
        // WpfPayload's (DrawingImage)image.Source keep compiling; the document
        // model and serialization only ever set shim BitmapSources.
        public new Microsoft.UI.Xaml.Media.ImageSource? Source
        {
            get => _source;
            set
            {
                _source = value as System.Windows.Media.Imaging.BitmapSource;
                base.Source = ToWinUIBitmap(value);
            }
        }

        public new double Height
        {
            get => base.Height;
            set => base.Height = value;
        }

        public new double Width
        {
            get => base.Width;
            set => base.Width = value;
        }

        public DpiScale GetDpi() => new(1.0, 1.0);
    }
#endif

    public partial class Image
    {
        // Re-encodes a shim BitmapSource to PNG and wraps it in a WinUI
        // BitmapImage, which is what the renderer can actually paint.
        private protected static Microsoft.UI.Xaml.Media.Imaging.BitmapImage? ToWinUIBitmap(
            object? value)
        {
            if (value is not System.Windows.Media.Imaging.BitmapSource bitmapSource)
                return null;

            var bytes = EncodeToPng(bitmapSource);
            if (bytes is null)
                return null;

            var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
            var stream = new global::Windows.Storage.Streams.InMemoryRandomAccessStream();
            var writer = new global::Windows.Storage.Streams.DataWriter(stream);
            writer.WriteBytes(bytes);
            _ = writer.StoreAsync();
            stream.Seek(0);
            _ = bitmap.SetSourceAsync(stream);
            return bitmap;
        }

        static byte[]? EncodeToPng(System.Windows.Media.Imaging.BitmapSource source)
        {
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Initialize(source);
            using var stream = new System.IO.MemoryStream();
            encoder.Save(stream);
            return stream.Length > 0 ? stream.ToArray() : null;
        }
    }
}
#endif
