using System.Collections;
using System.Windows.Documents;
using System.Windows.Input;

namespace System.Windows.Controls;

/// <summary>
/// WPF-compatible Control base class. Inherits from WinUI Control and adds the WPF virtual
/// event-override pattern (On*) that WPF source files like TextBoxBase override.
/// On HAS_UNO these are no-op stubs; actual input wiring is done by Uno's event model.
/// </summary>
public abstract class Control : Microsoft.UI.Xaml.Controls.Control
{
    protected Control()
    {
        InitializeDefaultStyleKey();
    }

    // Subclasses override this to set DefaultStyleKey = typeof(TheirType) before the
    // first Measure pass. Required because WPF's OverrideMetadata is a no-op on Uno.
    protected virtual void InitializeDefaultStyleKey() { }

    // WPF makes OnApplyTemplate() public; WinUI declares it protected.
    // Must use 'protected override' (not 'new virtual') so Uno's layout system
    // dispatches through the correct vtable slot when applying templates.
    // WPF subclasses widen accessibility with 'public override', which is legal in C#.
    protected override void OnApplyTemplate() => base.OnApplyTemplate();

    // ── WPF-only overrideable event methods ──────────────────────────────────

    internal virtual void ChangeVisualState(bool useTransitions) { }

    protected virtual void OnTemplateChanged(ControlTemplate oldTemplate, ControlTemplate newTemplate) { }

    protected virtual void OnMouseWheel(MouseWheelEventArgs e) { }

    protected virtual void OnPreviewKeyDown(KeyEventArgs e) { }

    protected virtual void OnKeyDown(KeyEventArgs e) { }

    protected virtual void OnKeyUp(KeyEventArgs e) { }

    protected virtual void OnTextInput(TextCompositionEventArgs e) { }

    protected virtual void OnMouseDown(MouseButtonEventArgs e) { }

    protected virtual void OnMouseMove(System.Windows.Input.MouseEventArgs e) { }

    protected virtual void OnMouseUp(MouseButtonEventArgs e) { }

    protected virtual void OnQueryCursor(QueryCursorEventArgs e) { }

    protected virtual void OnQueryContinueDrag(QueryContinueDragEventArgs e) { }

    protected virtual void OnGiveFeedback(GiveFeedbackEventArgs e) { }

    protected virtual void OnDragEnter(DragEventArgs e) { }

    protected virtual void OnDragOver(DragEventArgs e) { }

    protected virtual void OnDragLeave(DragEventArgs e) { }

    protected virtual void OnDrop(DragEventArgs e) { }

    protected virtual void OnContextMenuOpening(ContextMenuEventArgs e) { }

    protected virtual void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e) { }

    protected virtual void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e) { }

    protected virtual void OnLostFocus(RoutedEventArgs e) { }

    internal virtual void AddToEventRouteCore(EventRoute route, RoutedEventArgs args) { }

    // WPF UIElement.RaiseEvent — forward to the shared WPF-style routed event bag
    public void RaiseEvent(RoutedEventArgs e) => WinUIDependencyObjectExtensions.RaiseEvent(this, e);

    // WPF DependencyObject.SetValue accepting a DependencyPropertyKey (read-only DP write path).
    public void SetValue(System.Windows.DependencyPropertyKey key, object? value)
        => SetValue(key.DependencyProperty, value);

    // WPF DependencyObject.CoerceValue — no-op on HAS_UNO.
    protected void CoerceValue(DependencyProperty dp) { }

    // WPF FrameworkElement logical tree management — no-op on HAS_UNO.
    protected void AddLogicalChild(object child) { }
    protected void RemoveLogicalChild(object child) { }

    // WPF FrameworkElement.GetDpi — returns a default DPI scale on HAS_UNO.
    protected DpiScale GetDpi() => new DpiScale(1.0, 1.0);

    // WPF UIElement.AddHandler/RemoveHandler taking System.Windows.RoutedEvent.
    protected void AddHandler(System.Windows.RoutedEvent routedEvent, Delegate handler) =>
        WinUIDependencyObjectExtensions.AddHandler(this, routedEvent, handler);

    protected void RemoveHandler(System.Windows.RoutedEvent routedEvent, Delegate handler) =>
        WinUIDependencyObjectExtensions.RemoveHandler(this, routedEvent, handler);

    // WPF Control.OnVisualStatePropertyChanged — VisualStateManager not used on HAS_UNO.
    internal static void OnVisualStatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

    internal virtual DependencyObjectType DTypeThemeStyleKey => DependencyObjectType.FromSystemTypeInternal(GetType());

    // RichTextBox-level overrides that surface through TextBoxBase
    protected internal virtual IEnumerator LogicalChildren
        => System.Linq.Enumerable.Empty<object>().GetEnumerator();

    internal virtual FrameworkElement CreateRenderScope() => new Microsoft.UI.Xaml.Controls.Grid();

    protected virtual void OnDpiChanged(DpiScale oldDpiScaleInfo, DpiScale newDpiScaleInfo) { }

    protected virtual System.Windows.Automation.Peers.AutomationPeer? OnCreateAutomationPeer() => null;
}

/// <summary>Stub for WPF ControlTemplate — WinUI uses DataTemplate/ControlTemplate via Style.</summary>
public sealed class ControlTemplate
{
    public object? VisualTree { get; set; }
}

/// <summary>Mouse-wheel event args shim.</summary>
public class MouseWheelEventArgs : System.Windows.Input.MouseEventArgs
{
    public int Delta { get; }
    public MouseWheelEventArgs(int delta) => Delta = delta;
}

/// <summary>WPF DPI scale info — not applicable on HAS_UNO.</summary>
public readonly struct DpiScale
{
    public double DpiScaleX { get; }
    public double DpiScaleY { get; }
    public DpiScale(double x, double y) { DpiScaleX = x; DpiScaleY = y; }
}

/// <summary>WPF event-route stub — HAS_UNO never calls AddToEventRouteCore.</summary>
public sealed class EventRoute
{
    public void Add(object handler, bool handledEventsToo) { }
}

// UndoAction is defined in TextChangedEventArgs.cs (System.Windows.Controls namespace).
