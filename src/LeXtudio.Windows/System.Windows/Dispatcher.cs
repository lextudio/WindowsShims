using System.Windows.Threading;

namespace System.Windows;

/// <summary>
/// Marshals WPF-shim dispatcher work onto the real Uno/WinUI UI thread.
/// <para/>
/// Uno's <c>UIElement.Dispatcher</c> returns a <see cref="global::Windows.UI.Core.CoreDispatcher"/>,
/// so upstream WPF code like <c>this.Dispatcher.BeginInvoke(continuation)</c> binds to the
/// <see cref="CoreDispatcherExtensions"/> below. Those used to call <c>DynamicInvoke()</c> inline,
/// which runs the callback on whatever thread the task continuation completed on — off the UI
/// thread — tripping Uno's "dependency property system should not be accessed from non UI thread".
/// This helper instead enqueues onto the captured UI <see cref="global::Microsoft.UI.Dispatching.DispatcherQueue"/>.
/// </summary>
public static class ShimUiDispatcher
{
    private static global::Microsoft.UI.Dispatching.DispatcherQueue? _queue;

    /// <summary>
    /// Explicitly latch the UI DispatcherQueue. Call once from the host on the UI thread
    /// (e.g. App startup) for deterministic behaviour; otherwise it is captured lazily the
    /// first time a shim dispatcher call runs on the UI thread.
    /// </summary>
    public static void Capture(global::Microsoft.UI.Dispatching.DispatcherQueue queue) => _queue = queue;

    private static global::Microsoft.UI.Dispatching.DispatcherQueue? Resolve()
        => _queue ??= global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

    /// <summary>Fire-and-forget: run on the UI thread (inline if already there).</summary>
    public static void Post(Delegate method, object?[]? args = null)
    {
        var queue = Resolve();
        if (queue is null || queue.HasThreadAccess)
        {
            method.DynamicInvoke(args);
            return;
        }

        queue.TryEnqueue(() => method.DynamicInvoke(args));
    }

    /// <summary>Synchronous: run on the UI thread and block until it completes.</summary>
    public static void Send(Action callback)
    {
        var queue = Resolve();
        if (queue is null || queue.HasThreadAccess)
        {
            callback();
            return;
        }

        using var done = new System.Threading.ManualResetEventSlim();
        Exception? error = null;
        queue.TryEnqueue(() =>
        {
            try { callback(); }
            catch (Exception ex) { error = ex; }
            finally { done.Set(); }
        });
        done.Wait();
        if (error is not null)
            throw error;
    }
}

/// <summary>
/// Extension methods on WinUI's CoreDispatcher so WPF code that calls
/// dependencyObject.Dispatcher.BeginInvoke(...) still compiles.
/// (WinUI's DependencyObject.Dispatcher returns CoreDispatcher, not our Dispatcher shim.)
/// </summary>
public static class CoreDispatcherExtensions
{
    public static DispatcherProcessingDisabled DisableProcessing(
        this global::Windows.UI.Core.CoreDispatcher dispatcher) => default;

    public static bool CheckAccess(this global::Windows.UI.Core.CoreDispatcher dispatcher)
        => global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() is not null;

    public static void VerifyAccess(this global::Windows.UI.Core.CoreDispatcher dispatcher)
    {
    }

    public static void BeginInvoke(
        this global::Windows.UI.Core.CoreDispatcher dispatcher,
        DispatcherPriority priority,
        Delegate method)
        => ShimUiDispatcher.Post(method);

    public static void BeginInvoke(
        this global::Windows.UI.Core.CoreDispatcher dispatcher,
        DispatcherPriority priority,
        System.Threading.SendOrPostCallback callback,
        object? arg)
        => ShimUiDispatcher.Post(callback, new[] { arg });

    public static void BeginInvoke(
        this global::Windows.UI.Core.CoreDispatcher dispatcher,
        DispatcherPriority priority,
        System.Windows.Threading.DispatcherOperationCallback callback,
        object? arg)
        => ShimUiDispatcher.Post(callback, new[] { arg });
}



public sealed class Dispatcher
{
    public static Dispatcher CurrentDispatcher { get; } = new Dispatcher();

    public static implicit operator Dispatcher(global::Windows.UI.Core.CoreDispatcher? _)
        => CurrentDispatcher;

    public DispatcherProcessingDisabled DisableProcessing() => default;

    public bool CheckAccess()
        => global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() is not null;

    public void VerifyAccess()
    {
    }

    public void BeginInvoke(DispatcherPriority priority, Delegate method) =>
        ShimUiDispatcher.Post(method);

    public void BeginInvoke(DispatcherPriority priority, Delegate method, object? arg) =>
        ShimUiDispatcher.Post(method, new[] { arg });

    public void BeginInvoke(DispatcherPriority priority, System.Threading.SendOrPostCallback callback, object? arg) =>
        ShimUiDispatcher.Post(callback, new[] { arg });

    public void BeginInvoke(DispatcherPriority priority, System.Windows.Threading.DispatcherOperationCallback callback, object? arg) =>
        ShimUiDispatcher.Post(callback, new[] { arg });

    public event EventHandler ShutdownFinished;
    public event EventHandler ShutdownStarted;
}

public struct DispatcherProcessingDisabled : IDisposable
{
    public void Dispose() { }
}
