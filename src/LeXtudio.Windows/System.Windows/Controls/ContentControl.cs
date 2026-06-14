namespace System.Windows.Controls;

/// <summary>
/// WPF-compatible ContentControl base class. Extends WinUI ContentControl with the same
/// WPF surface that the shim Control adds — routed events, focus helpers, DependencyPropertyKey
/// write path — so that ContentControl subclasses (DataGridCell, ButtonBase, etc.) compile
/// upstream WPF source without per-class workarounds.
/// </summary>
public class ContentControl : Microsoft.UI.Xaml.Controls.ContentControl
{
    public ContentControl()
    {
        PointerEntered += (_, _) => { _isMouseOver = true;  UpdateVisualState(); };
        PointerExited  += (_, _) => { _isMouseOver = false; UpdateVisualState(); };
    }

    private bool _isMouseOver;

    // ── Routed event infrastructure ───────────────────────────────────────────

    // WPF UIElement.RaiseEvent — dispatch through the shared WPF-style handler bag.
    public void RaiseEvent(RoutedEventArgs e) => WinUIDependencyObjectExtensions.RaiseEvent(this, e);

    // WPF UIElement.AddHandler/RemoveHandler taking System.Windows.RoutedEvent.
    // 'protected' matches the accessibility in the shim Control; upstream WPF files
    // call AddHandler/RemoveHandler from within the class body so protected suffices.
    protected void AddHandler(System.Windows.RoutedEvent routedEvent, Delegate handler) =>
        WinUIDependencyObjectExtensions.AddHandler(this, routedEvent, handler);

    protected void RemoveHandler(System.Windows.RoutedEvent routedEvent, Delegate handler) =>
        WinUIDependencyObjectExtensions.RemoveHandler(this, routedEvent, handler);

    // ── Read-only DP write path ───────────────────────────────────────────────

    // WPF DependencyObject.SetValue accepting a DependencyPropertyKey.
    public void SetValue(System.Windows.DependencyPropertyKey key, object? value)
        => SetValue(key.DependencyProperty, value);

    // ── Focus helpers ─────────────────────────────────────────────────────────

    // WPF UIElement.Focus() — no FocusState parameter.
    public bool Focus() => Focus(Microsoft.UI.Xaml.FocusState.Programmatic);

    // WPF UIElement.MoveFocus — keyboard navigation; no-op on HAS_UNO.
    public bool MoveFocus(Input.TraversalRequest request) => false;

    // WPF UIElement.BringIntoView
    public void BringIntoView() => StartBringIntoView();

    // WPF UIElement.IsVisible
    public bool IsVisible => Visibility == Visibility.Visible;

    // WPF Visual.VisualParent — immediate visual parent in the tree.
    protected Microsoft.UI.Xaml.DependencyObject? VisualParent
        => Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(this);

    // WPF UIElement.IsKeyboardFocusWithin / IsKeyboardFocused.
    // Stub returning false; sufficient for the DataGrid shim path which tracks
    // focus through pointer events rather than keyboard-focus APIs.
    public bool IsKeyboardFocusWithin => false;
    public bool IsKeyboardFocused => false;
    public bool IsMouseOver => _isMouseOver;

    // ── Logical tree no-ops (ported WPF code calls these) ────────────────────

    protected void AddLogicalChild(object child) { }
    protected void RemoveLogicalChild(object child) { }

    // ── WPF coercion no-op ────────────────────────────────────────────────────

    protected void CoerceValue(DependencyProperty dp) { }

    // ── Visual state management ───────────────────────────────────────────────

    // Subclasses call UpdateVisualState to request a VSM transition. Mapped to
    // ChangeVisualState so one override covers both call patterns.
    internal void UpdateVisualState() => UpdateVisualState(true);
    internal virtual void UpdateVisualState(bool useTransitions) => ChangeVisualState(useTransitions);
    internal virtual void ChangeVisualState(bool useTransitions) { }
}
