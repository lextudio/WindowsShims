using System.Globalization;
using System.Windows.Data;
using System.Xml.Linq;

namespace System.Windows.Controls;

public static class WpfXamlResourceTranslator
{
    private static readonly XNamespace PresentationNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    public static WpfResourceSpec[] TranslateResourceDictionary(
        string xaml,
        Func<string, Type?> typeResolver,
        params WpfResourceSpec[] fallbackResources)
        => TranslateResourceDictionary(xaml, typeResolver, out _, fallbackResources);

    public static WpfResourceSpec[] TranslateResourceDictionary(
        string xaml,
        Func<string, Type?> typeResolver,
        out WpfXamlResourceTranslationReport report,
        params WpfResourceSpec[] fallbackResources)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xaml);
        ArgumentNullException.ThrowIfNull(typeResolver);
        ArgumentNullException.ThrowIfNull(fallbackResources);

        var document = XDocument.Parse(xaml, LoadOptions.PreserveWhitespace);
        var specs = new List<WpfResourceSpec>();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var translatedKeys = new List<string>();
        var fallbackKeys = new List<string>();
        var skippedKeys = new List<string>();

        foreach (var element in document.Root?.Elements() ?? [])
        {
            var key = GetKey(element);
            var spec = TranslateElement(element, typeResolver);
            if (spec == null || !keys.Add(spec.Key))
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    skippedKeys.Add(key);
                }

                continue;
            }

            specs.Add(spec);
            translatedKeys.Add(spec.Key);
        }

        foreach (var fallback in fallbackResources)
        {
            if (keys.Add(fallback.Key))
            {
                specs.Add(fallback);
                fallbackKeys.Add(fallback.Key);
            }
        }

        report = new WpfXamlResourceTranslationReport(translatedKeys, fallbackKeys, skippedKeys);
        return specs.ToArray();
    }

    private static WpfResourceSpec? TranslateElement(XElement element, Func<string, Type?> typeResolver)
    {
        var key = GetKey(element);
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        if (element.Name == PresentationNamespace + "Style")
        {
            return TranslateStyle(key, element, typeResolver);
        }

        if (element.Name == PresentationNamespace + "ControlTemplate")
        {
            return TranslateControlTemplate(key, element, typeResolver);
        }

        if (element.Name == PresentationNamespace + "DataTemplate")
        {
            return TranslateDataTemplate(key, element);
        }

        return TranslateObjectResource(key, element, typeResolver);
    }

    private static WpfResourceSpec? TranslateObjectResource(string key, XElement element, Func<string, Type?> typeResolver)
    {
        var type = typeResolver(element.Name.LocalName) ?? typeResolver("local:" + element.Name.LocalName);
        if (type == null)
        {
            return null;
        }

        object? value = null;
        var instanceField = type.GetField(
            "Instance",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        if (instanceField != null && type.IsInstanceOfType(instanceField.GetValue(null)))
        {
            value = instanceField.GetValue(null);
        }

        var instanceProperty = type.GetProperty(
            "Instance",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        if (value == null && instanceProperty != null && type.IsInstanceOfType(instanceProperty.GetValue(null)))
        {
            value = instanceProperty.GetValue(null);
        }

        value ??= Activator.CreateInstance(type, nonPublic: true);
        return value == null ? null : WpfResourceSpec.Value(key, value);
    }

    private static WpfResourceSpec? TranslateStyle(string key, XElement styleElement, Func<string, Type?> typeResolver)
    {
        var targetTypeText = styleElement.Attribute("TargetType")?.Value;
        var targetType = ResolveXamlType(targetTypeText, typeResolver);
        if (targetType == null)
        {
            return null;
        }

        var basedOn = ParseResourceReference(styleElement.Attribute("BasedOn")?.Value);
        var setters = new List<SetterSpec>();
        foreach (var setterElement in styleElement.Elements(PresentationNamespace + "Setter"))
        {
            var propertyName = setterElement.Attribute("Property")?.Value;
            var valueText = setterElement.Attribute("Value")?.Value;
            if (string.IsNullOrWhiteSpace(propertyName) || valueText == null)
            {
                continue;
            }

            setters.Add(WpfStyleFactory.Set(propertyName, ConvertSetterValue(valueText)));
        }

        if (setters.Count == 0)
        {
            return null;
        }

        return WpfResourceSpec.Style(key, new StyleSpec(targetType, basedOn: null, basedOn, setters.ToArray()));
    }

    private static WpfResourceSpec? TranslateControlTemplate(string key, XElement templateElement, Func<string, Type?> typeResolver)
    {
        if (templateElement.Descendants(PresentationNamespace + "TextBox").Any())
        {
            return WpfResourceSpec.TextFilter(key);
        }

        if (templateElement.Descendants().Any(element => element.Name.LocalName == "HexFilterControl"))
        {
            return WpfResourceSpec.HexFilter(key);
        }

        var flagsControl = templateElement.Descendants().FirstOrDefault(element => element.Name.LocalName == "FlagsFilterControl");
        var flagsTypeText = flagsControl?.Attribute("FlagsType")?.Value;
        var flagsType = ResolveXamlType(flagsTypeText, typeResolver);
        return flagsType == null ? null : WpfResourceSpec.FlagsFilter(key, flagsType);
    }

    private static WpfResourceSpec? TranslateDataTemplate(string key, XElement templateElement)
    {
        var visualRoot = templateElement.Elements().FirstOrDefault();
        if (visualRoot == null)
        {
            return null;
        }

        if (visualRoot.Name == PresentationNamespace + "DataGrid")
        {
            return TranslateDataGridDataTemplate(key, visualRoot);
        }

        var textBoxElement = visualRoot.Name == PresentationNamespace + "TextBox"
            ? visualRoot
            : visualRoot.Descendants(PresentationNamespace + "TextBox").FirstOrDefault();
        if (textBoxElement == null)
        {
            return null;
        }

        var textBinding = ParseBinding(textBoxElement.Attribute("Text")?.Value);
        if (textBinding == null)
        {
            return null;
        }

        textBinding.TargetNullValue ??= string.Empty;
        var assignments = new[] { BindingAssignment.To(nameof(Microsoft.UI.Xaml.Controls.TextBox.Text), textBinding) };
        var textBoxValues = ReadSimplePropertyValues(visualRoot, textBoxElement);
        return WpfResourceSpec.DataTemplate(key, (item, _) =>
            WpfTemplateFactory.Create<Microsoft.UI.Xaml.Controls.TextBox>(
                item,
                textBox => ApplySimplePropertyValues(textBox, textBoxValues),
                assignments));
    }

    private static WpfResourceSpec? TranslateDataGridDataTemplate(string key, XElement dataGridElement)
    {
        var itemsSourceBinding = ParseBinding(dataGridElement.Attribute("ItemsSource")?.Value);
        if (itemsSourceBinding == null)
        {
            return null;
        }

        var values = ReadSimplePropertyValues(dataGridElement);
        return WpfResourceSpec.DataTemplate(key, (item, _) =>
            WpfTemplateFactory.Create<DataGrid>(
                item,
                grid => ApplySimplePropertyValues(grid, values),
                BindingAssignment.To(nameof(DataGrid.ItemsSource), itemsSourceBinding)));
    }

    private static string? GetKey(XElement element)
        => element.Attribute(XamlNamespace + "Key")?.Value;

    private static Type? ResolveXamlType(string? value, Func<string, Type?> typeResolver)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.Trim();
        if (text.StartsWith("{x:Type ", StringComparison.Ordinal) && text.EndsWith('}'))
        {
            text = text.Substring("{x:Type ".Length, text.Length - "{x:Type ".Length - 1).Trim();
        }

        return typeResolver(text);
    }

    private static object ConvertSetterValue(string value)
    {
        var text = value.Trim();
        var resourceReference = ParseResourceReference(text);
        if (resourceReference != null)
        {
            return resourceReference;
        }

        if (bool.TryParse(text, out var boolean))
        {
            return boolean;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return new Microsoft.UI.Xaml.Thickness(number);
        }

        if (Enum.TryParse<Microsoft.UI.Xaml.VerticalAlignment>(text, out var verticalAlignment))
        {
            return verticalAlignment;
        }

        if (Enum.TryParse<Microsoft.UI.Xaml.HorizontalAlignment>(text, out var horizontalAlignment))
        {
            return horizontalAlignment;
        }

        return text;
    }

    private static WpfResourceReference? ParseResourceReference(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.Trim();
        var isStatic = text.StartsWith("{StaticResource ", StringComparison.Ordinal) && text.EndsWith('}');
        var isDynamic = text.StartsWith("{DynamicResource ", StringComparison.Ordinal) && text.EndsWith('}');
        if (!isStatic && !isDynamic)
        {
            return null;
        }

        var prefixLength = isStatic ? "{StaticResource ".Length : "{DynamicResource ".Length;
        var keyText = text.Substring(prefixLength, text.Length - prefixLength - 1).Trim();
        object key = keyText;
        if (keyText.StartsWith("{x:Type ", StringComparison.Ordinal) && keyText.EndsWith('}'))
        {
            key = keyText.Substring("{x:Type ".Length, keyText.Length - "{x:Type ".Length - 1).Trim();
        }
        else if (keyText.StartsWith("{x:Static ", StringComparison.Ordinal) && keyText.EndsWith('}'))
        {
            key = keyText.Substring("{x:Static ".Length, keyText.Length - "{x:Static ".Length - 1).Trim();
        }

        return isStatic ? WpfResourceReference.Static(key) : WpfResourceReference.Dynamic(key);
    }

    private static Binding? ParseBinding(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.Trim();
        if (!text.StartsWith("{Binding", StringComparison.Ordinal) || !text.EndsWith('}'))
        {
            return null;
        }

        var body = text.Substring("{Binding".Length, text.Length - "{Binding".Length - 1).Trim();
        if (string.IsNullOrWhiteSpace(body))
        {
            return new Binding();
        }

        string? path = null;
        foreach (var segment in body.Split(','))
        {
            var part = segment.Trim();
            if (part.StartsWith("Path=", StringComparison.Ordinal))
            {
                path = part.Substring("Path=".Length).Trim();
            }
            else if (!part.Contains('=', StringComparison.Ordinal))
            {
                path = part;
            }
        }

        return string.IsNullOrWhiteSpace(path) ? new Binding() : new Binding(path);
    }

    private static Dictionary<string, object?> ReadSimplePropertyValues(params XElement[] elements)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var element in elements)
        {
            foreach (var attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration || attribute.Name.NamespaceName.Length != 0)
                {
                    continue;
                }

                var propertyName = attribute.Name.LocalName;
                if (propertyName is "Text" or "Name" or "ItemsSource")
                {
                    continue;
                }

                if (LooksLikeMarkupExtension(attribute.Value))
                {
                    continue;
                }

                values[propertyName] = ConvertSimpleValue(attribute.Value);
            }
        }

        return values;
    }

    private static object? ConvertSimpleValue(string value)
    {
        var text = value.Trim();
        if (bool.TryParse(text, out var boolean))
        {
            return boolean;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return number;
        }

        if (Enum.TryParse<Microsoft.UI.Xaml.TextWrapping>(text, out var textWrapping))
        {
            return textWrapping;
        }

        if (Enum.TryParse<Microsoft.UI.Xaml.HorizontalAlignment>(text, out var horizontalAlignment))
        {
            return horizontalAlignment;
        }

        return text;
    }

    private static void ApplySimplePropertyValues(object target, IReadOnlyDictionary<string, object?> values)
    {
        var targetType = target.GetType();
        foreach (var (propertyName, value) in values)
        {
            var property = targetType.GetProperty(propertyName);
            if (property == null || !property.CanWrite)
            {
                continue;
            }

            var converted = ConvertToPropertyType(value, property.PropertyType);
            if (CanAssignValue(converted, property.PropertyType))
            {
                property.SetValue(target, converted);
            }
        }
    }

    private static object? ConvertToPropertyType(object? value, Type propertyType)
    {
        if (value == null || propertyType.IsInstanceOfType(value))
        {
            return value;
        }

        var targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        if (targetType.IsEnum && value is string text)
        {
            return Enum.Parse(targetType, text);
        }

        if (targetType == typeof(double) && value is IConvertible convertible)
        {
            return convertible.ToDouble(CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(int) && value is IConvertible intConvertible)
        {
            return intConvertible.ToInt32(CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(bool) && value is IConvertible boolConvertible)
        {
            return boolConvertible.ToBoolean(CultureInfo.InvariantCulture);
        }

        return value;
    }

    private static bool LooksLikeMarkupExtension(string value)
    {
        var text = value.Trim();
        return text.StartsWith('{') && text.EndsWith('}');
    }

    private static bool CanAssignValue(object? value, Type propertyType)
    {
        if (value == null)
        {
            return Nullable.GetUnderlyingType(propertyType) != null || !propertyType.IsValueType;
        }

        return propertyType.IsInstanceOfType(value);
    }
}

public sealed class WpfXamlResourceTranslationReport
{
    public WpfXamlResourceTranslationReport(
        IReadOnlyList<string> translatedKeys,
        IReadOnlyList<string> fallbackKeys,
        IReadOnlyList<string> skippedKeys)
    {
        TranslatedKeys = translatedKeys;
        FallbackKeys = fallbackKeys;
        SkippedKeys = skippedKeys;
    }

    public IReadOnlyList<string> TranslatedKeys { get; }

    public IReadOnlyList<string> FallbackKeys { get; }

    public IReadOnlyList<string> SkippedKeys { get; }
}
