namespace System.Windows.Controls;

/// <summary>
/// Minimal bridge metadata shared by WPF-style templates that cannot yet be
/// expressed as native WinUI templates.
/// </summary>
public interface IWpfTemplateBridge
{
    Type? TargetType { get; }

    Microsoft.UI.Xaml.FrameworkElement? LoadContent(object? dataContext);

    Microsoft.UI.Xaml.FrameworkElement? LoadContent(
        object? dataContext,
        Microsoft.UI.Xaml.DependencyObject? templatedParent);
}

public abstract class WpfTemplateBridge : IWpfTemplateBridge
{
    private readonly Func<object?, Microsoft.UI.Xaml.DependencyObject?, Microsoft.UI.Xaml.FrameworkElement?>? _factory;

    protected WpfTemplateBridge(
        Type? targetType = null,
        Func<object?, Microsoft.UI.Xaml.FrameworkElement?>? factory = null)
        : this(targetType, factory is null ? null : (dataContext, _) => factory(dataContext))
    {
    }

    protected WpfTemplateBridge(
        Type? targetType,
        Func<object?, Microsoft.UI.Xaml.DependencyObject?, Microsoft.UI.Xaml.FrameworkElement?>? factory)
    {
        TargetType = targetType;
        _factory = factory;
    }

    public Type? TargetType { get; }

    public Microsoft.UI.Xaml.FrameworkElement? LoadContent(object? dataContext)
        => LoadContent(dataContext, null);

    public Microsoft.UI.Xaml.FrameworkElement? LoadContent(
        object? dataContext,
        Microsoft.UI.Xaml.DependencyObject? templatedParent)
        => _factory?.Invoke(dataContext, templatedParent);
}
