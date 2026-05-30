// WPF's System.Windows.UIPropertyMetadata sits between PropertyMetadata and
// FrameworkPropertyMetadata. Linked WPF code (e.g. AvalonDock) registers
// dependency properties with `new UIPropertyMetadata(default, changed, coerce)`.
// We bridge it onto Microsoft.UI.Xaml.PropertyMetadata exactly like
// FrameworkPropertyMetadata does, so the same Register(...) call works on Uno.

namespace System.Windows
{
	public class UIPropertyMetadata : Microsoft.UI.Xaml.PropertyMetadata
	{
		public UIPropertyMetadata()
			: base(null) { }

		public UIPropertyMetadata(object defaultValue)
			: base(defaultValue)
		{
			DefaultValue = defaultValue;
		}

		public UIPropertyMetadata(object defaultValue, PropertyChangedCallback propertyChangedCallback)
			: base(defaultValue, Bridge(propertyChangedCallback))
		{
			DefaultValue = defaultValue;
			PropertyChangedCallback = propertyChangedCallback;
		}

		public UIPropertyMetadata(object defaultValue, PropertyChangedCallback propertyChangedCallback, CoerceValueCallback coerceValueCallback)
			: base(defaultValue, Bridge(propertyChangedCallback))
		{
			DefaultValue = defaultValue;
			PropertyChangedCallback = propertyChangedCallback;
			CoerceValueCallback = coerceValueCallback;
		}

		public new object DefaultValue { get; }
		public PropertyChangedCallback PropertyChangedCallback { get; }
		public CoerceValueCallback CoerceValueCallback { get; }

		// WPF and WinUI PropertyChangedCallback share the same effective signature
		// here (DependencyObject + DependencyPropertyChangedEventArgs both map to
		// the Microsoft.UI.Xaml types), so we can forward the callback directly —
		// AvalonDock relies on these firing (e.g. Title change → RaisePropertyChanged).
		private static Microsoft.UI.Xaml.PropertyChangedCallback Bridge(PropertyChangedCallback wpf)
			=> wpf == null ? null : (d, e) => wpf(d, e);
	}
}

namespace System.Windows.Controls
{
	// WPF's orientation enum. WinUI has Microsoft.UI.Xaml.Controls.Orientation, but
	// AvalonDock source fully-qualifies `System.Windows.Controls.Orientation`, so we
	// provide it here with matching member order (Horizontal = 0, Vertical = 1).
	public enum Orientation
	{
		Horizontal = 0,
		Vertical = 1,
	}
}
