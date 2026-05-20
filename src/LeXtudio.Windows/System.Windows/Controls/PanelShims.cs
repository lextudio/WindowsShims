namespace System.Windows.Controls;

public class Panel : FrameworkElement
{
    public static DependencyProperty BackgroundProperty { get; internal set; }
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
}
