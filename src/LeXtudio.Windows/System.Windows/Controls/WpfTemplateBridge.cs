namespace System.Windows.Controls;

/// <summary>
/// Minimal bridge metadata shared by WPF-style templates that cannot yet be
/// expressed as native WinUI templates.
/// </summary>
public interface IWpfTemplateBridge
{
    Type? TargetType { get; }

    Microsoft.UI.Xaml.FrameworkElement? LoadContent(object? dataContext);
}

public abstract class WpfTemplateBridge : IWpfTemplateBridge
{
    private readonly Func<object?, Microsoft.UI.Xaml.FrameworkElement?>? _factory;

    protected WpfTemplateBridge(
        Type? targetType = null,
        Func<object?, Microsoft.UI.Xaml.FrameworkElement?>? factory = null)
    {
        TargetType = targetType;
        _factory = factory;
    }

    public Type? TargetType { get; }

    public Microsoft.UI.Xaml.FrameworkElement? LoadContent(object? dataContext)
        => _factory?.Invoke(dataContext);
}
