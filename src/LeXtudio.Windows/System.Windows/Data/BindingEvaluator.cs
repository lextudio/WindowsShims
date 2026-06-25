using System.Globalization;
using System.Reflection;

namespace System.Windows.Data;

public static class BindingEvaluator
{
    private static readonly object UnsetValue = new();

    public static void Apply(object target, string propertyName, object? dataContext, Binding binding)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        ArgumentNullException.ThrowIfNull(binding);

        var property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public);
        if (property is null || !property.CanWrite)
        {
            throw new InvalidOperationException(
                $"Writable property '{propertyName}' was not found on '{target.GetType().FullName}'.");
        }

        var value = Evaluate(dataContext, binding, property.PropertyType);
        property.SetValue(target, CoerceValue(value, property.PropertyType));
    }

    public static object? Evaluate(object? dataContext, Binding binding, Type? targetType = null)
    {
        ArgumentNullException.ThrowIfNull(binding);

        var source = binding.Source ?? dataContext;
        var value = EvaluatePath(source, binding.Path?.Path);
        if (ReferenceEquals(value, UnsetValue))
        {
            return binding.FallbackValue;
        }

        if (value is null && binding.TargetNullValue is not null)
        {
            value = binding.TargetNullValue;
        }

        if (binding.Converter is not null)
        {
            value = binding.Converter.Convert(
                value,
                targetType ?? typeof(object),
                binding.ConverterParameter,
                binding.ConverterCulture ?? CultureInfo.CurrentCulture);
            if (ReferenceEquals(value, Binding.DoNothing))
            {
                return binding.FallbackValue;
            }
        }

        if (!string.IsNullOrEmpty(binding.StringFormat))
        {
            value = string.Format(
                binding.ConverterCulture ?? CultureInfo.CurrentCulture,
                binding.StringFormat,
                value);
        }

        return value;
    }

    public static T? Evaluate<T>(object? dataContext, Binding binding)
    {
        var value = Evaluate(dataContext, binding, typeof(T));
        if (value is null)
        {
            return default;
        }

        if (value is T typed)
        {
            return typed;
        }

        return (T?)Convert.ChangeType(value, typeof(T), CultureInfo.CurrentCulture);
    }

    private static object? CoerceValue(object? value, Type targetType)
    {
        if (value is null)
        {
            return null;
        }

        var nullableType = Nullable.GetUnderlyingType(targetType);
        var effectiveType = nullableType ?? targetType;

        if (effectiveType.IsInstanceOfType(value))
        {
            return value;
        }

        if (effectiveType.IsEnum)
        {
            return value is string text
                ? Enum.Parse(effectiveType, text)
                : Enum.ToObject(effectiveType, value);
        }

        return Convert.ChangeType(value, effectiveType, CultureInfo.CurrentCulture);
    }

    private static object? EvaluatePath(object? source, string? path)
    {
        if (string.IsNullOrEmpty(path) || path == ".")
        {
            return source;
        }

        var value = source;
        foreach (var segment in path.Split('.'))
        {
            if (string.IsNullOrEmpty(segment))
            {
                continue;
            }

            if (value is null)
            {
                return null;
            }

            var property = value.GetType().GetProperty(
                segment,
                BindingFlags.Instance | BindingFlags.Public);
            if (property is null)
            {
                return UnsetValue;
            }

            value = property.GetValue(value);
        }

        return value;
    }
}
