// WPF's System.Windows.GridLength lives in PresentationFramework; WinUI/Uno
// exposes Microsoft.UI.Xaml.GridLength. Code that fully-qualifies
// `new System.Windows.GridLength(value)` (e.g. AvalonDock Layout tests) needs
// this shim so the fully-qualified name resolves without modification.
//
// The shim simply wraps the WinUI struct and forwards to its constructors /
// properties so it is call-compatible with the WPF original.

namespace System.Windows
{
	public readonly struct GridLength : IEquatable<GridLength>
	{
		private readonly Microsoft.UI.Xaml.GridLength _inner;

		public GridLength(double pixels)
			=> _inner = new Microsoft.UI.Xaml.GridLength(pixels, Microsoft.UI.Xaml.GridUnitType.Pixel);

		public GridLength(double value, GridUnitType unitType)
			=> _inner = new Microsoft.UI.Xaml.GridLength(value, (Microsoft.UI.Xaml.GridUnitType)(int)unitType);

		public static GridLength Auto
			=> new GridLength(1.0, GridUnitType.Auto);

		public double Value => _inner.Value;
		public GridUnitType GridUnitType => (GridUnitType)(int)_inner.GridUnitType;
		public bool IsAbsolute => _inner.IsAbsolute;
		public bool IsAuto => _inner.IsAuto;
		public bool IsStar => _inner.IsStar;

		// Implicit conversion to/from the WinUI struct so AvalonDock model code
		// that holds Microsoft.UI.Xaml.GridLength (via global-using alias) can
		// accept values constructed as System.Windows.GridLength.
		public static implicit operator Microsoft.UI.Xaml.GridLength(GridLength g)
			=> g._inner;

		public static implicit operator GridLength(Microsoft.UI.Xaml.GridLength g)
			=> new GridLength(g.Value, (GridUnitType)(int)g.GridUnitType);

		public bool Equals(GridLength other) => _inner.Equals(other._inner);
		public override bool Equals(object obj) => obj is GridLength g && Equals(g);
		public override int GetHashCode() => _inner.GetHashCode();
		public override string ToString() => _inner.ToString();
	}

	public enum GridUnitType
	{
		Auto = 0,
		Pixel = 1,
		Star = 2,
	}
}
