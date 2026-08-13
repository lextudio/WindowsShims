using System.Runtime.Serialization;
using SkiaSharp;

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

	// WinAppSDK does not allow subclassing Microsoft.UI.Xaml.Media.ImageSource from managed
	// code: its constructor needs a WinRT composable instance, and passing null throws inside
	// the base constructor. So on that target BitmapSource stands alone, and
	// System.Windows.Controls.Image converts it to a real BitmapImage for rendering. Uno does
	// permit the subclassing, so it is kept there for upstream code that assigns a
	// BitmapSource straight into a WinUI Source property.
	public class BitmapSource
#if !WINDOWS_APP_SDK
		: Microsoft.UI.Xaml.Media.ImageSource
#endif
	{
		private byte[] _pixels = [];

		public BitmapSource()
#if !WINDOWS_APP_SDK
			: base("")
#endif
		{ }

		public int PixelWidth { get; set; }

		public int PixelHeight { get; set; }

		// WPF exposes Width/Height (DIP) alongside PixelWidth/PixelHeight; the RTF writer reads them.
		public double Width { get; set; }
		public double Height { get; set; }

		// Used by WpfPayload for image serialization and the RTF converter.
		public double DpiX { get; set; }
		public double DpiY { get; set; }
		public PixelFormat Format { get; set; }

		public BitmapPalette? Palette { get; set; }

		public virtual byte[] GetPixelData()
		{
			return _pixels;
		}

		public static BitmapSource Create(int pixelWidth, int pixelHeight, double dpiX, double dpiY, PixelFormat pixelFormat, BitmapPalette? palette, byte[] pixels, int stride)
		{
			return new BitmapSource
			{
				PixelWidth = pixelWidth,
				PixelHeight = pixelHeight,
				Width = pixelWidth,
				Height = pixelHeight,
				DpiX = dpiX,
				DpiY = dpiY,
				Format = pixelFormat,
				_pixels = (byte[])pixels.Clone(),
			};
		}

		public void CopyPixels(Array pixels, int stride, int offset)
		{
			var data = GetPixelData();
			Buffer.BlockCopy(data, 0, pixels, offset, Math.Min(data.Length, pixels.Length));
		}

		// Decodes encoded image bytes (PNG/JPEG/...) into a BitmapSource holding
		// BGRA pixel data. Used by the shim XamlReader for data-URI image sources.
		public static BitmapSource? Decode(byte[] data)
		{
			if (data.Length == 0)
				return null;
			using var skBitmap = SkiaSharp.SKBitmap.Decode(data);
			if (skBitmap is null)
				return null;
			var pixels = new byte[skBitmap.Width * skBitmap.Height * 4];
			System.Runtime.InteropServices.Marshal.Copy(skBitmap.GetPixels(), pixels, 0, pixels.Length);
			return Create(skBitmap.Width, skBitmap.Height, 96.0, 96.0, PixelFormats.Pbgra32, null, pixels, skBitmap.Width * 4);
		}
	}

	public class BitmapPalette
	{
		public IList<Color> Colors { get; } = new List<Color>();
	}

	// Shim for WPF BitmapImage; used by TextRangeSerialization for image embedding in XAML packages.
	public class BitmapImage : BitmapSource
	{
		public static readonly DependencyProperty UriSourceProperty =
			DependencyProperty.Register("UriSource", typeof(Uri), typeof(BitmapImage), null);
		public static readonly DependencyProperty CacheOptionProperty =
			DependencyProperty.Register("CacheOption", typeof(object), typeof(BitmapImage), null);
	}

	// Cross-platform BitmapEncoder hierarchy backed by SkiaSharp.
	// Used by WpfPayload to embed images into XamlPackage (OPC) containers.

	public abstract class BitmapEncoder
	{
		protected BitmapSource? _source;
		public void Initialize(BitmapSource source) => _source = source;
		public abstract void Save(Stream stream);
		public IList<BitmapSource> Frames { get; } = new List<BitmapSource>();

		internal static SKEncodedImageFormat ToSkFormat(string contentType) => contentType switch
		{
			"image/bmp" => SKEncodedImageFormat.Bmp,
			"image/jpeg" or "image/jpg" => SKEncodedImageFormat.Jpeg,
			"image/png" => SKEncodedImageFormat.Png,
			_ => SKEncodedImageFormat.Png,
		};
	}

	public sealed class PngBitmapEncoder : BitmapEncoder
	{
		public override void Save(Stream stream) => EncodeToStream(stream, SKEncodedImageFormat.Png);

		internal void EncodeToStream(Stream stream, SKEncodedImageFormat format)
		{
			if (_source is null) return;
			var pixels = _source.GetPixelData();
			int w = _source.PixelWidth;
			int h = _source.PixelHeight;
			using var skBmp = new SKBitmap(w, h, SKColorType.Bgra8888, SKAlphaType.Unpremul);
			System.Runtime.InteropServices.Marshal.Copy(pixels, 0, skBmp.GetPixels(), pixels.Length);
			using var skImg = SKImage.FromBitmap(skBmp);
			using var data = skImg.Encode(format, 100);
			data.SaveTo(stream);
		}
	}

	public sealed class JpegBitmapEncoder : BitmapEncoder
	{
		public int QualityLevel { get; set; } = 75;
		public override void Save(Stream stream)
		{
			if (_source is null) return;
			var pixels = _source.GetPixelData();
			int w = _source.PixelWidth;
			int h = _source.PixelHeight;
			using var skBmp = new SKBitmap(w, h, SKColorType.Bgra8888, SKAlphaType.Unpremul);
			System.Runtime.InteropServices.Marshal.Copy(pixels, 0, skBmp.GetPixels(), pixels.Length);
			using var skImg = SKImage.FromBitmap(skBmp);
			using var data = skImg.Encode(SKEncodedImageFormat.Jpeg, QualityLevel);
			data.SaveTo(stream);
		}
	}

	public sealed class BmpBitmapEncoder : BitmapEncoder
	{
		public override void Save(Stream stream)
		{
			if (_source is null) return;
			var png = new PngBitmapEncoder();
			png.Initialize(_source);
			png.EncodeToStream(stream, SKEncodedImageFormat.Bmp);
		}
	}

	public sealed class GifBitmapEncoder : BitmapEncoder
	{
		public override void Save(Stream stream)
		{
			if (_source is null) return;
			var png = new PngBitmapEncoder();
			png.Initialize(_source);
			png.EncodeToStream(stream, SKEncodedImageFormat.Png);
		}
	}

	public sealed class TiffBitmapEncoder : BitmapEncoder
	{
		public override void Save(Stream stream)
		{
			if (_source is null) return;
			var png = new PngBitmapEncoder();
			png.Initialize(_source);
			png.EncodeToStream(stream, SKEncodedImageFormat.Png);
		}
	}

	// RenderTargetBitmap renders a WPF UI element into a bitmap.
	public sealed class RenderTargetBitmap : BitmapSource
	{
		public RenderTargetBitmap(int pixelWidth, int pixelHeight, double dpiX, double dpiY, PixelFormat pixelFormat)
		{
			PixelWidth = pixelWidth;
			PixelHeight = pixelHeight;
			DpiX = dpiX;
			DpiY = dpiY;
			Format = pixelFormat;
		}

		public void Render(UIElement visual) { }
	}

	// BitmapFrameDecode represents a single frame from a decoded bitmap.
	public class BitmapFrameDecode : BitmapSource
	{
		public BitmapDecoder? Decoder { get; set; }
	}

	// Minimal BitmapDecoder stub.
	public class BitmapDecoder
	{
		public BitmapFrameCollection Frames { get; } = new();
	}

	public class BitmapFrameCollection : List<BitmapFrameDecode> { }
}

namespace System.Windows.Media
{
	// DrawingImage is a WPF type representing an image backed by a Drawing.
	// Standalone on WinAppSDK for the same reason as BitmapSource above.
	public class DrawingImage
#if !WINDOWS_APP_SDK
		: Microsoft.UI.Xaml.Media.ImageSource
#endif
	{
		public DrawingImage()
#if !WINDOWS_APP_SDK
			: base("")
#endif
		{ }

		public object? Drawing { get; set; }
		public double Width { get; set; }
		public double Height { get; set; }
	}
}


// ── PackagePart extension: GetSeekableStream ──────────────────────────────

namespace System.IO.Packaging
{
	internal static class PackagePartExtensions
	{
		internal static System.IO.Stream GetSeekableStream(this PackagePart part) => part.GetStream();
	}
}

// ── PackageStore ──────────────────────────────────────────────────────────

namespace System.IO.Packaging
{
	internal static class PackageStore
	{
		private static readonly Dictionary<Uri, Package> _packages = new();
		internal static void AddPackage(Uri uri, Package package) => _packages[uri] = package;
		internal static void RemovePackage(Uri uri) => _packages.Remove(uri);
	}
}

// ── ParserContext / XamlParseException ────────────────────────────────────

namespace System.Windows.Markup
{
	// ParserContext is used by XamlReader.Load and must be public to match WPF API.
	public class ParserContext
	{
		public Uri? BaseUri { get; set; }
	}

	public class XamlParseException : Exception
	{
		public XamlParseException() { }
		public XamlParseException(string message) : base(message) { }
		public XamlParseException(string message, Exception inner) : base(message, inner) { }
	}
}
