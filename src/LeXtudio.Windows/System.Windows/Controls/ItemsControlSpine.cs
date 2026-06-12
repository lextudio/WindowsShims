using System.Collections;
using System.Collections.Specialized;
using MS.Internal.Controls;

namespace System.Windows.Controls;

// WPF-shaped virtuals that Selector/MultiSelector override. WPF declares these
// across UIElement/FrameworkElement/ItemsControl; the shim hosts them all on
// ItemsControl because the Uno base types do not have them. They are no-op
// hooks until item-container generation exists.
public partial class ItemsControl : IGeneratorHost
{
    private ItemContainerGenerator? _itemContainerGenerator;

    public ItemContainerGenerator ItemContainerGenerator => _itemContainerGenerator ??= new ItemContainerGenerator();

    bool IGeneratorHost.IsItemItsOwnContainer(object item) => IsItemItsOwnContainerOverride(item);

    // No container generation, so containers never resolve to their owner.
    public static ItemsControl? ItemsControlFromItemContainer(DependencyObject container) => null;

    protected virtual void OnInitialized(EventArgs e)
    {
    }

    protected virtual void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
    {
    }

    protected virtual void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
    }

    protected virtual void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
    {
    }

    protected virtual void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
    }

    protected virtual void ClearContainerForItemOverride(DependencyObject element, object item)
    {
    }

    internal virtual void AdjustItemInfoOverride(NotifyCollectionChangedEventArgs e)
    {
    }

    // Info adjustment tracks generated containers in WPF; without a generator
    // the stored infos have nothing to adjust.
    internal void AdjustItemInfos(NotifyCollectionChangedEventArgs e, IEnumerable<ItemInfo> list)
    {
    }

    internal void AdjustItemInfosAfterGeneratorChange(IEnumerable<ItemInfo> list, bool claimUniqueContainer)
    {
    }

    // WPF control/element virtuals the DataGrid control root overrides.
    protected virtual void OnTextInput(Input.TextCompositionEventArgs e)
    {
    }

    // The KeyEventArgs shim lives in System.Windows (matching how linked files
    // resolve it through enclosing-namespace lookup).
    protected virtual void OnKeyDown(KeyEventArgs e)
    {
    }

    protected virtual void OnMouseMove(Input.MouseEventArgs e)
    {
    }

    protected virtual void OnIsMouseCapturedChanged(DependencyPropertyChangedEventArgs e)
    {
    }

    internal virtual void OnIsGroupingChanged(DependencyPropertyChangedEventArgs e)
    {
    }

    protected virtual void OnContextMenuOpening(ContextMenuEventArgs e)
    {
    }

    protected virtual void OnTemplateChanged(ControlTemplate oldTemplate, ControlTemplate newTemplate)
    {
    }

    protected internal virtual bool HandlesScrolling => false;

    protected virtual DependencyObject? GetContainerForItemOverride() => null;

    internal virtual void ChangeVisualState(bool useTransitions)
    {
    }

    // Containers are never generated, so focusing an item's container is a no-op.
    internal virtual bool FocusItem(ItemInfo info, ItemNavigateArgs itemNavigateArgs) => false;

    // Bridge subset of WPF's nested navigation-args type.
    internal class ItemNavigateArgs
    {
        private static ItemNavigateArgs? _empty;

        public ItemNavigateArgs(Input.InputDevice? deviceUsed, Input.ModifierKeys modifierKeys)
        {
            DeviceUsed = deviceUsed;
            ModifierKeys = modifierKeys;
        }

        public Input.InputDevice? DeviceUsed { get; }

        internal Input.ModifierKeys ModifierKeys { get; }

        public static ItemNavigateArgs Empty => _empty ??= new ItemNavigateArgs(null, Input.ModifierKeys.None);
    }

    public bool HasItems => Items.Count > 0;

    // WPF resolves containers through the item container generator; the shim
    // keeps caller-provided state until one exists.
    internal ItemInfo NewItemInfo(object? item, DependencyObject? container = null, int index = -1)
        => new(item, container, index);

    internal ItemInfo NewUnresolvedItemInfo(object? item)
        => new(item, ItemInfo.UnresolvedContainer, -1);

    internal ItemInfo? ItemInfoFromIndex(int index)
        => index >= 0 && index < Items.Count
            ? new ItemInfo(Items[index], null, index)
            : null;

    // No container generation yet, so containers never map back to items.
    internal static object? GetItemOrContainerFromContainer(DependencyObject container) => null;

    protected virtual bool IsItemItsOwnContainerOverride(object item) => item is Microsoft.UI.Xaml.UIElement;

    // WPF inherits these from DispatcherObject/DependencyObject. They live on
    // the shim because upstream code calls them without a receiver, where C#
    // does not consider extension members.
    public bool CheckAccess() => true;

    public void VerifyAccess()
    {
    }

    internal void SetValue(DependencyPropertyKey key, object? value)
        => SetValue(key.DependencyProperty, value);

    internal void ClearValue(DependencyPropertyKey key)
        => ClearValue(key.DependencyProperty);

    // Uno has no current-value layer distinct from local values.
    internal void SetCurrentValueInternal(DependencyProperty dp, object? value)
        => SetValue(dp, value);

    public void CoerceValue(DependencyProperty dp)
    {
    }

    // Bare-call routed-event plumbing; explicit-receiver sites use the
    // DependencyObject extension members backed by the same handler bags.
    public void AddHandler(RoutedEvent routedEvent, Delegate handler)
        => ((Microsoft.UI.Xaml.DependencyObject)this).AddHandler(routedEvent, handler);

    public void RemoveHandler(RoutedEvent routedEvent, Delegate handler)
        => ((Microsoft.UI.Xaml.DependencyObject)this).RemoveHandler(routedEvent, handler);

    public void RaiseEvent(RoutedEventArgs e)
        => ((Microsoft.UI.Xaml.DependencyObject)this).RaiseEvent(e);

    // WPF ItemsSource swaps the inner view; the shim only stores the value
    // until the items/view pipeline exists.
    public IEnumerable? ItemsSource { get; set; }

    // Focus tracking is not bridged; WPF reads this during selection-active
    // bookkeeping only.
    public bool IsKeyboardFocusWithin => false;

    // The shim has no ISupportInitialize phase, so elements are always
    // considered initialized.
    public bool IsInitialized => true;
}
