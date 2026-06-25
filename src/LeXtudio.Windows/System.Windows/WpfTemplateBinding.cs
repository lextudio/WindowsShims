namespace System.Windows;

public static class WpfTemplateBinding
{
    public static void Apply(
        Microsoft.UI.Xaml.DependencyObject target,
        DependencyProperty targetProperty,
        Microsoft.UI.Xaml.DependencyObject templatedParent,
        DependencyProperty sourceProperty)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(targetProperty);
        ArgumentNullException.ThrowIfNull(templatedParent);
        ArgumentNullException.ThrowIfNull(sourceProperty);

        target.SetValue(targetProperty, templatedParent.GetValue(sourceProperty));
    }
}
