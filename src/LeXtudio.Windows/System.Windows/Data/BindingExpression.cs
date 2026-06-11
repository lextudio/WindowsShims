using System.Reflection;

namespace System.Windows.Data;

public abstract class BindingExpressionBase
{
    // WPF uses a NamedObject sentinel that marks items disconnected from the
    // visual tree. Only reference identity matters to callers.
    internal static readonly object DisconnectedItem = new();
}

// Untargeted binding-expression bridge for the selector spine's
// SelectedValue paths. WPF evaluates through the full property engine and
// supports XML, indexers, and attached properties; this bridge walks plain
// dotted CLR property paths against the activated item.
public class BindingExpression : BindingExpressionBase
{
    private object? _item;
    private bool _isActive;

    private BindingExpression(Binding binding)
    {
        ParentBinding = binding;
    }

    public Binding ParentBinding { get; }

    public object? Value
    {
        get
        {
            if (!_isActive)
            {
                return DependencyProperty.UnsetValue;
            }

            return EvaluatePath(_item, ParentBinding.Path?.Path);
        }
    }

    internal static BindingExpressionBase CreateUntargetedBindingExpression(
        DependencyObject target,
        Binding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);

        return new BindingExpression(binding);
    }

    internal void Activate(object? item)
    {
        _item = item;
        _isActive = true;
    }

    internal void Deactivate()
    {
        _item = null;
        _isActive = false;
    }

    private static object? EvaluatePath(object? item, string? path)
    {
        if (item is null)
        {
            return DependencyProperty.UnsetValue;
        }

        if (string.IsNullOrEmpty(path))
        {
            return item;
        }

        var current = item;
        foreach (var member in path.Split('.'))
        {
            if (current is null)
            {
                return DependencyProperty.UnsetValue;
            }

            var property = current.GetType().GetProperty(
                member,
                BindingFlags.Instance | BindingFlags.Public);
            if (property is null)
            {
                return DependencyProperty.UnsetValue;
            }

            current = property.GetValue(current);
        }

        return current;
    }
}
