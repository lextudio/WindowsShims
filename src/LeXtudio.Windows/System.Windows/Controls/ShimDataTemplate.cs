namespace System.Windows.Controls;

// A DataTemplate subclass that holds a C# factory instead of XAML content.
// Used by DataGridDetailsPresenter to render row details without a parsed XAML template.
// BuildRowDetails detects ShimDataTemplate via DataGridHelper.TransferProperty and invokes
// Factory(item) directly, bypassing WinUI's ContentTemplate mechanism.
public sealed class ShimDataTemplate : Microsoft.UI.Xaml.DataTemplate, IWpfTemplateBridge
{
    public Func<object?, Microsoft.UI.Xaml.FrameworkElement?> Factory { get; }

    public Func<object?, Microsoft.UI.Xaml.DependencyObject?, Microsoft.UI.Xaml.FrameworkElement?> TemplatedParentFactory { get; }

    public Type? TargetType { get; }

    public ShimDataTemplate(Func<object?, Microsoft.UI.Xaml.FrameworkElement?> factory)
        : this(null, factory, factory is null ? null : (dataContext, _) => factory(dataContext))
    {
    }

    public ShimDataTemplate(
        Func<object?, Microsoft.UI.Xaml.DependencyObject?, Microsoft.UI.Xaml.FrameworkElement?> factory)
        : this(null, dataContext => factory(dataContext, null), factory)
    {
    }

    internal ShimDataTemplate(
        Type? targetType,
        Func<object?, Microsoft.UI.Xaml.FrameworkElement?> factory)
        : this(targetType, factory, factory is null ? null : (dataContext, _) => factory(dataContext))
    {
    }

    internal ShimDataTemplate(
        Type? targetType,
        Func<object?, Microsoft.UI.Xaml.FrameworkElement?> factory,
        Func<object?, Microsoft.UI.Xaml.DependencyObject?, Microsoft.UI.Xaml.FrameworkElement?>? templatedParentFactory)
    {
        TargetType = targetType;
        Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        TemplatedParentFactory = templatedParentFactory ?? ((dataContext, _) => Factory(dataContext));
    }

    Microsoft.UI.Xaml.FrameworkElement? IWpfTemplateBridge.LoadContent(object? dataContext)
        => Factory(dataContext);

    Microsoft.UI.Xaml.FrameworkElement? IWpfTemplateBridge.LoadContent(
        object? dataContext,
        Microsoft.UI.Xaml.DependencyObject? templatedParent)
        => TemplatedParentFactory(dataContext, templatedParent);
}
