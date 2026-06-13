using System.ComponentModel;

namespace System.Windows.Threading;

// DispatcherOperationCallback is defined in DispatcherPriority.cs

public sealed class DispatcherOperation
{
    internal DispatcherOperation(Task task) => Task = task;
    public Task Task { get; }
}

public sealed class DispatcherOperation<T>
{
    internal DispatcherOperation(Task<T> task) => Task = task;
    public Task<T> Task { get; }
}

public sealed class Dispatcher
{
    private static readonly Lazy<Dispatcher> _current =
        new(() => new Dispatcher(SynchronizationContext.Current), isThreadSafe: false);

    private readonly SynchronizationContext _context;
    private readonly int _threadId;

    public Dispatcher(SynchronizationContext? context = null)
    {
        _context = context ?? new SynchronizationContext();
        _threadId = Environment.CurrentManagedThreadId;
        Thread = Thread.CurrentThread;
    }

    public static Dispatcher CurrentDispatcher => _current.Value;

    public Thread Thread { get; }

    public bool CheckAccess() => Environment.CurrentManagedThreadId == _threadId;

    public void VerifyAccess()
    {
        if (!CheckAccess())
            throw new InvalidOperationException("The calling thread cannot access this dispatcher.");
    }

    // ── Invoke (synchronous) ──────────────────────────────────────────────

    public void Invoke(Action callback) => Invoke(callback, DispatcherPriority.Send);

    public void Invoke(Action callback, DispatcherPriority priority)
    {
        ArgumentNullException.ThrowIfNull(callback);
        if (CheckAccess()) { callback(); return; }

        using var done = new ManualResetEventSlim();
        Exception? ex = null;
        _context.Post(_ =>
        {
            try { callback(); }
            catch (Exception e) { ex = e; }
            finally { done.Set(); }
        }, null);
        done.Wait();
        if (ex is not null) throw ex;
    }

    public void Invoke(Action callback, DispatcherPriority priority, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Invoke(callback, priority);
    }

    public T Invoke<T>(Func<T> callback) => Invoke(callback, DispatcherPriority.Send);

    public T Invoke<T>(Func<T> callback, DispatcherPriority priority)
    {
        ArgumentNullException.ThrowIfNull(callback);
        if (CheckAccess()) return callback();

        using var done = new ManualResetEventSlim();
        Exception? ex = null;
        T? result = default;
        _context.Post(_ =>
        {
            try { result = callback(); }
            catch (Exception e) { ex = e; }
            finally { done.Set(); }
        }, null);
        done.Wait();
        if (ex is not null) throw ex;
        return result!;
    }

    public T Invoke<T>(Func<T> callback, DispatcherPriority priority, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Invoke(callback, priority);
    }

    public object? Invoke(Delegate method, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(method);
        return Invoke(() => method.DynamicInvoke(args), DispatcherPriority.Send);
    }

    // ── InvokeAsync (fire and collect) ────────────────────────────────────

    public DispatcherOperation InvokeAsync(Action callback)
        => InvokeAsync(callback, DispatcherPriority.Normal);

    public DispatcherOperation InvokeAsync(Action callback, DispatcherPriority priority)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _context.Post(_ =>
        {
            try { callback(); tcs.SetResult(null); }
            catch (Exception e) { tcs.SetException(e); }
        }, null);
        return new DispatcherOperation(tcs.Task);
    }

    public DispatcherOperation InvokeAsync(Action callback, DispatcherPriority priority, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            return new DispatcherOperation(Task.FromCanceled(ct));
        return InvokeAsync(callback, priority);
    }

    public DispatcherOperation<T> InvokeAsync<T>(Func<T> callback)
        => InvokeAsync(callback, DispatcherPriority.Normal);

    public DispatcherOperation<T> InvokeAsync<T>(Func<T> callback, DispatcherPriority priority)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _context.Post(_ =>
        {
            try { tcs.SetResult(callback()); }
            catch (Exception e) { tcs.SetException(e); }
        }, null);
        return new DispatcherOperation<T>(tcs.Task);
    }

    public DispatcherOperation<T> InvokeAsync<T>(Func<T> callback, DispatcherPriority priority, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            return new DispatcherOperation<T>(Task.FromCanceled<T>(ct));
        return InvokeAsync(callback, priority);
    }

    // ── BeginInvoke (fire and forget) ─────────────────────────────────────

    public DispatcherOperation BeginInvoke(Action callback)
        => InvokeAsync(callback, DispatcherPriority.Normal);

    public DispatcherOperation BeginInvoke(Action callback, DispatcherPriority priority)
        => InvokeAsync(callback, priority);

    public DispatcherOperation BeginInvoke(Delegate method, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(method);
        var op = InvokeAsync<object?>(() => method.DynamicInvoke(args), DispatcherPriority.Normal);
        return new DispatcherOperation(op.Task);
    }

    // WPF-style BeginInvoke(DispatcherOperationCallback, DispatcherPriority, object) overload (delegate first).
    public DispatcherOperation BeginInvoke(DispatcherOperationCallback callback, DispatcherPriority priority, object? arg)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var op = InvokeAsync<object?>(() => callback(arg), priority);
        return new DispatcherOperation(op.Task);
    }

    // WPF-style BeginInvoke(DispatcherPriority, Delegate, object) overload (priority first).
    public DispatcherOperation BeginInvoke(DispatcherPriority priority, Delegate method, object? arg)
    {
        ArgumentNullException.ThrowIfNull(method);
        var op = InvokeAsync<object?>(() => method.DynamicInvoke(arg), priority);
        return new DispatcherOperation(op.Task);
    }
}
