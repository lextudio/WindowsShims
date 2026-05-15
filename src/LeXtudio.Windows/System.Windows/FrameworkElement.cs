namespace System.Windows;

// FrameworkElement is aliased to Microsoft.UI.Xaml.FrameworkElement in GlobalUsings.cs.
// Any WPF-specific FrameworkElement APIs that WinUI lacks are provided via
// WinUIFrameworkElementExtensions.cs extension members.

/// <summary>
/// WPF FrameworkContentElement shim — base for document content elements (TextElement, etc.)
/// that are not UIElements. No WinUI equivalent exists; inherits directly from DependencyObject
/// (which is now aliased to Microsoft.UI.Xaml.DependencyObject).
/// </summary>
public partial class FrameworkContentElement : DependencyObject
{
    public DependencyObject? Parent { get; internal set; }

    public virtual void BeginInit() { }

    public virtual void EndInit() { }

    protected virtual System.Windows.Automation.Peers.AutomationPeer? OnCreateAutomationPeer() => null;

    // WPF-style routed event methods — called without `this.` in upstream WPF source files.
    // They forward to the C# 14 extension members defined in WinUIDependencyObjectExtensions.cs.
    public void AddHandler(RoutedEvent routedEvent, Delegate handler) =>
        WinUIDependencyObjectExtensions.AddHandler(this, routedEvent, handler);

    public void RemoveHandler(RoutedEvent routedEvent, Delegate handler) =>
        WinUIDependencyObjectExtensions.RemoveHandler(this, routedEvent, handler);

    public void RaiseEvent(RoutedEventArgs args) =>
        WinUIDependencyObjectExtensions.RaiseEvent(this, args);

    public virtual bool Focus() => true;

    public void CoerceValue(DependencyProperty property) { }

    // WPF-internal helpers used by upstream document types.
    protected void SetCurrentDeferredValue(DependencyProperty property, object? value) =>
        SetValue(property, value);

    protected object LookupEntry(int globalIndex) => new();

    protected bool HasExpression(object entry, DependencyProperty property) => false;
}
