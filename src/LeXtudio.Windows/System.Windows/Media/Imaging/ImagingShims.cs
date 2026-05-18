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
}
