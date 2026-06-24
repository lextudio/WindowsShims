namespace System.Windows.Controls;

// A DataTemplate subclass that holds a C# factory instead of XAML content.
// Used by DataGridDetailsPresenter to render row details without a parsed XAML template.
// BuildRowDetails detects ShimDataTemplate via DataGridHelper.TransferProperty and invokes
// Factory(item) directly, bypassing WinUI's ContentTemplate mechanism.
public sealed class ShimDataTemplate : Microsoft.UI.Xaml.DataTemplate, IWpfTemplateBridge
{
    public Func<object?, Microsoft.UI.Xaml.FrameworkElement?> Factory { get; }

    public Type? TargetType { get; }

    public ShimDataTemplate(Func<object?, Microsoft.UI.Xaml.FrameworkElement?> factory)
        : this(null, factory)
    {
    }

    internal ShimDataTemplate(
        Type? targetType,
        Func<object?, Microsoft.UI.Xaml.FrameworkElement?> factory)
    {
        TargetType = targetType;
        Factory = factory;
    }

    Microsoft.UI.Xaml.FrameworkElement? IWpfTemplateBridge.LoadContent(object? dataContext)
        => Factory(dataContext);
}
