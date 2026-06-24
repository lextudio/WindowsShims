#if WINDOWS_APP_SDK
namespace System.Windows;

public class StaticResourceExtension
{
    public StaticResourceExtension()
    {
    }

    public StaticResourceExtension(object resourceKey)
    {
        ResourceKey = resourceKey;
    }

    public object? ResourceKey { get; set; }

    public virtual object? ProvideValue(IServiceProvider serviceProvider) => ResourceKey;
}

public class DynamicResourceExtension
{
    public DynamicResourceExtension()
    {
    }

    public DynamicResourceExtension(object resourceKey)
    {
        ResourceKey = resourceKey;
    }

    public object? ResourceKey { get; set; }

    public virtual object? ProvideValue(IServiceProvider serviceProvider) => ResourceKey;
}

public class TemplateBindingExtension
{
    public TemplateBindingExtension()
    {
    }

    public TemplateBindingExtension(DependencyProperty property)
    {
        Property = property;
    }

    public DependencyProperty? Property { get; set; }

    public object? Converter { get; set; }

    public object? ConverterParameter { get; set; }

    public virtual object ProvideValue(IServiceProvider serviceProvider)
        => new TemplateBindingExpression(this);
}

public sealed class TemplateBindingExpression
{
    public TemplateBindingExpression(TemplateBindingExtension templateBindingExtension)
    {
        TemplateBindingExtension = templateBindingExtension;
    }

    public TemplateBindingExtension TemplateBindingExtension { get; }
}
#endif
