using SkiaSharp;

namespace System.Windows.Media.Imaging
{
	public sealed class WriteableBitmap : BitmapSource
	{
		SKBitmap _bitmap;

		public WriteableBitmap(int pixelWidth, int pixelHeight, double dpiX, double dpiY, PixelFormat pixelFormat, BitmapPalette? palette)
		{
			PixelWidth = pixelWidth;
			PixelHeight = pixelHeight;
			DpiX = dpiX;
			DpiY = dpiY;
			Format = pixelFormat;
			_bitmap = new SKBitmap(pixelWidth, pixelHeight, SKColorType.Bgra8888, SKAlphaType.Unpremul);
			BackBufferStride = pixelWidth * 4;
		}

		public WriteableBitmap(BitmapSource source)
		{
			PixelWidth = source.PixelWidth;
			PixelHeight = source.PixelHeight;
			DpiX = source.DpiX;
			DpiY = source.DpiY;
			Format = source.Format;
			_bitmap = new SKBitmap(PixelWidth, PixelHeight, SKColorType.Bgra8888, SKAlphaType.Unpremul);
			BackBufferStride = PixelWidth * 4;
			var data = source.GetPixelData();
			if (data.Length > 0)
				System.Runtime.InteropServices.Marshal.Copy(data, 0, _bitmap.GetPixels(), Math.Min(data.Length, PixelWidth * PixelHeight * 4));
		}

		public IntPtr BackBuffer => _bitmap.GetPixels();

		public int BackBufferStride { get; }

		public void Lock() { }

		public void Unlock() { }

		public override byte[] GetPixelData()
		{
			int size = PixelWidth * PixelHeight * 4;
			byte[] pixels = new byte[size];
			System.Runtime.InteropServices.Marshal.Copy(_bitmap.GetPixels(), pixels, 0, size);
			return pixels;
		}
	}

	public sealed class FormatConvertedBitmap : BitmapSource
	{
		public FormatConvertedBitmap() { }

		public FormatConvertedBitmap(BitmapSource source, PixelFormat pixelFormat, BitmapPalette? palette, double alphaThreshold)
		{
			PixelWidth = source.PixelWidth;
			PixelHeight = source.PixelHeight;
			DpiX = source.DpiX;
			DpiY = source.DpiY;
			Format = pixelFormat;
		}

		public void CopyPixels(byte[] pixels, int stride, int offset)
		{
			base.CopyPixels(pixels, stride, offset);
		}
	}

	public sealed class TransformedBitmap : BitmapSource
	{
		public TransformedBitmap() { }

		public TransformedBitmap(BitmapSource source, object transform)
		{
			PixelWidth = source.PixelWidth;
			PixelHeight = source.PixelHeight;
			DpiX = source.DpiX;
			DpiY = source.DpiY;
			Format = source.Format;
		}
	}

	public class BitmapFrame : BitmapSource
	{
		public static BitmapFrame Create(Stream stream) => new BitmapFrame();
		public static BitmapFrame Create(BitmapSource source) => (BitmapFrame)source;
		public static BitmapFrame Create(int pixelWidth, int pixelHeight, double dpiX, double dpiY, PixelFormat pixelFormat, BitmapPalette palette, BitmapSource source, byte[] unused) => new BitmapFrame();
	}
}

namespace System.Windows.Media
{
	public sealed class ScaleTransform
	{
		public ScaleTransform() { }
		public ScaleTransform(double scaleX, double scaleY) { ScaleX = scaleX; ScaleY = scaleY; }
		public double ScaleX { get; set; }
		public double ScaleY { get; set; }
	}
}

namespace System.Windows.Media.Imaging
{
	public static class PixelFormats
	{
		public static PixelFormat Default => new PixelFormat(32, 24, 8);
		public static PixelFormat Bgra32 => new PixelFormat(32, 24, 0, 8, 8, 8, 8);
		public static PixelFormat Pbgra32 => new PixelFormat(32, 32, 8, 8, 8, 8, 8);
		public static PixelFormat Indexed1 => new PixelFormat(1, 0, 0, 0, 0, 0, 0);
		public static PixelFormat Indexed4 => new PixelFormat(4, 0, 0, 0, 0, 0, 0);
		public static PixelFormat Indexed8 => new PixelFormat(8, 0, 0, 0, 0, 0, 0);
		public static PixelFormat Rgb24 => new PixelFormat(24, 24, 23, 0, 8, 8, 8);
	}

	public readonly struct PixelFormat : IEquatable<PixelFormat>
	{
		public int BitsPerPixel { get; }
		public PixelFormat(int bpp, params int[] channelDepths) => BitsPerPixel = bpp;

		public bool Equals(PixelFormat other) => BitsPerPixel == other.BitsPerPixel;
		public override bool Equals(object? obj) => obj is PixelFormat pf && Equals(pf);
		public override int GetHashCode() => BitsPerPixel;
		public static bool operator ==(PixelFormat left, PixelFormat right) => left.Equals(right);
		public static bool operator !=(PixelFormat left, PixelFormat right) => !left.Equals(right);
	}
}
