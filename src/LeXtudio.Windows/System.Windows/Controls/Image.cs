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
    public class Image : Microsoft.UI.Xaml.Controls.Image
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
                if (value is not System.Windows.Media.Imaging.BitmapSource bitmapSource)
                {
                    base.Source = null;
                    return;
                }

                var bytes = EncodeToPng(bitmapSource);
                if (bytes is null)
                {
                    base.Source = null;
                    return;
                }

                var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                var stream = new global::Windows.Storage.Streams.InMemoryRandomAccessStream();
                var writer = new global::Windows.Storage.Streams.DataWriter(stream);
                writer.WriteBytes(bytes);
                _ = writer.StoreAsync();
                stream.Seek(0);
                _ = bitmap.SetSourceAsync(stream);
                base.Source = bitmap;
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
