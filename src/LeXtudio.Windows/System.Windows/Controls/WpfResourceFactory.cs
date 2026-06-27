namespace System.Windows.Controls;

public static class WpfResourceFactory
{
    public static void Populate(System.Windows.ResourceDictionary dictionary, params WpfResourceSpec[] resources)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentNullException.ThrowIfNull(resources);

        foreach (var resource in resources)
        {
            dictionary[resource.Key] = resource.CreateValue(dictionary);
        }
    }

    public static IEnumerable<(object Key, object Value)> CreateMany(params WpfResourceSpec[] resources)
    {
        ArgumentNullException.ThrowIfNull(resources);

        foreach (var resource in resources)
        {
            yield return (resource.Key, resource.CreateValue());
        }
    }
}

public sealed class WpfResourceSpec
{
    private readonly Func<System.Windows.ResourceDictionary?, object> _createValue;

    private WpfResourceSpec(object key, Func<System.Windows.ResourceDictionary?, object> createValue, object? descriptor = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(createValue);
        Key = key;
        _createValue = createValue;
        Descriptor = descriptor;
    }

    public object Key { get; }

    public object? Descriptor { get; }

    public object CreateValue() => _createValue(null);

    internal object CreateValue(System.Windows.ResourceDictionary dictionary) => _createValue(dictionary);

    public static WpfResourceSpec Value(object key, object value)
        => new(key, _ => value, value);

    public static WpfResourceSpec Value(object key, Func<object> valueFactory, object? descriptor = null)
    {
        ArgumentNullException.ThrowIfNull(valueFactory);
        return new(key, _ => valueFactory(), descriptor);
    }

    public static WpfResourceSpec Style(object key, Type targetType, params SetterSpec[] setters)
        => Style(key, WpfStyleFactory.Style(targetType, setters));

    public static WpfResourceSpec Style(object key, StyleSpec style)
    {
        ArgumentNullException.ThrowIfNull(style);
        return new(key, dictionary => style.CreateStyle(resourceKey =>
        {
            if (dictionary is not null && dictionary.ContainsKey(resourceKey))
            {
                return dictionary[resourceKey];
            }

            return null;
        }), style);
    }

    public static WpfResourceSpec Filter(object key, DataGridExtensions.FilterKind kind, Type? flagsType = null)
        => new(key, _ => new DataGridExtensions.FilterControlTemplate(kind, flagsType));

    public static WpfResourceSpec TextFilter(object key)
        => Filter(key, DataGridExtensions.FilterKind.Text);

    public static WpfResourceSpec HexFilter(object key)
        => Filter(key, DataGridExtensions.FilterKind.Hex);

    public static WpfResourceSpec FlagsFilter(object key, Type flagsType)
    {
        ArgumentNullException.ThrowIfNull(flagsType);
        return Filter(key, DataGridExtensions.FilterKind.Flags, flagsType);
    }

    public static WpfResourceSpec DataTemplate(
        object key,
        Func<object?, Microsoft.UI.Xaml.DependencyObject?, Microsoft.UI.Xaml.FrameworkElement?> factory)
        => new(key, _ => new ShimDataTemplate(factory));

    public static WpfResourceSpec DataTemplate(
        object key,
        Func<System.Windows.ResourceDictionary?, object?, Microsoft.UI.Xaml.DependencyObject?, Microsoft.UI.Xaml.FrameworkElement?> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return new(key, dictionary => new ShimDataTemplate((item, templatedParent) => factory(dictionary, item, templatedParent)));
    }
}
