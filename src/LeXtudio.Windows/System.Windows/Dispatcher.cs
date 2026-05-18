namespace System.Windows;

/// <summary>
/// Extension methods on WinUI's CoreDispatcher so WPF code that calls
/// dependencyObject.Dispatcher.BeginInvoke(...) still compiles.
/// (WinUI's DependencyObject.Dispatcher returns CoreDispatcher, not our Dispatcher shim.)
/// </summary>
public static class CoreDispatcherExtensions
{
    public static DispatcherProcessingDisabled DisableProcessing(
        this global::Windows.UI.Core.CoreDispatcher dispatcher) => default;

    public static void BeginInvoke(
        this global::Windows.UI.Core.CoreDispatcher dispatcher,
        DispatcherPriority priority,
        Delegate method)
        => method.DynamicInvoke();

    public static void BeginInvoke(
        this global::Windows.UI.Core.CoreDispatcher dispatcher,
        DispatcherPriority priority,
        System.Threading.SendOrPostCallback callback,
        object? arg)
        => callback(arg);
}



public sealed class Dispatcher
{
    public static Dispatcher CurrentDispatcher { get; } = new Dispatcher();

    public static implicit operator Dispatcher(global::Windows.UI.Core.CoreDispatcher? _)
        => CurrentDispatcher;

    public DispatcherProcessingDisabled DisableProcessing() => default;

    public void BeginInvoke(DispatcherPriority priority, Delegate method) =>
        method.DynamicInvoke();

    public void BeginInvoke(DispatcherPriority priority, Delegate method, object? arg) =>
        method.DynamicInvoke(arg);

    public void BeginInvoke(DispatcherPriority priority, System.Threading.SendOrPostCallback callback, object? arg) =>
        callback(arg);
}

public struct DispatcherProcessingDisabled : IDisposable
{
    public void Dispose() { }
}

public enum DispatcherPriority
{
    Background,
    Loaded,
    Normal,
    Send
}
