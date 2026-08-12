#if WINUI_BRIDGE
using Microsoft.UI.Xaml.Controls;

namespace System.Windows.Controls
{
    // Shim for WPF's System.Windows.Controls.Image used as the child of
    // InlineUIContainer/BlockUIContainer. Holds a shim BitmapSource for the
    // document model and serialization (data-URI sources, RTF \pict), and
    // renders through an inner WinUI Image control: the Source is re-encoded
    // to PNG and fed into a WinUI BitmapImage so FlowDocumentView's canvas
    // shows the actual pixels.
    //
    // Composes rather than derives from Microsoft.UI.Xaml.Controls.Image
    // because that type is sealed; Grid is the lightest non-sealed panel that
    // can host the single inner Image and stretch to its size.
    public class Image : Microsoft.UI.Xaml.Controls.Grid
    {
        private readonly Microsoft.UI.Xaml.Controls.Image _inner = new();
        private System.Windows.Media.Imaging.BitmapSource? _source;

        public Image() => Children.Add(_inner);

        // Typed as the base ImageSource (like WPF) so upstream casts such as
        // WpfPayload's (DrawingImage)image.Source keep compiling; the document
        // model and serialization only ever set shim BitmapSources.
        public Microsoft.UI.Xaml.Media.ImageSource? Source
        {
            get => _source;
            set
            {
                _source = value as System.Windows.Media.Imaging.BitmapSource;
                if (value is not System.Windows.Media.Imaging.BitmapSource bitmapSource)
                {
                    _inner.Source = null;
                    return;
                }

                var bytes = EncodeToPng(bitmapSource);
                if (bytes is null)
                {
                    _inner.Source = null;
                    return;
                }

                var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                var stream = new global::Windows.Storage.Streams.InMemoryRandomAccessStream();
                var writer = new global::Windows.Storage.Streams.DataWriter(stream);
                writer.WriteBytes(bytes);
                _ = writer.StoreAsync();
                stream.Seek(0);
                _ = bitmap.SetSourceAsync(stream);
                _inner.Source = bitmap;
            }
        }

        public DpiScale GetDpi() => new(1.0, 1.0);

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
