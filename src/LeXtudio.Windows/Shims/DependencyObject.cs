namespace System.Windows;

public class DependencyObject
{
    private readonly System.Collections.Generic.Dictionary<DependencyProperty, object?> _values = new();
    private readonly System.Collections.Generic.Dictionary<System.Windows.RoutedEvent, System.Collections.Generic.List<System.Delegate>> _handlers = new();

    public object? GetValue(DependencyProperty property)
    {
        return _values.TryGetValue(property, out var value) ? value : property.DefaultValue;
    }

    public void SetValue(DependencyProperty property, object? value)
    {
        var oldValue = GetValue(property);
        if (property.Metadata.CoerceValueCallback is not null)
        {
            value = property.Metadata.CoerceValueCallback(this, value!);
        }

        _values[property] = value;
        property.Metadata.PropertyChangedCallback?.Invoke(
            this,
            new DependencyPropertyChangedEventArgs
            {
                OldValue = oldValue,
                NewValue = value,
                NewEntry = new Entry { IsDeferredReference = false }
            });
    }

    protected void SetCurrentDeferredValue(DependencyProperty property, object? value)
    {
        _values[property] = value;
    }

    protected object LookupEntry(int globalIndex) => new();

    protected bool HasExpression(object entry, DependencyProperty property) => false;

    public void CoerceValue(DependencyProperty property)
    {
        SetValue(property, GetValue(property));
    }

    public void AddHandler(System.Windows.RoutedEvent routedEvent, System.Delegate handler)
    {
        if (!_handlers.TryGetValue(routedEvent, out var handlers))
        {
            handlers = [];
            _handlers[routedEvent] = handlers;
        }

        handlers.Add(handler);
    }

    public void RemoveHandler(System.Windows.RoutedEvent routedEvent, System.Delegate handler)
    {
        if (_handlers.TryGetValue(routedEvent, out var handlers))
        {
            handlers.Remove(handler);
        }
    }

    public void RaiseEvent(System.Windows.RoutedEventArgs args)
    {
        if (args.RoutedEvent is null)
        {
            return;
        }

        if (_handlers.TryGetValue(args.RoutedEvent, out var handlers))
        {
            foreach (var handler in handlers.ToArray())
            {
                handler.DynamicInvoke(this, args);
            }
        }
    }

    public virtual bool Focus() => true;

    public virtual void VerifyAccess()
    {
    }

    public Dispatcher Dispatcher { get; } = new();

    public bool IsMouseCaptured { get; private set; }

    public bool IsMouseOver { get; set; }

    public void CaptureMouse()
    {
        IsMouseCaptured = true;
    }

    public void ReleaseMouseCapture()
    {
        IsMouseCaptured = false;
    }
}

public sealed class Dispatcher
{
    public void BeginInvoke(DispatcherPriority priority, System.Threading.SendOrPostCallback callback, object? arg)
    {
        callback(arg);
    }
}

public enum DispatcherPriority
{
    Send
}
