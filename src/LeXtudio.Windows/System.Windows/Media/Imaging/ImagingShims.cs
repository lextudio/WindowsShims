using System.Runtime.Serialization;

#if !WINDOWS_APP_SDK

namespace WinRT
{
	public interface IObjectReference
	{
    }
}

#endif

namespace System.Windows.Media.Imaging
{

	public class BitmapSource : Microsoft.UI.Xaml.Media.ImageSource
	{
		public BitmapSource()
#if WINDOWS_APP_SDK
			: base((WinRT.IObjectReference)null)
#else
			: base("")
#endif
		{ }

		public int PixelWidth { get; set; }

		public int PixelHeight { get; set; }

		// WPF exposes Width/Height (DIP) alongside PixelWidth/PixelHeight; the RTF writer reads them.
		public double Width { get; set; }
		public double Height { get; set; }
	}

	public static class BitmapFrame
	{
		public static BitmapSource Create(System.IO.Stream stream) => new BitmapSource();
	}

	// Shim for WPF BitmapImage; used by TextRangeSerialization for image embedding in XAML packages.
	// The image-serialization path is gated #if !HAS_UNO; these stubs just satisfy the compiler.
	public class BitmapImage : BitmapSource
	{
		public static readonly DependencyProperty UriSourceProperty =
			DependencyProperty.Register("UriSource", typeof(Uri), typeof(BitmapImage), null);
		public static readonly DependencyProperty CacheOptionProperty =
			DependencyProperty.Register("CacheOption", typeof(object), typeof(BitmapImage), null);
	}
}
