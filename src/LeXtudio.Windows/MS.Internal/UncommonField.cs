using System.Runtime.CompilerServices;

namespace MS.Internal;

// Bridge for WPF's UncommonField<T>. WPF stores these in the dependency
// object's effective-value table through internal entry indices; that storage
// is not reachable on WinUI, so this bridge keeps per-instance values in a
// ConditionalWeakTable keyed by the owning DependencyObject.
internal class UncommonField<T>
{
    private readonly ConditionalWeakTable<DependencyObject, object?> _storage = new();
    private readonly T? _defaultValue;

    public UncommonField()
        : this(default!)
    {
    }

    public UncommonField(T? defaultValue)
    {
        _defaultValue = defaultValue;
    }

    public void SetValue(DependencyObject instance, T? value)
    {
        ArgumentNullException.ThrowIfNull(instance);

        _storage.Remove(instance);
        _storage.Add(instance, value);
    }

    public T? GetValue(DependencyObject instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        return _storage.TryGetValue(instance, out var value) ? (T?)value : _defaultValue;
    }

    public void ClearValue(DependencyObject instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        _storage.Remove(instance);
    }
}
