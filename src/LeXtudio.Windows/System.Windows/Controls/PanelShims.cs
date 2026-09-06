namespace System.Windows.Controls;

// Session 119 (DataGrid virtualization, Slice 3): the WPF Panel shim now derives
// from the real WinUI Microsoft.UI.Xaml.Controls.Panel so that the WPF-derived
// virtualizing panel chain (Panel -> VirtualizingPanel -> VirtualizingStackPanel ->
// DataGridRowsPresenter, and DataGridCellsPanel) becomes a *live* layout element:
// Uno calls its MeasureOverride/ArrangeOverride, and InternalChildren are real
// visual children. The WPF-shaped surface (InternalChildren, IsItemsHost,
// OnIsItemsHostChanged) is bridged on top; Background and Children come from the
// WinUI base. UIElementCollection is aliased to the WinUI collection (GlobalUsings).
public class Panel : Microsoft.UI.Xaml.Controls.Panel
{
    public static readonly DependencyProperty IsItemsHostProperty =
        DependencyProperty.Register(
            "IsItemsHost",
            typeof(bool),
            typeof(Panel),
            new PropertyMetadata(false, (d, e) =>
            {
                if (d is Panel panel)
                {
                    panel.OnIsItemsHostChanged((bool)e.OldValue, (bool)e.NewValue);
                }
            }));

    // WPF's Panel.InternalChildren is the protected accessor the virtualizing panels
    // mutate; map it onto the live WinUI Children collection.
    protected internal UIElementCollection InternalChildren => Children;

    public bool IsItemsHost
    {
        get => (bool)GetValue(IsItemsHostProperty);
        set => SetValue(IsItemsHostProperty, value);
    }

    protected virtual void OnIsItemsHostChanged(bool oldIsItemsHost, bool newIsItemsHost)
    {
    }
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

    /// <summary>
    /// WPF's ScrollContentPresenter.ComputeScrollOffsetWithMinimalScroll, ported from
    /// upstream: the scroll offset that brings a child rect into a viewport with the least
    /// possible motion, aligning only the nearer edge when the child does not fully fit.
    /// </summary>
    internal static double ComputeScrollOffsetWithMinimalScroll(
        double topView, double bottomView, double topChild, double bottomChild)
    {
        bool alignTop = false;
        bool alignBottom = false;
        return ComputeScrollOffsetWithMinimalScroll(topView, bottomView, topChild, bottomChild, ref alignTop, ref alignBottom);
    }

    internal static double ComputeScrollOffsetWithMinimalScroll(
        double topView, double bottomView, double topChild, double bottomChild,
        ref bool alignTop, ref bool alignBottom)
    {
        // # CHILD POSITION       CHILD SIZE      SCROLL      REMEDY
        // 1 Above viewport       <= viewport     Down        Align top edge of child & viewport
        // 2 Above viewport       > viewport      Down        Align bottom edge of child & viewport
        // 3 Below viewport       <= viewport     Up          Align bottom edge of child & viewport
        // 4 Below viewport       > viewport      Up          Align top edge of child & viewport
        // 5 Entirely within viewport             NA          No scroll.
        // 6 Spanning viewport                    NA          No scroll.
        bool above = topChild < topView && bottomChild < bottomView;
        bool below = topChild > topView && bottomChild > bottomView;
        bool larger = (bottomChild - topChild) > (bottomView - topView);

        if ((above && !larger) || (below && larger))
        {
            alignTop = true;
            alignBottom = false;
            return topChild;
        }

        if (above || below)
        {
            alignTop = false;
            alignBottom = true;
            return bottomChild - (bottomView - topView);
        }

        // Entirely within, or spanning, the viewport: no scroll needed.
        return topView;
    }

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
