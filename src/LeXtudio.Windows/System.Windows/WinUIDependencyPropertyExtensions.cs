namespace System.Windows;

/// <summary>
/// WPF compatibility extension methods on Microsoft.UI.Xaml.DependencyProperty.
/// AddOwner is a WPF API for sharing a DependencyProperty across multiple owner types;
/// WinUI/Uno doesn't natively support this so we return the same instance.
/// Used by WPF source files like TableColumn that call Panel.BackgroundProperty.AddOwner(...).
/// </summary>
public static class WinUIDependencyPropertyExtensions
{
    public static Microsoft.UI.Xaml.DependencyProperty AddOwner(
        this Microsoft.UI.Xaml.DependencyProperty property,
        System.Type ownerType) => property;

    public static Microsoft.UI.Xaml.DependencyProperty AddOwner(
        this Microsoft.UI.Xaml.DependencyProperty property,
        System.Type ownerType,
        Microsoft.UI.Xaml.PropertyMetadata typeMetadata) => property;

    // Accept WPF-style FrameworkPropertyMetadata too (used by linked WPF source files).
    public static Microsoft.UI.Xaml.DependencyProperty AddOwner(
        this Microsoft.UI.Xaml.DependencyProperty property,
        System.Type ownerType,
        FrameworkPropertyMetadata typeMetadata) => property;
}
