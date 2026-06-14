using System.Windows.Controls.Primitives;

namespace System.Windows.Controls;

// ScrollUnit: controls whether scroll offsets are in pixels or logical items.
public enum ScrollUnit
{
    Pixel = 0,
    Item  = 1,
}

// VirtualizingPanel: abstract base for WPF virtualizing panels. Extends our
// Panel shim so casts like `panel as VirtualizingStackPanel` compile and work.
public abstract class VirtualizingPanel : Panel
{
    public static readonly DependencyProperty IsVirtualizingProperty =
        DependencyProperty.RegisterAttached("IsVirtualizing", typeof(bool),
            typeof(VirtualizingPanel), new PropertyMetadata(true));

    public static readonly DependencyProperty VirtualizationModeProperty =
        DependencyProperty.RegisterAttached("VirtualizationMode", typeof(VirtualizationMode),
            typeof(VirtualizingPanel), new PropertyMetadata(VirtualizationMode.Standard));

    internal static readonly DependencyProperty ShouldCacheContainerSizeProperty =
        DependencyProperty.RegisterAttached(
            "ShouldCacheContainerSize",
            typeof(bool),
            typeof(VirtualizingPanel),
            new FrameworkPropertyMetadata(true));

    public static ScrollUnit GetScrollUnit(UIElement element) => ScrollUnit.Item;

    public static VirtualizationMode GetVirtualizationMode(DependencyObject element)
        => (VirtualizationMode)element.GetValue(VirtualizationModeProperty);

    public static void SetVirtualizationMode(DependencyObject element, VirtualizationMode value)
        => element.SetValue(VirtualizationModeProperty, value);

    public static bool GetIsVirtualizing(DependencyObject element)
        => (bool)element.GetValue(IsVirtualizingProperty);

    public static void SetIsVirtualizing(DependencyObject element, bool value)
        => element.SetValue(IsVirtualizingProperty, value);

    // IsVirtualizing / InRecyclingMode instance flags live on the linked
    // VirtualizingPanel subclasses (e.g. DataGridCellsPanel) themselves, matching
    // upstream WPF; the attached-DP getters above remain the shared source.

    // WPF VirtualizingPanel.ItemContainerGenerator: the generator scoped to this
    // panel, obtained from the items owner. The linked DataGridCellsPanel measure
    // path drives generation through it.
    protected IItemContainerGenerator? ItemContainerGenerator
        => ItemsControl.GetItemsOwner(this)?.ItemContainerGenerator is IItemContainerGenerator generator
            ? generator.GetItemContainerGeneratorForPanel(this)
            : null;

    // WPF UIElement.MeasureDirty flag, used by BringIndexIntoView retry timing.
    // The Uno layout pass does not expose it; reporting clean keeps the
    // single-pass behavior.
    internal bool MeasureDirty => false;

    // WPF UIElement.MeasureDuringArrange flag. The Uno measure/arrange split does
    // not re-enter measure during arrange, so this is always false.
    internal bool MeasureDuringArrange => false;

    protected override void OnIsItemsHostChanged(bool oldIsItemsHost, bool newIsItemsHost)
    {
    }

    protected virtual void OnClearChildren()
    {
    }

    protected internal virtual void BringIndexIntoView(int index)
    {
    }

    protected virtual void OnItemsChanged(object sender, ItemsChangedEventArgs args)
    {
    }

    protected void AddInternalChild(UIElement child)
        => InternalChildren.Add(child);

    protected void InsertInternalChild(int index, UIElement child)
        => InternalChildren.Insert(index, child);

    protected void RemoveInternalChildRange(int index, int range)
        => RemoveInternalChildRange(InternalChildren, index, range);

    internal static void AddInternalChild(UIElementCollection children, UIElement child)
        => children.Add(child);

    internal static void InsertInternalChild(UIElementCollection children, int index, UIElement child)
        => children.Insert(index, child);

    internal static void RemoveInternalChildRange(UIElementCollection children, int index, int range)
    {
        for (var i = 0; i < range && index < children.Count; i++)
        {
            children.RemoveAt(index);
        }
    }
}

// VirtualizingStackPanel: virtualizes items in a stack layout. DataGrid casts
// its items host to this type to check scroll state; returns null in our shim.
public class VirtualizingStackPanel : VirtualizingPanel
{
    internal bool IgnoreMaxDesiredSize { get; set; }

    protected internal override void BringIndexIntoView(int index)
    {
    }

    protected override void OnIsItemsHostChanged(bool oldIsItemsHost, bool newIsItemsHost)
    {
    }

    protected virtual void OnViewportSizeChanged(Size oldViewportSize, Size newViewportSize)
    {
    }

    protected virtual void OnCleanUpVirtualizedItem(CleanUpVirtualizedItemEventArgs e)
    {
    }
}

public sealed class CleanUpVirtualizedItemEventArgs : EventArgs
{
    public UIElement? UIElement { get; init; }

    public bool Cancel { get; set; }
}
