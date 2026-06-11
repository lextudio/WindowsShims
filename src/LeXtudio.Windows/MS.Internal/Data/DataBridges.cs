using System.ComponentModel;
using System.Windows.Data;

namespace MS.Internal.Data;

internal class BindingExpressionUncommonField : UncommonField<BindingExpression>
{
}

// Bridge subset of WPF's DynamicValueConverter: convert a value to a target
// type using component-model type converters, falling back to IConvertible.
// Returns DependencyProperty.UnsetValue when no conversion applies, matching
// the WPF contract that callers test against.
internal class DynamicValueConverter
{
    public DynamicValueConverter(bool targetToSourceNeededFlag)
    {
    }

    public object? Convert(object? value, Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);

        if (value is null)
        {
            return targetType.IsValueType ? DependencyProperty.UnsetValue : null;
        }

        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        try
        {
            var targetConverter = TypeDescriptor.GetConverter(targetType);
            if (targetConverter.CanConvertFrom(value.GetType()))
            {
                return targetConverter.ConvertFrom(value);
            }

            var sourceConverter = TypeDescriptor.GetConverter(value.GetType());
            if (sourceConverter.CanConvertTo(targetType))
            {
                return sourceConverter.ConvertTo(value, targetType);
            }

            if (value is IConvertible)
            {
                return System.Convert.ChangeType(value, targetType);
            }
        }
        catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException or NotSupportedException or ArgumentException)
        {
        }

        return DependencyProperty.UnsetValue;
    }
}
