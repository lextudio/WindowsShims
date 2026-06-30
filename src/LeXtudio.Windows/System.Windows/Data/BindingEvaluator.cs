using System.Globalization;
using System.Linq;
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

        var property = ResolveProperty(target.GetType(), propertyName);
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

            var property = ResolveProperty(value.GetType(), segment);
            if (property is null)
            {
                return UnsetValue;
            }

            value = property.GetValue(value);
        }

        return value;
    }

    private static PropertyInfo? ResolveProperty(Type type, string propertyName)
    {
        try
        {
            return type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        }
        catch (AmbiguousMatchException)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.Name == propertyName)
                .OrderByDescending(p => p.DeclaringType?.Namespace?.StartsWith("System.Windows", StringComparison.Ordinal) == true)
                .ThenBy(p => InheritanceDistance(type, p.DeclaringType))
                .FirstOrDefault();
        }
    }

    private static int InheritanceDistance(Type type, Type? declaringType)
    {
        if (declaringType is null)
        {
            return int.MaxValue;
        }

        var distance = 0;
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current == declaringType)
            {
                return distance;
            }

            distance++;
        }

        return int.MaxValue;
    }
}
