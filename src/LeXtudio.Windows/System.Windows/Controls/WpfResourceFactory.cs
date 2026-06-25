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

    public static IEnumerable<(string Key, object Value)> CreateMany(params WpfResourceSpec[] resources)
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

    private WpfResourceSpec(string key, Func<System.Windows.ResourceDictionary?, object> createValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(createValue);
        Key = key;
        _createValue = createValue;
    }

    public string Key { get; }

    public object CreateValue() => _createValue(null);

    internal object CreateValue(System.Windows.ResourceDictionary dictionary) => _createValue(dictionary);

    public static WpfResourceSpec Value(string key, object value)
        => new(key, _ => value);

    public static WpfResourceSpec Style(string key, Type targetType, params SetterSpec[] setters)
        => new(key, _ => WpfStyleFactory.Create(targetType, setters));

    public static WpfResourceSpec Style(string key, StyleSpec style)
    {
        ArgumentNullException.ThrowIfNull(style);
        return new(key, dictionary => style.CreateStyle(resourceKey =>
        {
            if (dictionary is not null && dictionary.ContainsKey(resourceKey))
            {
                return dictionary[resourceKey];
            }

            return null;
        }));
    }

    public static WpfResourceSpec Filter(string key, DataGridExtensions.FilterKind kind, Type? flagsType = null)
        => new(key, _ => new DataGridExtensions.FilterControlTemplate(kind, flagsType));

    public static WpfResourceSpec TextFilter(string key)
        => Filter(key, DataGridExtensions.FilterKind.Text);

    public static WpfResourceSpec HexFilter(string key)
        => Filter(key, DataGridExtensions.FilterKind.Hex);

    public static WpfResourceSpec FlagsFilter(string key, Type flagsType)
    {
        ArgumentNullException.ThrowIfNull(flagsType);
        return Filter(key, DataGridExtensions.FilterKind.Flags, flagsType);
    }

    public static WpfResourceSpec DataTemplate(
        string key,
        Func<object?, Microsoft.UI.Xaml.DependencyObject?, Microsoft.UI.Xaml.FrameworkElement?> factory)
        => new(key, _ => new ShimDataTemplate(factory));
}
