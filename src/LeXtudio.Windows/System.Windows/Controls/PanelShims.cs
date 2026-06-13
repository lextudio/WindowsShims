namespace System.Windows.Controls;

public class Panel : FrameworkElement
{
    private readonly UIElementCollection _children;

    public Panel() => _children = new UIElementCollection(this);

    public static DependencyProperty BackgroundProperty { get; internal set; }

    public UIElementCollection Children => _children;
}

// Minimal UIElementCollection shim: DataGrid accesses Panel.Children to
// enumerate visual children; the shim stores a simple list.
public sealed class UIElementCollection : System.Collections.ObjectModel.Collection<UIElement>
{
    internal UIElementCollection(Panel owner) { }
}

// Decorator: single-child FrameworkElement. WPF's Decorator is in PresentationFramework
// but is not available in WinUI. This shim provides the base class needed by AdornerDecorator.
public class Decorator : FrameworkElement
{
    private UIElement _child;

    public virtual UIElement Child
    {
        get => _child;
        set => _child = value;
    }

    // Internal virtual to allow NonLogicalAdornerDecorator to bypass logical-tree tracking.
    internal virtual UIElement IntChild
    {
        get => _child;
        set => _child = value;
    }

    // No-op stubs: WPF visual-tree management methods not available in WinUI.
    protected void AddVisualChild(UIElement child) { }
    protected void RemoveVisualChild(UIElement child) { }
    protected void AddLogicalChild(object child) { }
    protected void RemoveLogicalChild(object child) { }
}

public class ScrollContentPresenter : FrameworkElement
{
    // AdornerLayer property used by AdornerLayer.GetAdornerLayer visual-tree walk.
    public System.Windows.Documents.AdornerLayer AdornerLayer => null;

    // WPF attached property that enables logical (item-based) scrolling.
    public static readonly DependencyProperty CanContentScrollProperty =
        DependencyProperty.RegisterAttached("CanContentScroll", typeof(bool),
            typeof(ScrollContentPresenter), new PropertyMetadata(false));

    public static bool GetCanContentScroll(DependencyObject element)
        => (bool)element.GetValue(CanContentScrollProperty);

    public static void SetCanContentScroll(DependencyObject element, bool value)
        => element.SetValue(CanContentScrollProperty, value);

    // Instance property for direct access on a ScrollContentPresenter.
    public bool CanContentScroll
    {
        get => GetCanContentScroll(this);
        set => SetCanContentScroll(this, value);
    }
}
