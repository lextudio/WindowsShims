namespace System.Windows.Controls;

/// <summary>
/// WPF-compatible ContentPresenter base. Extends WinUI ContentPresenter with the
/// small WPF surface the linked upstream DataGridDetailsPresenter depends on:
/// routed-event plumbing, a DependencyPropertyKey write path, a no-op
/// DefaultStyleKeyProperty (WinUI ContentPresenter derives from FrameworkElement,
/// which has none), and an OnVisualParentChanged virtual the upstream overrides.
/// </summary>
public class ContentPresenter : Microsoft.UI.Xaml.Controls.ContentPresenter
{
    // WinUI ContentPresenter has no DefaultStyleKey (it is a FrameworkElement, not
    // a Control). Provide a dummy DP so upstream OverrideMetadata calls resolve;
    // there is no WPF default template under Uno, so it is otherwise inert.
    public static readonly DependencyProperty DefaultStyleKeyProperty =
        DependencyProperty.Register("DefaultStyleKey_CP", typeof(object), typeof(ContentPresenter),
            new PropertyMetadata(null));

    // WPF UIElement routed event accessed unqualified by the upstream static ctor.
    // EventManager.RegisterClassHandler is a no-op, so this only needs to exist.
    public static readonly RoutedEvent MouseLeftButtonDownEvent = new();

    // ── Routed event infrastructure (mirrors the ContentControl shim) ──────────
    public void RaiseEvent(RoutedEventArgs e) => WinUIDependencyObjectExtensions.RaiseEvent(this, e);

    protected void AddHandler(System.Windows.RoutedEvent routedEvent, Delegate handler) =>
        WinUIDependencyObjectExtensions.AddHandler(this, routedEvent, handler);

    protected void RemoveHandler(System.Windows.RoutedEvent routedEvent, Delegate handler) =>
        WinUIDependencyObjectExtensions.RemoveHandler(this, routedEvent, handler);

    // WPF DependencyObject.SetValue accepting a DependencyPropertyKey.
    public void SetValue(System.Windows.DependencyPropertyKey key, object? value)
        => SetValue(key.DependencyProperty, value);

    // ── WPF no-ops the ported code calls ───────────────────────────────────────
    protected void CoerceValue(DependencyProperty dp) { }
    protected void AddLogicalChild(object child) { }
    protected void RemoveLogicalChild(object child) { }

    // WPF Visual.OnVisualParentChanged — WinUI has no such virtual, so the upstream
    // override binds here. It is never invoked under Uno (the owning DataGridRow
    // drives SyncProperties explicitly), so an empty default is correct.
    protected internal virtual void OnVisualParentChanged(DependencyObject oldParent) { }
}
