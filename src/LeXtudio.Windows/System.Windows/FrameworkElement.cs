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

    // Session 60: the linked DataGridComboBoxColumn has an ApplyStyle overload
    // targeting a FrameworkContentElement; WinUI's Style is the effective type.
    public Microsoft.UI.Xaml.Style? Style { get; set; }

    // See WinUIFrameworkElementExtensions for the FrameworkElement counterpart.
    public bool IsLogicalChildrenIterationInProgress => false;

    public virtual void BeginInit() { }

    public virtual void EndInit() { }

    protected virtual System.Windows.Automation.Peers.AutomationPeer? OnCreateAutomationPeer() => null;

    // WPF FrameworkContentElement.OnPropertyChanged — called by property engine on value changes.
    protected virtual void OnPropertyChanged(DependencyPropertyChangedEventArgs e) { }

    // WPF UIElement.IsEnabledCore — overridden by FlowDocument to propagate RichTextBox read-only state.
    protected virtual bool IsEnabledCore => true;

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

    internal virtual int EffectiveValuesInitialSize => 2;
    internal virtual DependencyObjectType? DTypeThemeStyleKey => null;

    public bool IsInitialized => true;

    // Called by the WPF tree machinery when an element gets a new logical parent.
    // TableRow, TableCell, and TableRowGroup override this to wire up parent references.
    internal virtual void OnNewParent(DependencyObject newParent) { }

    // WPF's xml:lang / language property; used by TextRangeSerialization for special-casing xml:lang attribute.
    public static readonly DependencyProperty LanguageProperty =
        DependencyProperty.Register("Language", typeof(System.Windows.Markup.XmlLanguage), typeof(FrameworkContentElement), null);

    protected internal virtual System.Collections.IEnumerator LogicalChildren =>
        System.Linq.Enumerable.Empty<object>().GetEnumerator();

    protected void VerifyAccess() { }
}
