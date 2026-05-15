namespace System.Windows;

/// <summary>
/// WPF-compatible FrameworkPropertyMetadata.
///
/// Stores dependency property metadata in WPF style format.
/// Callbacks and options are preserved as-is for use by the property system.
/// </summary>
public sealed class FrameworkPropertyMetadata
{
    public FrameworkPropertyMetadata(object? defaultValue)
    {
        DefaultValue = defaultValue;
        Options = FrameworkPropertyMetadataOptions.None;
        PropertyChangedCallback = null;
        CoerceValueCallback = null;
    }

    public FrameworkPropertyMetadata(object? defaultValue, FrameworkPropertyMetadataOptions options)
    {
        DefaultValue = defaultValue;
        Options = options;
        PropertyChangedCallback = null;
        CoerceValueCallback = null;
    }

    public FrameworkPropertyMetadata(object? defaultValue, FrameworkPropertyMetadataOptions options, PropertyChangedCallback? changed)
    {
        DefaultValue = defaultValue;
        Options = options;
        PropertyChangedCallback = changed;
        CoerceValueCallback = null;
    }

    public FrameworkPropertyMetadata(object? defaultValue, PropertyChangedCallback? changed)
    {
        DefaultValue = defaultValue;
        Options = FrameworkPropertyMetadataOptions.None;
        PropertyChangedCallback = changed;
        CoerceValueCallback = null;
    }

    public FrameworkPropertyMetadata(object? defaultValue, PropertyChangedCallback? changed, CoerceValueCallback? coerce)
    {
        DefaultValue = defaultValue;
        Options = FrameworkPropertyMetadataOptions.None;
        PropertyChangedCallback = changed;
        CoerceValueCallback = coerce;
    }

    public FrameworkPropertyMetadata(object? defaultValue, FrameworkPropertyMetadataOptions options, PropertyChangedCallback? changed, CoerceValueCallback? coerce)
    {
        DefaultValue = defaultValue;
        Options = options;
        PropertyChangedCallback = changed;
        CoerceValueCallback = coerce;
    }

    public object? DefaultValue { get; }
    public FrameworkPropertyMetadataOptions Options { get; }
    public PropertyChangedCallback? PropertyChangedCallback { get; }
    public CoerceValueCallback? CoerceValueCallback { get; }
}
