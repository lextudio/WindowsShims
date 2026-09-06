namespace System.Windows.Media
{
    /// <summary>Compiler shim for System.Windows.Media.ImageSource.</summary>
    public abstract class ImageSource { }

    /// <summary>
    /// WPF's ImageSource carries Width/Height in device-independent pixels; WinUI's does
    /// not, and only a BitmapSource knows its size. These report the pixel size, which is
    /// the same number at 96 dpi, and 0 for a source whose size is not yet known.
    /// </summary>
    public static class WinUIImageSourceExtensions
    {
        extension(Microsoft.UI.Xaml.Media.ImageSource? source)
        {
            public double Width => source is Imaging.BitmapSource bitmap ? bitmap.PixelWidth : 0.0;

            public double Height => source is Imaging.BitmapSource bitmap ? bitmap.PixelHeight : 0.0;
        }
    }
}
