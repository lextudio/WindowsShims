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
        var keys = new HashSet<object>();
        var translatedKeys = new List<string>();
        var fallbackKeys = new List<string>();
        var skippedKeys = new List<string>();

        foreach (var element in GetResourceElements(document.Root))
        {
            var key = GetResourceKey(element, typeResolver);
            var spec = TranslateElement(element, typeResolver);
            if (spec == null || !keys.Add(spec.Key))
            {
                if (key != null)
                {
                    skippedKeys.Add(FormatReportKey(key));
                }

                continue;
            }

            specs.Add(spec);
            translatedKeys.Add(FormatReportKey(spec.Key));
        }

        foreach (var fallback in fallbackResources)
        {
            if (keys.Add(fallback.Key))
            {
                specs.Add(fallback);
                fallbackKeys.Add(FormatReportKey(fallback.Key));
            }
        }

        report = new WpfXamlResourceTranslationReport(translatedKeys, fallbackKeys, skippedKeys);
        return specs.ToArray();
    }

    private static WpfResourceSpec? TranslateElement(XElement element, Func<string, Type?> typeResolver)
    {
        var key = GetResourceKey(element, typeResolver);
        if (key == null)
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

        if (element.Name == PresentationNamespace + "AlternationConverter")
        {
            return TranslateAlternationConverter(key, element);
        }

        if (element.Name == PresentationNamespace + "SolidColorBrush")
        {
            return TranslateSolidColorBrush(key, element);
        }

        return TranslateObjectResource(key, element, typeResolver);
    }

    private static IEnumerable<XElement> GetResourceElements(XElement? root)
    {
        if (root == null)
        {
            return [];
        }

        if (root.Name == PresentationNamespace + "ResourceDictionary")
        {
            return root.Elements();
        }

        var resourcesElements = root
            .DescendantsAndSelf()
            .SelectMany(element => element.Elements(element.Name.Namespace + element.Name.LocalName + ".Resources"));
        return resourcesElements.SelectMany(element => element.Elements());
    }

    private static WpfResourceSpec? TranslateAlternationConverter(object key, XElement converterElement)
    {
        var brushes = new List<SolidColorBrushSpec>();
        foreach (var child in converterElement.Elements())
        {
            if (child.Name == PresentationNamespace + "SolidColorBrush"
                && ReadSolidColorBrush(child) is { } brush)
            {
                brushes.Add(brush);
                continue;
            }

            return null;
        }

        return WpfResourceSpec.Value(
            key,
            () =>
            {
                var converter = new AlternationConverter();
                foreach (var brush in brushes)
                {
                    converter.Add(CreateSolidColorBrush(brush));
                }

                return converter;
            },
            new AlternationConverterSpec(brushes.ToArray()));
    }

    private static WpfResourceSpec? TranslateSolidColorBrush(object key, XElement brushElement)
        => ReadSolidColorBrush(brushElement) is { } brush
            ? WpfResourceSpec.Value(key, () => CreateSolidColorBrush(brush), brush)
            : null;

    private static WpfResourceSpec? TranslateObjectResource(object key, XElement element, Func<string, Type?> typeResolver)
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

    private static WpfResourceSpec? TranslateStyle(object key, XElement styleElement, Func<string, Type?> typeResolver)
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
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                continue;
            }

            var valueText = setterElement.Attribute("Value")?.Value;
            if (valueText != null)
            {
                setters.Add(WpfStyleFactory.Set(propertyName, ConvertSetterValue(valueText)));
                continue;
            }

            if (TranslateSetterElementValue(setterElement, typeResolver) is { } setterValue)
            {
                setters.Add(WpfStyleFactory.Set(propertyName, setterValue));
            }
        }

        if (setters.Count == 0)
        {
            return null;
        }

        return WpfResourceSpec.Style(key, new StyleSpec(targetType, basedOn: null, basedOn, setters.ToArray()));
    }

    private static object? TranslateSetterElementValue(XElement setterElement, Func<string, Type?> typeResolver)
    {
        var valueElement = setterElement.Element(PresentationNamespace + "Setter.Value")?.Elements().FirstOrDefault();
        if (valueElement?.Name == PresentationNamespace + "ControlTemplate")
        {
            var targetType = ResolveXamlType(valueElement.Attribute("TargetType")?.Value, typeResolver);
            var template = new ControlTemplate(targetType);
            template.VisualTree = valueElement;
            return template;
        }

        if (valueElement?.Name == PresentationNamespace + "ContextMenu")
        {
            return TranslateContextMenu(valueElement);
        }

        return null;
    }

    private static ContextMenuSpec TranslateContextMenu(XElement contextMenuElement)
        => new(contextMenuElement
            .Elements(PresentationNamespace + "MenuItem")
            .Select(itemElement => new MenuItemSpec(
                itemElement.Attribute("Header")?.Value,
                itemElement.Attribute("Command")?.Value,
                itemElement.Attribute("CommandParameter")?.Value,
                itemElement.Attribute("InputGestureText")?.Value))
            .ToArray());

    private static WpfResourceSpec? TranslateControlTemplate(object key, XElement templateElement, Func<string, Type?> typeResolver)
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

    private static WpfResourceSpec? TranslateDataTemplate(object key, XElement templateElement)
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

        if (visualRoot.Name == PresentationNamespace + "StackPanel")
        {
            return TranslateStackPanelDataTemplate(key, visualRoot);
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

    private static WpfResourceSpec? TranslateDataGridDataTemplate(object key, XElement dataGridElement)
    {
        var itemsSourceBinding = ParseBinding(dataGridElement.Attribute("ItemsSource")?.Value);
        if (itemsSourceBinding == null)
        {
            return null;
        }

        var values = ReadSimplePropertyValues(dataGridElement);
        var resourceValues = ReadResourcePropertyValues(dataGridElement);
        return WpfResourceSpec.DataTemplate(key, (resources, item, _) =>
            WpfTemplateFactory.Create<DataGrid>(
                item,
                grid =>
                {
                    ApplySimplePropertyValues(grid, values);
                    ApplyResourcePropertyValues(grid, resourceValues, resources);
                },
                BindingAssignment.To(nameof(DataGrid.ItemsSource), itemsSourceBinding)));
    }

    private static WpfResourceSpec? TranslateStackPanelDataTemplate(object key, XElement stackPanelElement)
    {
        var template = CreateElementFactory(stackPanelElement);
        if (template == null)
        {
            return null;
        }

        return WpfResourceSpec.DataTemplate(key, (item, _) => template(item));
    }

    private static Func<object?, Microsoft.UI.Xaml.FrameworkElement?>? CreateElementFactory(XElement element)
    {
        if (element.Name == PresentationNamespace + "StackPanel")
        {
            var childFactories = new List<Func<object?, Microsoft.UI.Xaml.FrameworkElement?>>();
            foreach (var child in element.Elements())
            {
                var childFactory = CreateElementFactory(child);
                if (childFactory == null)
                {
                    return null;
                }

                childFactories.Add(childFactory);
            }

            var values = ReadSimplePropertyValues(element);
            return dataContext =>
            {
                var panel = new Microsoft.UI.Xaml.Controls.StackPanel();
                ApplySimplePropertyValues(panel, values);
                foreach (var childFactory in childFactories)
                {
                    if (childFactory(dataContext) is { } child)
                    {
                        panel.Children.Add(child);
                    }
                }

                return panel;
            };
        }

        if (element.Name == PresentationNamespace + "TextBlock")
        {
            var values = ReadSimplePropertyValues(element);
            var textBinding = ParseBinding(element.Attribute("Text")?.Value);
            var text = textBinding == null && !LooksLikeMarkupExtension(element.Attribute("Text")?.Value ?? string.Empty)
                ? element.Attribute("Text")?.Value
                : null;
            return dataContext =>
            {
                var textBlock = new Microsoft.UI.Xaml.Controls.TextBlock();
                ApplySimplePropertyValues(textBlock, values);
                if (textBinding != null)
                {
                    BindingAssignment.To(nameof(Microsoft.UI.Xaml.Controls.TextBlock.Text), textBinding)
                        .Apply(textBlock, dataContext);
                }
                else if (text != null)
                {
                    textBlock.Text = text;
                }

                return textBlock;
            };
        }

        if (element.Name == PresentationNamespace + "ListBox")
        {
            var values = ReadSimplePropertyValues(element);
            var itemsSourceBinding = ParseBinding(element.Attribute("ItemsSource")?.Value);
            var itemTemplateElement = element.Element(PresentationNamespace + "ListBox.ItemTemplate")
                ?.Elements(PresentationNamespace + "DataTemplate")
                .FirstOrDefault();
            Func<object?, Microsoft.UI.Xaml.FrameworkElement?>? itemTemplateFactory = null;
            if (itemTemplateElement != null)
            {
                var itemTemplateRoot = itemTemplateElement.Elements().FirstOrDefault();
                if (itemTemplateRoot == null)
                {
                    return null;
                }

                itemTemplateFactory = CreateElementFactory(itemTemplateRoot);
                if (itemTemplateFactory == null)
                {
                    return null;
                }
            }

            return dataContext =>
            {
                var listView = new Microsoft.UI.Xaml.Controls.ListView();
                ApplySimplePropertyValues(listView, values);
                if (itemsSourceBinding != null)
                {
                    BindingAssignment.To(nameof(Microsoft.UI.Xaml.Controls.ListView.ItemsSource), itemsSourceBinding)
                        .Apply(listView, dataContext);
                }

                if (itemTemplateFactory != null)
                {
                    listView.ItemTemplate = new ShimDataTemplate(item => itemTemplateFactory(item));
                }

                return listView;
            };
        }

        if (element.Name == PresentationNamespace + "CheckBox")
        {
            var values = ReadSimplePropertyValues(element);
            var contentBinding = ParseBinding(element.Attribute("Content")?.Value);
            var checkedBinding = ParseBinding(element.Attribute("IsChecked")?.Value);
            var content = contentBinding == null && !LooksLikeMarkupExtension(element.Attribute("Content")?.Value ?? string.Empty)
                ? element.Attribute("Content")?.Value
                : null;
            return dataContext =>
            {
                var checkBox = new Microsoft.UI.Xaml.Controls.CheckBox();
                ApplySimplePropertyValues(checkBox, values);
                if (contentBinding != null)
                {
                    BindingAssignment.To(nameof(Microsoft.UI.Xaml.Controls.CheckBox.Content), contentBinding)
                        .Apply(checkBox, dataContext);
                }
                else if (content != null)
                {
                    checkBox.Content = content;
                }

                if (checkedBinding != null)
                {
                    BindingAssignment.To(nameof(Microsoft.UI.Xaml.Controls.CheckBox.IsChecked), checkedBinding)
                        .Apply(checkBox, dataContext);
                }

                return checkBox;
            };
        }

        return null;
    }

    private static object? GetResourceKey(XElement element, Func<string, Type?> typeResolver)
    {
        var explicitKey = element.Attribute(XamlNamespace + "Key")?.Value;
        if (!string.IsNullOrWhiteSpace(explicitKey))
        {
            return explicitKey;
        }

        if (element.Name == PresentationNamespace + "DataTemplate")
        {
            return ResolveXamlType(element.Attribute("DataType")?.Value, typeResolver);
        }

        return null;
    }

    private static string FormatReportKey(object key)
        => key is Type type ? type.FullName ?? type.Name : key.ToString() ?? string.Empty;

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

        var binding = ParseBinding(text);
        if (binding != null)
        {
            return binding;
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

        if (Enum.TryParse<Microsoft.UI.Xaml.Controls.Orientation>(text, out var orientation))
        {
            return orientation;
        }

        return text;
    }

    private static SolidColorBrushSpec? ReadSolidColorBrush(XElement brushElement)
    {
        var colorText = brushElement.Attribute("Color")?.Value;
        if (string.IsNullOrWhiteSpace(colorText))
        {
            return null;
        }

        if (System.Windows.Media.ColorConverter.ConvertFromString(colorText) is not global::Windows.UI.Color color)
        {
            return null;
        }

        double? opacity = null;
        if (double.TryParse(brushElement.Attribute("Opacity")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedOpacity))
        {
            opacity = parsedOpacity;
        }

        return new SolidColorBrushSpec(color, opacity);
    }

    private static Microsoft.UI.Xaml.Media.SolidColorBrush CreateSolidColorBrush(SolidColorBrushSpec spec)
    {
        var brush = new Microsoft.UI.Xaml.Media.SolidColorBrush(spec.Color);
        if (spec.Opacity.HasValue)
        {
            brush.Opacity = spec.Opacity.Value;
        }

        return brush;
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

    private static Dictionary<string, WpfResourceReference> ReadResourcePropertyValues(params XElement[] elements)
    {
        var values = new Dictionary<string, WpfResourceReference>(StringComparer.Ordinal);
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

                if (ParseResourceReference(attribute.Value) is { } reference)
                {
                    values[propertyName] = reference;
                }
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

    private static void ApplyResourcePropertyValues(
        object target,
        IReadOnlyDictionary<string, WpfResourceReference> values,
        System.Windows.ResourceDictionary? resources)
    {
        if (resources == null || values.Count == 0)
        {
            return;
        }

        var targetType = target.GetType();
        foreach (var (propertyName, reference) in values)
        {
            if (!resources.ContainsKey(reference.Key))
            {
                continue;
            }

            var property = targetType.GetProperty(propertyName);
            if (property == null || !property.CanWrite)
            {
                continue;
            }

            var value = resources[reference.Key];
            if (CanAssignValue(value, property.PropertyType))
            {
                property.SetValue(target, value);
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

public sealed record SolidColorBrushSpec(global::Windows.UI.Color Color, double? Opacity);

public sealed record AlternationConverterSpec(IReadOnlyList<SolidColorBrushSpec> Values);

public sealed record MenuItemSpec(
    object? Header,
    object? Command,
    object? CommandParameter,
    string? InputGestureText);

public sealed record ContextMenuSpec(IReadOnlyList<MenuItemSpec> Items) : IWpfDeferredSetterValue
{
    public object CreateValue()
    {
        var contextMenu = new ContextMenu();
        foreach (var item in Items)
        {
            contextMenu.Items.Add(new MenuItem
            {
                Header = item.Header,
                Command = item.Command,
                CommandParameter = item.CommandParameter,
                InputGestureText = item.InputGestureText,
            });
        }

        return contextMenu;
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
