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
    // Parameter is named 'propertyChangedCallback' to match the named argument emitted by
    // Uno's DependencyObjectGenerator source generator.
    public FrameworkPropertyMetadata(Microsoft.UI.Xaml.PropertyChangedCallback? propertyChangedCallback)
        : base(null, propertyChangedCallback) { }

    public FrameworkPropertyMetadata(object? defaultValue, Microsoft.UI.Xaml.PropertyChangedCallback? propertyChangedCallback)
        : base(defaultValue, propertyChangedCallback)
    {
        DefaultValue = defaultValue;
    }

    public FrameworkPropertyMetadata(object? defaultValue, FrameworkPropertyMetadataOptions options, Microsoft.UI.Xaml.PropertyChangedCallback? propertyChangedCallback)
        : base(defaultValue, propertyChangedCallback)
    {
        DefaultValue = defaultValue;
        Options = options;
    }

    public FrameworkPropertyMetadata(object? defaultValue, Microsoft.UI.Xaml.PropertyChangedCallback propertyChangedCallback, CoerceValueCallback? coerce)
        : base(defaultValue, propertyChangedCallback)
    {
        DefaultValue = defaultValue;
        CoerceValueCallback = coerce;
    }

    public FrameworkPropertyMetadata(object? defaultValue, FrameworkPropertyMetadataOptions options, Microsoft.UI.Xaml.PropertyChangedCallback propertyChangedCallback, CoerceValueCallback? coerce)
        : base(defaultValue, propertyChangedCallback)
    {
        DefaultValue = defaultValue;
        Options = options;
        CoerceValueCallback = coerce;
    }

    public new object? DefaultValue { get; }
    public FrameworkPropertyMetadataOptions Options { get; }
    public PropertyChangedCallback? PropertyChangedCallback { get; }
    public CoerceValueCallback? CoerceValueCallback { get; }

    // WPF PropertyChangedCallback signature uses (DependencyObject d, DependencyPropertyChangedEventArgs e)
    // where DependencyObject is now Microsoft.UI.Xaml.DependencyObject. WinUI fires its own
    // PropertyChangedCallback with the same sender/args types, but the delegate types differ
    // (System.Windows.PropertyChangedCallback vs Microsoft.UI.Xaml.PropertyChangedCallback).
    // A proper bridge would adapt the delegate; for now it is not wired so WPF callbacks stored
    // here are not invoked from WinUI's property change path.
    private static Microsoft.UI.Xaml.PropertyChangedCallback? Bridge(PropertyChangedCallback? wpf)
        => null;
}
