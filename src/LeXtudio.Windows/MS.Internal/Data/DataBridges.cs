using System.ComponentModel;
using System.Windows.Data;

namespace MS.Internal.Data;

// Stable "unset" sentinel for the binding/converter shims.
//
// On the WinUI (WINDOWS_APP_SDK) target Microsoft.UI.Xaml.DependencyProperty.UnsetValue is a
// WinRT-projected sentinel that returns a DIFFERENT wrapper on each access, so two separate reads are
// not Equal (`a == DependencyProperty.UnsetValue` / NUnit Is.EqualTo fail across accesses). Capture it
// ONCE here and have the shims (and their tests) share this single reference. On the Uno desktop target
// UnsetValue is already a stable managed object, so this is just a harmless one-time capture and the
// value stays identical to DependencyProperty.UnsetValue.
internal static class BindingValue
{
    internal static readonly object UnsetValue = Microsoft.UI.Xaml.DependencyProperty.UnsetValue;
}

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
            return targetType.IsValueType ? BindingValue.UnsetValue : null;
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

        return BindingValue.UnsetValue;
    }
}
