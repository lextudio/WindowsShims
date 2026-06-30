using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace System.Windows.Controls;

// Currency-tracking subset of WPF's ItemCollection (which is a full
// CollectionView over either direct items or an ItemsSource view). The shim
// keeps direct items only, raises collection-change notifications, and tracks
// a current item/position with the WPF clamping rules the selector spine
// relies on. Sort descriptions are stored but not applied; the editable-view
// implementation supports direct-list item editing bookkeeping but cannot
// construct new items. Filtering, grouping, and deferred refresh are not
// supported.
public class ItemCollection : Collection<object?>, INotifyCollectionChanged, IEditableCollectionView, IEditableCollectionViewAddNewItem, IItemProperties
{
    // When set, UIElement items inserted into this shim are also forwarded to the
    // WinUI visual-tree items collection so they actually render.
    internal Microsoft.UI.Xaml.Controls.ItemCollection? WinUIItems { get; set; }

    // When set, UIElement items are added to this panel's Children instead of
    // WinUI's ItemsControl.Items. Used by ToolBar to guarantee horizontal layout.
    internal Microsoft.UI.Xaml.Controls.Panel? PanelHost { get; set; }

    private int _currentPosition = -1;
    private SortDescriptionCollection? _sortDescriptions;
    private NewItemPlaceholderPosition _newItemPlaceholderPosition;
    private object? _currentAddItem;

    // Session 119 (Slice 11): WPF-style ICollectionView.Filter. When set, Refresh()
    // rebuilds the visible backing list to the filtered+sorted subset of the retained
    // unfiltered source, so Items itself is the filtered view (matching WPF) and the
    // DataGrid no longer needs a parallel filtered-view shim. Null filter = no filtering
    // (backing stays the full set, sorted in place) — unchanged behavior.
    private List<object?>? _unfilteredSource;

    public Predicate<object?>? Filter { get; set; }

    public SortDescriptionCollection SortDescriptions
    {
        get
        {
            if (_sortDescriptions is null)
            {
                _sortDescriptions = [];
                // Changing sort descriptions marks the view as needing a refresh
                // (the WPF DataGrid sort path checks Items.NeedsRefresh).
                ((INotifyCollectionChanged)_sortDescriptions).CollectionChanged += (_, _) => NeedsRefresh = true;
            }

            return _sortDescriptions;
        }
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public event EventHandler? CurrentChanged;

    public object? CurrentItem
        => _currentPosition >= 0 && _currentPosition < Count ? this[_currentPosition] : null;

    public int CurrentPosition => _currentPosition;

    public bool IsEmpty => Count == 0;

    internal bool IsChanging => false;

    internal object? GetRepresentativeItem() => this.FirstOrDefault(item => !IsPlaceholder(item));

    // WPF exposes the inner CollectionView; the shim is its own (degenerate)
    // view, and item hash codes are never assumed reliable.
    internal ItemCollection CollectionView => this;

    internal bool HasReliableHashCodes() => false;

    // ── IEditableCollectionView (direct-list semantics) ────────────────────
    // The shim supports editable-item bookkeeping plus a narrow direct-list
    // add-new flow, including a placeholder sentinel and user-supplied new
    // items through IEditableCollectionViewAddNewItem.

    public bool CanAddNew => CanAddNewItem || CanConstructNewItem();

    public bool IsAddingNew => _currentAddItem is not null;

    public object? CurrentAddItem => _currentAddItem;

    public bool CanAddNewItem => true;

    public bool CanRemove => true;

    public bool CanCancelEdit => CurrentEditItem is System.ComponentModel.IEditableObject;

    public bool IsEditingItem => CurrentEditItem is not null;

    public object? CurrentEditItem { get; private set; }

    public NewItemPlaceholderPosition NewItemPlaceholderPosition
    {
        get => _newItemPlaceholderPosition;
        set
        {
            if (value is not (NewItemPlaceholderPosition.None
                or NewItemPlaceholderPosition.AtBeginning
                or NewItemPlaceholderPosition.AtEnd))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _newItemPlaceholderPosition = value;
            SyncPlaceholderVisibility();
        }
    }

    public object AddNew()
    {
        var type = GetRepresentativeItem()?.GetType();
        if (type is null)
        {
            throw new InvalidOperationException("AddNew is not supported by the bridge without an item type.");
        }

        var ctor = type.GetConstructor(Type.EmptyTypes);
        if (ctor is null)
        {
            throw new InvalidOperationException($"AddNew is not supported by the bridge for type '{type.Name}' without a parameterless constructor.");
        }

        return AddNewItem(ctor.Invoke(null));
    }

    public object AddNewItem(object newItem)
    {
        if (_currentAddItem is not null)
        {
            throw new InvalidOperationException("A new-item transaction is already active.");
        }

        RemovePlaceholderIfPresent();

        var index = _newItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning ? 0 : Count;
        InsertItem(index, newItem);
        _currentAddItem = newItem;
        (newItem as IEditableObject)?.BeginEdit();
        return newItem;
    }

    public void CommitNew()
    {
        (_currentAddItem as IEditableObject)?.EndEdit();
        _currentAddItem = null;
        SyncPlaceholderVisibility();
    }

    public void CancelNew()
    {
        if (_currentAddItem is null)
        {
            return;
        }

        (_currentAddItem as IEditableObject)?.CancelEdit();
        var index = IndexOf(_currentAddItem);
        _currentAddItem = null;
        if (index >= 0)
        {
            RemoveItem(index);
        }

        SyncPlaceholderVisibility();
    }

    public void EditItem(object item)
    {
        CurrentEditItem = item;
        (item as System.ComponentModel.IEditableObject)?.BeginEdit();
    }

    public void CommitEdit()
    {
        (CurrentEditItem as System.ComponentModel.IEditableObject)?.EndEdit();
        CurrentEditItem = null;
    }

    public void CancelEdit()
    {
        if (CurrentEditItem is not System.ComponentModel.IEditableObject editable)
        {
            throw new InvalidOperationException("CancelEdit is not supported by the bridge (CanCancelEdit is false).");
        }

        editable.CancelEdit();
        CurrentEditItem = null;
    }

    void IEditableCollectionView.Remove(object item) => Remove(item);

    public bool MoveCurrentTo(object? item)
        => MoveCurrentToPosition(IndexOf(item));

    public bool MoveCurrentToPosition(int position)
    {
        if (position < -1 || position > Count)
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        var clamped = position == Count ? -1 : position;
        if (clamped != _currentPosition)
        {
            _currentPosition = clamped;
            CurrentChanged?.Invoke(this, EventArgs.Empty);
        }

        return CurrentItem is not null;
    }

    protected override void InsertItem(int index, object? item)
    {
        base.InsertItem(index, item);
        // Forward UIElements to WinUI visual tree so they actually render.
        if (item is Microsoft.UI.Xaml.UIElement uie)
        {
            if (PanelHost is not null)
                PanelHost.Children.Add(uie);
            else if (WinUIItems is not null)
                WinUIItems.Add(uie);
        }

        // While filtered, the backing list is the view; keep the retained full set in sync
        // so the item survives a filter change.
        if (_unfilteredSource is not null && !IsPlaceholder(item))
        {
            _unfilteredSource.Add(item);
        }

        if (index <= _currentPosition)
        {
            _currentPosition++;
        }

        CollectionChanged?.Invoke(
            this,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
    }

    protected override void RemoveItem(int index)
    {
        var removed = this[index];
        base.RemoveItem(index);
        _unfilteredSource?.Remove(removed);
        if (index < _currentPosition || _currentPosition >= Count)
        {
            MoveCurrentToPosition(Math.Min(_currentPosition - (index < _currentPosition ? 1 : 0), Count - 1));
        }

        CollectionChanged?.Invoke(
            this,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed, index));
    }

    protected override void SetItem(int index, object? item)
    {
        var old = this[index];
        base.SetItem(index, item);
        CollectionChanged?.Invoke(
            this,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, old, index));
    }

    protected override void ClearItems()
    {
        base.ClearItems();
        PanelHost?.Children.Clear();
        if (PanelHost is null) WinUIItems?.Clear();
        _unfilteredSource = null;
        _currentPosition = -1;
        CollectionChanged?.Invoke(
            this,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    // CollectionView-compatible surface used by DataGrid.
    public bool CanSort => true;
    public bool NeedsRefresh { get; private set; }

    public System.Collections.ObjectModel.ObservableCollection<System.ComponentModel.GroupDescription> GroupDescriptions
        => _groupDescriptions ??= new();

    private System.Collections.ObjectModel.ObservableCollection<System.ComponentModel.GroupDescription>? _groupDescriptions;

    // Session 50: apply SortDescriptions to the underlying items (real
    // collection-view sort), so the WPF DataGrid sort path (PerformSort →
    // Items.SortDescriptions → Refresh) drives ordering instead of a shim.
    // Session 119 (Slice 14): bulk-replace the items with a single Reset instead of N
    // per-item Add notifications. ItemsSource population of a large table (e.g. MethodDef,
    // ~30k rows) otherwise fires one CollectionChanged per row, and a hooked DataGrid rebuilds
    // on each → O(n²) and a load timeout. One Reset → one rebuild (and the virtualized panel
    // only realizes the visible window).
    internal void ReplaceAll(System.Collections.IEnumerable? items)
    {
        if (Items is not List<object?> backing)
        {
            return;
        }

        backing.Clear();
        _unfilteredSource = null;
        if (items is not null)
        {
            foreach (var item in items)
            {
                backing.Add(item);
            }
        }

        _currentPosition = -1;

        // Apply the filter/sort view if active (fires its own Reset); otherwise raise a single Reset.
        if (Filter is not null || _sortDescriptions is { Count: > 0 })
        {
            Refresh();
        }
        else
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    public void Refresh()
    {
        NeedsRefresh = false;

        if (Items is not List<object?> backing)
        {
            return;
        }

        var hasSort = _sortDescriptions is { Count: > 0 };
        var hasFilter = Filter is not null;

        // Nothing to apply and no prior filter to undo → preserve the original no-op fast path.
        if (!hasSort && !hasFilter && _unfilteredSource is null)
        {
            return;
        }

        // Capture the full set the first time a filter engages; the view is derived from it
        // so clearing the filter can restore every item.
        if (hasFilter && _unfilteredSource is null)
        {
            _unfilteredSource = new List<object?>(backing);
        }

        var source = _unfilteredSource ?? backing;
        var placeholder = source.FirstOrDefault(IsPlaceholder);
        IEnumerable<object?> visible = source.Where(item => !IsPlaceholder(item));
        if (hasFilter)
        {
            var filter = Filter!;
            visible = visible.Where(item => filter(item));
        }

        if (hasSort)
        {
            visible = ApplySortDescriptions(visible);
        }

        var view = visible.ToList();
        backing.Clear();
        backing.AddRange(view);
        if (placeholder is not null)
        {
            if (_newItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning)
            {
                backing.Insert(0, placeholder);
            }
            else if (_newItemPlaceholderPosition == NewItemPlaceholderPosition.AtEnd)
            {
                backing.Add(placeholder);
            }
        }

        // Filter cleared → the backing list is the full set again; drop the source snapshot.
        if (!hasFilter)
        {
            _unfilteredSource = null;
        }

        _currentPosition = -1;
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    private List<object?> ApplySortDescriptions(IEnumerable<object?> items)
    {
        IOrderedEnumerable<object?>? ordered = null;
        foreach (var sd in _sortDescriptions!)
        {
            var path = sd.PropertyName;
            object? Key(object? item) => item is null || string.IsNullOrEmpty(path)
                ? null
                : item.GetType().GetProperty(path)?.GetValue(item);

            var ascending = sd.Direction == System.ComponentModel.ListSortDirection.Ascending;
            ordered = ordered is null
                ? (ascending ? items.OrderBy(Key, Comparer<object?>.Default) : items.OrderByDescending(Key, Comparer<object?>.Default))
                : (ascending ? ordered.ThenBy(Key, Comparer<object?>.Default) : ordered.ThenByDescending(Key, Comparer<object?>.Default));
        }

        return ordered?.ToList() ?? items.ToList();
    }

    private static bool IsPlaceholder(object? item)
        => ReferenceEquals(item, System.Windows.Data.CollectionView.NewItemPlaceholder);

    private bool CanConstructNewItem()
        => GetRepresentativeItem()?.GetType().GetConstructor(Type.EmptyTypes) is not null;

    private void SyncPlaceholderVisibility()
    {
        RemovePlaceholderIfPresent();
        if (IsAddingNew || _newItemPlaceholderPosition == NewItemPlaceholderPosition.None)
        {
            return;
        }

        var index = _newItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning ? 0 : Count;
        InsertItem(index, System.Windows.Data.CollectionView.NewItemPlaceholder);
    }

    private void RemovePlaceholderIfPresent()
    {
        var index = IndexOf(System.Windows.Data.CollectionView.NewItemPlaceholder);
        if (index >= 0)
        {
            RemoveItem(index);
        }
    }

    public IDisposable DeferRefresh()
    {
        NeedsRefresh = true;
        return new DeferToken(this);
    }

    private sealed class DeferToken : IDisposable
    {
        private readonly ItemCollection _owner;
        internal DeferToken(ItemCollection owner) => _owner = owner;
        public void Dispose() => _owner.Refresh();
    }

    // ── IItemProperties ───────────────────────────────────────────────────
    // WPF's DataGrid.AddAutoColumns() casts Items to IItemProperties to
    // discover column names from the bound data type. Reflect the first
    // non-placeholder item's TypeDescriptor properties.
    ReadOnlyCollection<ItemPropertyInfo>? IItemProperties.ItemProperties
    {
        get
        {
            var representative = GetRepresentativeItem();
            DbgLog($"IItemProperties.ItemProperties: Count={Count} representative={representative?.GetType().Name ?? "null"}");
            if (representative is null)
                return null;
            var props = TypeDescriptor.GetProperties(representative)
                .Cast<PropertyDescriptor>()
                .Select(pd => new ItemPropertyInfo(pd.Name, pd.PropertyType, pd))
                .ToList();
            DbgLog($"  → {props.Count} props: {string.Join(", ", props.Take(5).Select(p => p.Name))}");
            return new ReadOnlyCollection<ItemPropertyInfo>(props);
        }
    }

    private static void DbgLog(string msg)
    {
        try { System.IO.File.AppendAllText("/tmp/roma-debug.log", $"[IC {DateTime.Now:HH:mm:ss.fff}] {msg}\n"); } catch { }
    }
}
