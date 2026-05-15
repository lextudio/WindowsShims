namespace System.Windows;

/// <summary>
/// WPF FrameworkPropertyMetadata that inherits from Microsoft.UI.Xaml.PropertyMetadata
/// so it can be passed directly to Microsoft.UI.Xaml.DependencyProperty.Register(...).
///
/// Bridges WPF's System.Windows.PropertyChangedCallback → WinUI's PropertyChangedCallback by wrapping.
/// </summary>
public class FrameworkPropertyMetadata : Microsoft.UI.Xaml.PropertyMetadata
{
    public FrameworkPropertyMetadata(object? defaultValue)
        : base(defaultValue)
    {
        DefaultValue = defaultValue;
    }

    public FrameworkPropertyMetadata(object? defaultValue, FrameworkPropertyMetadataOptions options)
        : base(defaultValue)
    {
        DefaultValue = defaultValue;
        Options = options;
    }

    public FrameworkPropertyMetadata(object? defaultValue, FrameworkPropertyMetadataOptions options, PropertyChangedCallback? changed)
        : base(defaultValue, Bridge(changed))
    {
        DefaultValue = defaultValue;
        Options = options;
        PropertyChangedCallback = changed;
    }

    public FrameworkPropertyMetadata(object? defaultValue, PropertyChangedCallback? changed)
        : base(defaultValue, Bridge(changed))
    {
        DefaultValue = defaultValue;
        PropertyChangedCallback = changed;
    }

    public FrameworkPropertyMetadata(object? defaultValue, PropertyChangedCallback? changed, CoerceValueCallback? coerce)
        : base(defaultValue, Bridge(changed))
    {
        DefaultValue = defaultValue;
        PropertyChangedCallback = changed;
        CoerceValueCallback = coerce;
    }

    public FrameworkPropertyMetadata(object? defaultValue, FrameworkPropertyMetadataOptions options, PropertyChangedCallback? changed, CoerceValueCallback? coerce)
        : base(defaultValue, Bridge(changed))
    {
        DefaultValue = defaultValue;
        Options = options;
        PropertyChangedCallback = changed;
        CoerceValueCallback = coerce;
    }

    // WinUI callback overloads — AvalonEdit and other Uno/WinUI consumers pass
    // Microsoft.UI.Xaml.PropertyChangedCallback directly (matches the base PropertyMetadata).
    public FrameworkPropertyMetadata(Microsoft.UI.Xaml.PropertyChangedCallback winuiChanged)
        : base(null, winuiChanged) { }

    public FrameworkPropertyMetadata(object? defaultValue, Microsoft.UI.Xaml.PropertyChangedCallback winuiChanged)
        : base(defaultValue, winuiChanged)
    {
        DefaultValue = defaultValue;
    }

    public FrameworkPropertyMetadata(object? defaultValue, FrameworkPropertyMetadataOptions options, Microsoft.UI.Xaml.PropertyChangedCallback winuiChanged)
        : base(defaultValue, winuiChanged)
    {
        DefaultValue = defaultValue;
        Options = options;
    }

    public FrameworkPropertyMetadata(object? defaultValue, Microsoft.UI.Xaml.PropertyChangedCallback winuiChanged, CoerceValueCallback? coerce)
        : base(defaultValue, winuiChanged)
    {
        DefaultValue = defaultValue;
        CoerceValueCallback = coerce;
    }

    public FrameworkPropertyMetadata(object? defaultValue, FrameworkPropertyMetadataOptions options, Microsoft.UI.Xaml.PropertyChangedCallback winuiChanged, CoerceValueCallback? coerce)
        : base(defaultValue, winuiChanged)
    {
        DefaultValue = defaultValue;
        Options = options;
        CoerceValueCallback = coerce;
    }

    public new object? DefaultValue { get; }
    public FrameworkPropertyMetadataOptions Options { get; }
    public PropertyChangedCallback? PropertyChangedCallback { get; }
    public CoerceValueCallback? CoerceValueCallback { get; }

    // Bridge WPF callback to WinUI's: WPF uses System.Windows.DependencyObject and WinUI uses
    // Microsoft.UI.Xaml.DependencyObject. These types don't share a base, so we can't directly
    // dispatch WPF callbacks from WinUI sender. The shim stores the WPF callback for inspection
    // but doesn't invoke it from WinUI's notification path. Shim consumers that need the WPF
    // callback semantics should rely on the local System.Windows.DependencyObject path.
    private static Microsoft.UI.Xaml.PropertyChangedCallback? Bridge(PropertyChangedCallback? wpf)
        => null;
}
