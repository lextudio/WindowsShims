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
    protected static readonly DependencyProperty FocusableProperty =
        DependencyProperty.Register("Focusable", typeof(bool), typeof(ItemsControl), new PropertyMetadata(true));

    private ItemContainerGenerator? _itemContainerGenerator;

    public ItemContainerGenerator ItemContainerGenerator => _itemContainerGenerator ??= new ItemContainerGenerator(this);

    bool IGeneratorHost.IsItemItsOwnContainer(object item) => IsItemItsOwnContainerOverride(item);

    // No container generation, so containers never resolve to their owner.
    public static ItemsControl? ItemsControlFromItemContainer(DependencyObject container) => null;

    // Session 119 (Slice 4): the panel marked IsItemsHost resolves its owning
    // ItemsControl by walking up the live visual tree. Returns null until a panel
    // is actually installed as an items host (so the virtualizing engine stays
    // inert on the current manual render path).
    public static ItemsControl? GetItemsOwner(DependencyObject element)
    {
        if (element is not Panel { IsItemsHost: true })
            return null;

        DependencyObject? current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(element);
        while (current is not null)
        {
            if (current is ItemsControl owner)
                return owner;
            current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    protected virtual void OnInitialized(EventArgs e)
    {
    }

    protected virtual void OnItemTemplateChanged(DataTemplate oldTemplate, DataTemplate newTemplate)
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

    internal bool IsItemItsOwnContainerInternal(object? item) => IsItemItsOwnContainerOverride(item!);

    internal DependencyObject? CreateContainerForItem(object? item)
        => IsItemItsOwnContainerOverride(item!) && item is DependencyObject dependencyObject
            ? dependencyObject
            : GetContainerForItemOverride();

    internal void PrepareContainerForItem(DependencyObject container, object? item)
    {
        if (item is not null)
        {
            PrepareContainerForItemOverride(container, item);
        }
    }

    internal void ClearContainerForItem(DependencyObject container, object? item)
    {
        if (item is not null)
        {
            ClearContainerForItemOverride(container, item);
        }
    }

    // Session 119 (Slice 7): shim-specific hooks the virtualizing panel calls as
    // containers enter/leave the realized window, carrying the display index that
    // PrepareContainerForItemOverride does not receive. DataGrid overrides these to
    // apply alternating background / selection visuals and keep the generator
    // registry in sync. No-op by default.
    internal virtual void ShimOnContainerRealized(DependencyObject container, object? item, int index)
    {
    }

    internal virtual void ShimOnContainerRecycled(DependencyObject container, object? item)
    {
    }


    internal virtual void ChangeVisualState(bool useTransitions)
    {
    }

    protected virtual Geometry GetLayoutClip(Size layoutSlotSize) => null;

    protected virtual int VisualChildrenCount => 0;

    protected virtual Visual GetVisualChild(int index) => null;

    protected void AddVisualChild(UIElement child)
    {
    }

    protected void RemoveVisualChild(UIElement child)
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

    // Session 121 (DataGrid grouping, Slice 1): reflects Items.GroupDescriptions
    // now that ItemCollection actually builds a group tree on Refresh(). Container
    // generation for GroupItem/DataGrid row-group headers over that tree is a
    // later slice, so code gated on IsGrouping that assumes a rendered group
    // header (not just correct data ordering) is still unreached in practice.
    public bool IsGrouping => Items.GroupDescriptions.Count > 0;

    // Session 121 (DataGrid grouping, Slice 4): per-nesting-level header/container
    // customization, mirroring upstream WPF's ItemsControl.GroupStyle collection
    // (indexed by group depth — GroupItem.ShimPrepareGroupHeader clamps depth to
    // the last entry, matching WPF's own GroupStyleSelector-less fallback). Empty
    // by default, so existing grouped grids keep Slice 2's fixed "{name} ({count})"
    // header until a caller opts in.
    public System.Collections.ObjectModel.ObservableCollection<GroupStyle> GroupStyle
        => _groupStyle ??= new();

    private System.Collections.ObjectModel.ObservableCollection<GroupStyle>? _groupStyle;

    // Session 121 (DataGrid grouping, Slice 5): mirrors upstream's
    // ItemsControl.GroupStyleSelector — resolved ahead of the GroupStyle
    // collection when set (see GroupItem.ResolveGroupStyle).
    public GroupStyleSelector? GroupStyleSelector { get; set; }

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
    // Shadow the WinUI CoreDispatcher Dispatcher property with our WPF-compatible shim.
    public new Threading.Dispatcher Dispatcher => Threading.Dispatcher.CurrentDispatcher;

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

    // Focus traversal is not bridged; WPF callers fall through their
    // no-focus-moved paths.
    public bool MoveFocus(Input.TraversalRequest request) => false;

    public bool Focus() => false;

    // WPF ItemsSource swaps the inner view; the shim populates Items to
    // keep IItemProperties.ItemProperties and OrderedItems() working for
    // DataGrid auto-column generation and row rendering.
    private IEnumerable? _itemsSource;
    public IEnumerable? ItemsSource
    {
        get => _itemsSource;
        set
        {
            var old = _itemsSource;
            _itemsSource = value;
            SyncItemsFromSource(old, value);
            OnItemsSourceChanged(old, value);
        }
    }

    private void SyncItemsFromSource(IEnumerable? oldSource, IEnumerable? newSource)
    {
        // Unsubscribe from old source notifications
        if (oldSource is System.Collections.Specialized.INotifyCollectionChanged oldNcc)
            oldNcc.CollectionChanged -= OnItemsSourceCollectionChanged;

        // Bulk-populate with a single Reset (avoids an O(n²) per-item rebuild storm on
        // large tables).
        Items.ReplaceAll(newSource);

        // Subscribe to live updates
        if (newSource is System.Collections.Specialized.INotifyCollectionChanged newNcc)
            newNcc.CollectionChanged += OnItemsSourceCollectionChanged;
    }

    private void OnItemsSourceCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                if (e.NewItems is not null)
                    foreach (var item in e.NewItems)
                        Items.Insert(e.NewStartingIndex, item);
                break;
            case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                if (e.OldItems is not null)
                    for (var i = 0; i < e.OldItems.Count; i++)
                        Items.RemoveAt(e.OldStartingIndex);
                break;
            case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                Items.ReplaceAll(sender as IEnumerable);
                break;
        }
    }

    // Focus tracking is not bridged; WPF reads this during selection-active
    // bookkeeping only.
    public bool IsKeyboardFocusWithin => false;

    // The shim has no ISupportInitialize phase, so elements are always
    // considered initialized.
    public bool IsInitialized => true;

    // ── DataGrid-required ItemsControl surface ────────────────────────────

    // AutoScrollTimeout: WPF ItemsControl static used by DataGrid's drag-scroll timer.
    internal static TimeSpan AutoScrollTimeout { get; } = TimeSpan.FromMilliseconds(200);

    // ItemsHost: the panel that lays out item containers.
    internal Panel? ItemsHost => null;

    // IsEnabled / IsEnabledProperty, DefaultStyleKeyProperty, and
    // IsTabStopProperty now come from the WinUI Control base (session 24
    // rebase). DataGrid's static ctor OverrideMetadata calls resolve to the
    // Control DPs via the no-op shim extension.

    public static readonly DependencyProperty ItemContainerStyleProperty =
        DependencyProperty.Register("ItemContainerStyle", typeof(Style), typeof(ItemsControl),
            new PropertyMetadata(null));

    public Style? ItemContainerStyle
    {
        get => (Style?)GetValue(ItemContainerStyleProperty);
        set => SetValue(ItemContainerStyleProperty, value);
    }

    public static readonly DependencyProperty ItemContainerStyleSelectorProperty =
        DependencyProperty.Register("ItemContainerStyleSelector", typeof(StyleSelector), typeof(ItemsControl),
            new PropertyMetadata(null));

    public StyleSelector? ItemContainerStyleSelector
    {
        get => (StyleSelector?)GetValue(ItemContainerStyleSelectorProperty);
        set => SetValue(ItemContainerStyleSelectorProperty, value);
    }

    public static readonly DependencyProperty ItemsPanelProperty =
        DependencyProperty.Register("ItemsPanel", typeof(ItemsPanelTemplate), typeof(ItemsControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register("ItemsSourceDP", typeof(IEnumerable), typeof(ItemsControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty AlternationCountProperty =
        DependencyProperty.Register("AlternationCount", typeof(int), typeof(ItemsControl),
            new PropertyMetadata(0));

    public static readonly DependencyProperty AlternationIndexProperty =
        DependencyProperty.RegisterAttached("AlternationIndex", typeof(int), typeof(ItemsControl),
            new PropertyMetadata(0));

    public static readonly DependencyProperty IsTextSearchEnabledProperty =
        DependencyProperty.Register("IsTextSearchEnabled", typeof(bool), typeof(ItemsControl),
            new PropertyMetadata(false));

    public bool IsTextSearchEnabled
    {
        get => (bool)GetValue(IsTextSearchEnabledProperty);
        set => SetValue(IsTextSearchEnabledProperty, value);
    }

    public static readonly DependencyProperty ItemBindingGroupProperty =
        DependencyProperty.Register("ItemBindingGroup", typeof(Data.BindingGroup), typeof(ItemsControl),
            new PropertyMetadata(null));

    public Data.BindingGroup? ItemBindingGroup
    {
        get => (Data.BindingGroup?)GetValue(ItemBindingGroupProperty);
        set => SetValue(ItemBindingGroupProperty, value);
    }

    // IsMouseCaptured / mouse capture: not bridged at the UIElement level;
    // DataGrid reads it during selection drag-scroll.
    public bool IsMouseCaptured => false;

    public void ReleaseMouseCapture() { }

    public static readonly RoutedEvent MouseUpEvent = new();

    // IsKeyboardFocusWithinPropertyKey: WPF read-only DP for focus-within.
    internal static readonly DependencyPropertyKey IsKeyboardFocusWithinPropertyKey =
        DependencyProperty.RegisterReadOnly("IsKeyboardFocusWithin_Shim", typeof(bool),
            typeof(ItemsControl), new PropertyMetadata(false));

    public static readonly DependencyProperty IsKeyboardFocusWithinProperty =
        IsKeyboardFocusWithinPropertyKey.DependencyProperty;

    // SetCurrentValue: WPF method that sets local value without triggering
    // expression coercion; maps to SetValue on Uno.
    public void SetCurrentValue(DependencyProperty dp, object? value) => SetValue(dp, value);

    // SetBinding override: routes our BindingBase through BindingOperations so
    // the caller does not have to convert to WinUI's BindingBase.
    public void SetBinding(DependencyProperty dp, Data.BindingBase binding)
        => Data.BindingOperations.SetBinding(this, dp, binding);

    // Container/item-info lookups resolve through the generator registry
    // (session 27). Prefer index, fall back to item identity.
    internal DependencyObject? ContainerFromItemInfo(ItemInfo info)
    {
        if (info.Index >= 0 && ItemContainerGenerator.ContainerFromIndex(info.Index) is { } byIndex)
        {
            return byIndex;
        }

        return info.Item is not null ? ItemContainerGenerator.ContainerFromItem(info.Item) : null;
    }

    internal ItemInfo ItemInfoFromContainer(DependencyObject container)
    {
        var item = ItemContainerGenerator.ItemFromContainer(container);
        var index = ItemContainerGenerator.IndexFromContainer(container);
        return NewItemInfo(item == DependencyProperty.UnsetValue ? null : item, container, index);
    }

    internal ItemInfo LeaseItemInfo(ItemInfo info, bool ensureIndex = false) => info;

    // Navigation by line/page: no-op until a real scroll host exists.
    internal virtual object? OnBringItemIntoView(object arg) => null;
    internal virtual object? OnBringItemIntoView(ItemInfo info) => null;

    internal virtual void PrepareNavigateByLine(
        ItemInfo startingInfo,
        FrameworkElement startingElement,
        Input.FocusNavigationDirection direction,
        ItemNavigateArgs args,
        out FrameworkElement? targetElement) { targetElement = null; }

    internal virtual void PrepareToNavigateByPage(
        ItemInfo startingInfo,
        FrameworkElement startingElement,
        Input.FocusNavigationDirection direction,
        ItemNavigateArgs args,
        out FrameworkElement? targetElement) { targetElement = null; }

    // WPF UIElement.PredictFocus returns the element that would receive focus
    // in the given direction. The shim always returns null.
    public DependencyObject? PredictFocus(Input.FocusNavigationDirection direction) => null;

}
