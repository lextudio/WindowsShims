namespace System.Windows.Controls;

public static class WpfStyleFactory
{
    public static Microsoft.UI.Xaml.Style Create(Type targetType, params SetterSpec[] setters)
        => Create(targetType, basedOn: null, setters);

    public static Microsoft.UI.Xaml.Style Create(
        Type targetType,
        Microsoft.UI.Xaml.Style? basedOn,
        params SetterSpec[] setters)
    {
        ArgumentNullException.ThrowIfNull(targetType);
        ArgumentNullException.ThrowIfNull(setters);

        var style = new Microsoft.UI.Xaml.Style(targetType);
        if (basedOn != null)
        {
            style.BasedOn = basedOn;
        }

        foreach (var setter in setters)
        {
            style.Setters.Add(setter.CreateSetter(targetType));
        }

        return style;
    }

    public static SetterSpec Set(Microsoft.UI.Xaml.DependencyProperty property, object? value)
        => new(property, value);

    public static SetterSpec Set(string propertyName, object? value)
        => new(propertyName, value);

    public static StyleSpec Style(Type targetType, params SetterSpec[] setters)
        => new(targetType, basedOn: null, setters);

    public static StyleSpec BasedOn(Type targetType, Microsoft.UI.Xaml.Style basedOn, params SetterSpec[] setters)
        => new(targetType, basedOn, setters);
}

public sealed class WpfResourceReference
{
    public WpfResourceReference(object key, bool isDynamic)
    {
        ArgumentNullException.ThrowIfNull(key);
        Key = key;
        IsDynamic = isDynamic;
    }

    public object Key { get; }

    public bool IsDynamic { get; }

    public static WpfResourceReference Static(object key) => new(key, isDynamic: false);

    public static WpfResourceReference Dynamic(object key) => new(key, isDynamic: true);
}

public sealed class StyleSpec
{
    public StyleSpec(
        Type targetType,
        Microsoft.UI.Xaml.Style? basedOn,
        WpfResourceReference? basedOnReference,
        params SetterSpec[] setters)
    {
        ArgumentNullException.ThrowIfNull(targetType);
        ArgumentNullException.ThrowIfNull(setters);
        TargetType = targetType;
        BasedOn = basedOn;
        BasedOnReference = basedOnReference;
        Setters = setters;
    }

    public StyleSpec(Type targetType, Microsoft.UI.Xaml.Style? basedOn, params SetterSpec[] setters)
        : this(targetType, basedOn, basedOnReference: null, setters)
    {
    }

    public Type TargetType { get; }

    public Microsoft.UI.Xaml.Style? BasedOn { get; }

    public WpfResourceReference? BasedOnReference { get; }

    public IReadOnlyList<SetterSpec> Setters { get; }

    public Microsoft.UI.Xaml.Style CreateStyle()
        => CreateStyle(resourceResolver: null);

    public Microsoft.UI.Xaml.Style CreateStyle(Func<object, object?>? resourceResolver)
    {
        var basedOn = BasedOn;
        if (basedOn == null && BasedOnReference != null && resourceResolver?.Invoke(BasedOnReference.Key) is Microsoft.UI.Xaml.Style resolved)
        {
            basedOn = resolved;
        }

        var style = new Microsoft.UI.Xaml.Style(TargetType);
        if (basedOn != null)
        {
            style.BasedOn = basedOn;
        }

        foreach (var setter in Setters)
        {
            style.Setters.Add(setter.CreateSetter(TargetType, resourceResolver));
        }

        return style;
    }
}

public sealed class SetterSpec
{
    private readonly Microsoft.UI.Xaml.DependencyProperty? _property;

    public SetterSpec(Microsoft.UI.Xaml.DependencyProperty property, object? value)
    {
        ArgumentNullException.ThrowIfNull(property);
        _property = property;
        PropertyName = property.Name;
        Value = value;
    }

    public SetterSpec(string propertyName, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        PropertyName = propertyName;
        Value = value;
    }

    public Microsoft.UI.Xaml.DependencyProperty? Property => _property;

    public string PropertyName { get; }

    public object? Value { get; }

    public Microsoft.UI.Xaml.Setter CreateSetter(Type? targetType = null, Func<object, object?>? resourceResolver = null)
    {
        var property = _property ?? ResolveDependencyProperty(targetType, PropertyName);
        if (property == null)
        {
            throw new InvalidOperationException($"Could not resolve dependency property '{PropertyName}'.");
        }

        var value = Value is WpfResourceReference reference
            ? resourceResolver?.Invoke(reference.Key) ?? Value
            : Value;
        return new(property, value);
    }

    public Microsoft.UI.Xaml.Setter CreateSetter()
        => CreateSetter(null);

    private static Microsoft.UI.Xaml.DependencyProperty? ResolveDependencyProperty(Type? targetType, string propertyName)
    {
        var propertyFieldName = propertyName.EndsWith("Property", StringComparison.Ordinal)
            ? propertyName
            : propertyName + "Property";

        for (var type = targetType; type != null; type = type.BaseType)
        {
            var property = ResolveDependencyPropertyOnType(type, propertyFieldName);
            if (property != null)
            {
                return property;
            }
        }

        return ResolveDependencyPropertyOnType(typeof(Microsoft.UI.Xaml.Controls.Control), propertyFieldName);
    }

    private static Microsoft.UI.Xaml.DependencyProperty? ResolveDependencyPropertyOnType(Type type, string propertyFieldName)
    {
        var field = type.GetField(
            propertyFieldName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        if (field?.GetValue(null) is Microsoft.UI.Xaml.DependencyProperty propertyFromField)
        {
            return propertyFromField;
        }

        var propertyInfo = type.GetProperty(
            propertyFieldName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return propertyInfo?.GetValue(null) as Microsoft.UI.Xaml.DependencyProperty;
    }
}
