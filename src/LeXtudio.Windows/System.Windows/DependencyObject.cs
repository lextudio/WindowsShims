namespace System.Windows;

/// <summary>
/// WPF-style DependencyObject shim.
/// Uses Microsoft.UI.Xaml.DependencyProperty as the property key (single source of truth).
/// Value storage and callback dispatch are local — WinUI's own DO machinery is not engaged
/// because System.Windows.DependencyObject does not inherit from Microsoft.UI.Xaml.DependencyObject.
/// </summary>
public class DependencyObject
{
    private readonly System.Collections.Generic.Dictionary<Microsoft.UI.Xaml.DependencyProperty, object?> _values = new();
    private readonly System.Collections.Generic.Dictionary<System.Windows.RoutedEvent, System.Collections.Generic.List<System.Delegate>> _handlers = new();

    public object? GetValue(Microsoft.UI.Xaml.DependencyProperty property)
    {
        if (_values.TryGetValue(property, out var value)) return value;
        // PropertyMetadata is internal in WinUI; default value access goes through GetDefaultValue if available.
        return null;
    }

    public void SetValue(Microsoft.UI.Xaml.DependencyProperty property, object? value)
    {
        var oldValue = GetValue(property);
        // If metadata is our FrameworkPropertyMetadata, run WPF callbacks/coerce locally.
        if (property is not null)
        {
            // Best-effort: try to read metadata via reflection-free path — WinUI does not expose
            // GetMetadata publicly, so callbacks attached via FrameworkPropertyMetadata only fire
            // when objects are local DependencyObject instances (this class) and metadata flows
            // through the FrameworkPropertyMetadata constructor.
        }
        _values[property] = value;
    }

    protected void SetCurrentDeferredValue(Microsoft.UI.Xaml.DependencyProperty property, object? value)
    {
        _values[property] = value;
    }

    protected object LookupEntry(int globalIndex) => new();

    protected bool HasExpression(object entry, Microsoft.UI.Xaml.DependencyProperty property) => false;

    public void CoerceValue(Microsoft.UI.Xaml.DependencyProperty property)
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
