namespace System.Windows;

// System.Windows.DependencyProperty class removed — use Microsoft.UI.Xaml.DependencyProperty
// (the global using alias 'DependencyProperty' resolves to it). WPF-specific extensions
// (AddOwner, OverrideMetadata) are provided as extension methods in WinUIDependencyPropertyExtensions.cs.

// WPF delegate signatures kept for source compatibility with linked WPF code.
public delegate void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e);
public delegate object CoerceValueCallback(DependencyObject d, object baseValue);
public delegate bool ValidateValueCallback(object value);

public readonly struct DependencyPropertyChangedEventArgs
{
    public object? OldValue { get; init; }
    public object? NewValue { get; init; }
    public Entry NewEntry { get; init; }
}

public readonly struct Entry
{
    public bool IsDeferredReference { get; init; }
}

[System.Flags]
public enum FrameworkPropertyMetadataOptions
{
    None = 0,
    AffectsRender = 1,
    AffectsMeasure = 2,
    AffectsParentMeasure = 4,
    BindsTwoWayByDefault = 8,
    Inherits = 16
}

public readonly struct ValueSource
{
    public BaseValueSource BaseValueSource { get; init; }
    public bool IsExpression { get; init; }
}

public enum BaseValueSource
{
    Local = 0
}

public static class DependencyPropertyHelper
{
    public static ValueSource GetValueSource(DependencyObject obj, Microsoft.UI.Xaml.DependencyProperty dp) => new() { BaseValueSource = BaseValueSource.Local, IsExpression = false };
}
