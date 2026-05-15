// WinUI-compatible FrameworkPropertyMetadata: bridges WPF-style construction to Microsoft.UI.Xaml.PropertyMetadata.
// This class is used in Uno/WinUI contexts where AvalonEdit expects the WPF FrameworkPropertyMetadata API.
// AvalonEdit calls `new FrameworkPropertyMetadata(callback)` (single-arg WPF style), which
// Uno's PropertyMetadata doesn't support. This subclass adds the missing constructors.
//
// Note: This is separate from the System.Windows.FrameworkPropertyMetadata data holder class.
// Uno/WinUI consuming projects use a global using alias to route to this class:
//   global using FrameworkPropertyMetadata = Microsoft.UI.Xaml.FrameworkPropertyMetadata;

using System.Windows;

namespace Microsoft.UI.Xaml;

public class FrameworkPropertyMetadata : Microsoft.UI.Xaml.PropertyMetadata
{
	public FrameworkPropertyMetadata(Microsoft.UI.Xaml.PropertyChangedCallback propertyChangedCallback)
		: base(null, propertyChangedCallback) { }

	public FrameworkPropertyMetadata(object defaultValue)
		: base(defaultValue) { }

	public FrameworkPropertyMetadata(object defaultValue, Microsoft.UI.Xaml.PropertyChangedCallback propertyChangedCallback)
		: base(defaultValue, propertyChangedCallback) { }

	public FrameworkPropertyMetadata(object defaultValue, FrameworkPropertyMetadataOptions options)
		: base(defaultValue) { }

	public FrameworkPropertyMetadata(object defaultValue, FrameworkPropertyMetadataOptions options,
										  Microsoft.UI.Xaml.PropertyChangedCallback propertyChangedCallback)
		: base(defaultValue, propertyChangedCallback) { }

	public FrameworkPropertyMetadata(object defaultValue, Microsoft.UI.Xaml.PropertyChangedCallback propertyChangedCallback,
										  CoerceValueCallback coerceValueCallback)
		: base(defaultValue, propertyChangedCallback) { }

	public FrameworkPropertyMetadata(object defaultValue, FrameworkPropertyMetadataOptions options,
										  Microsoft.UI.Xaml.PropertyChangedCallback propertyChangedCallback,
										  CoerceValueCallback coerceValueCallback)
		: base(defaultValue, propertyChangedCallback) { }
}
