// WinUI-compatible FrameworkPropertyMetadata: bridges WPF-style construction to Microsoft.UI.Xaml.PropertyMetadata.
// This class is used in Uno/WinUI contexts where AvalonEdit expects the WPF FrameworkPropertyMetadata API.
// AvalonEdit calls `new FrameworkPropertyMetadata(callback)` (single-arg WPF style), which
// Uno's PropertyMetadata doesn't support. This subclass adds the missing constructors.
//
// Note: This is separate from the System.Windows.FrameworkPropertyMetadata data holder class.
// On Uno (net9.0-desktop): FrameworkPropertyMetadataOptions is available from System.Windows.
// On WinUI (net9.0-windows10.0.19041.0): FrameworkPropertyMetadataOptions doesn't exist, so those overloads are excluded.

#if !WINDOWS_APP_SDK
using System.Windows;
#endif

namespace LeXtudio.UI.Xaml;

public class FrameworkPropertyMetadata : Microsoft.UI.Xaml.PropertyMetadata
{
	public FrameworkPropertyMetadata(Microsoft.UI.Xaml.PropertyChangedCallback propertyChangedCallback)
		: base(null, propertyChangedCallback) { }

	public FrameworkPropertyMetadata(object defaultValue)
		: base(defaultValue) { }

	public FrameworkPropertyMetadata(object defaultValue, Microsoft.UI.Xaml.PropertyChangedCallback propertyChangedCallback)
		: base(defaultValue, propertyChangedCallback) { }

#if !WINDOWS_APP_SDK
	public FrameworkPropertyMetadata(object defaultValue, System.Windows.FrameworkPropertyMetadataOptions options)
		: base(defaultValue) { }

	public FrameworkPropertyMetadata(object defaultValue, System.Windows.FrameworkPropertyMetadataOptions options,
										  Microsoft.UI.Xaml.PropertyChangedCallback propertyChangedCallback)
		: base(defaultValue, propertyChangedCallback) { }

	public FrameworkPropertyMetadata(object defaultValue, Microsoft.UI.Xaml.PropertyChangedCallback propertyChangedCallback,
										  System.Windows.CoerceValueCallback coerceValueCallback)
		: base(defaultValue, propertyChangedCallback) { }

	public FrameworkPropertyMetadata(object defaultValue, System.Windows.FrameworkPropertyMetadataOptions options,
										  Microsoft.UI.Xaml.PropertyChangedCallback propertyChangedCallback,
										  System.Windows.CoerceValueCallback coerceValueCallback)
		: base(defaultValue, propertyChangedCallback) { }
#endif
}
