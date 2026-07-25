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

		// Used by WpfPayload for image serialization and the RTF converter.
		public double DpiX { get; set; }
		public double DpiY { get; set; }
		public PixelFormat Format { get; set; }

		public BitmapPalette? Palette { get; set; }

		public virtual byte[] GetPixelData()
		{
			return new byte[PixelWidth * PixelHeight * 4];
		}

		public void CopyPixels(Array pixels, int stride, int offset)
		{
			var data = GetPixelData();
			Buffer.BlockCopy(data, 0, pixels, offset, Math.Min(data.Length, pixels.Length));
		}
	}

	public class BitmapPalette
	{
		public IList<Color> Colors { get; } = new List<Color>();
	}

	public static class BitmapFrame
	{
		public static BitmapSource Create(System.IO.Stream stream) => new BitmapSource();
		public static BitmapSource Create(BitmapSource source) => source;
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
	public class DrawingImage : Microsoft.UI.Xaml.Media.ImageSource
	{
		public object? Drawing { get; set; }
		public double Width { get; set; }
		public double Height { get; set; }
	}
}

namespace System.Windows.Media
{
	// PixelFormats provides well-known PixelFormat values.
	public static class PixelFormats
	{
		public static PixelFormat Default => new(32, 24, 8);
		public static PixelFormat Bgra32 => new(32, 24, 0, 8, 8, 8, 8);
		public static PixelFormat Pbgra32 => new(32, 32, 8, 8, 8, 8, 8);
	}

	public readonly struct PixelFormat
	{
		public int BitsPerPixel { get; }
		public PixelFormat(int bpp, params int[] channelDepths) => BitsPerPixel = bpp;
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
