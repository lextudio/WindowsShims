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
public class ItemCollection : Collection<object?>, INotifyCollectionChanged, IEditableCollectionView
{
    private int _currentPosition = -1;
    private SortDescriptionCollection? _sortDescriptions;

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

    internal object? GetRepresentativeItem() => Count > 0 ? this[0] : null;

    // WPF exposes the inner CollectionView; the shim is its own (degenerate)
    // view, and item hash codes are never assumed reliable.
    internal ItemCollection CollectionView => this;

    internal bool HasReliableHashCodes() => false;

    // ── IEditableCollectionView (degenerate direct-list semantics) ─────────
    // New-item construction needs an item type/factory the shim does not
    // have, so adding is unsupported; edit tracking is bookkeeping only.

    public bool CanAddNew => false;

    public bool IsAddingNew => false;

    public object? CurrentAddItem => null;

    public bool CanRemove => true;

    public bool CanCancelEdit => false;

    public bool IsEditingItem => CurrentEditItem is not null;

    public object? CurrentEditItem { get; private set; }

    public NewItemPlaceholderPosition NewItemPlaceholderPosition
    {
        get => NewItemPlaceholderPosition.None;
        set
        {
            if (value != NewItemPlaceholderPosition.None)
            {
                throw new NotSupportedException("New-item placeholders are not supported by the bridge.");
            }
        }
    }

    public object AddNew() => throw new InvalidOperationException("AddNew is not supported by the bridge.");

    public void CommitNew()
    {
    }

    public void CancelNew()
    {
    }

    public void EditItem(object item)
    {
        CurrentEditItem = item;
    }

    public void CommitEdit()
    {
        CurrentEditItem = null;
    }

    public void CancelEdit()
        => throw new InvalidOperationException("CancelEdit is not supported by the bridge (CanCancelEdit is false).");

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
    public void Refresh()
    {
        NeedsRefresh = false;

        if (_sortDescriptions is { Count: > 0 } && Items is List<object?> backing && backing.Count > 1)
        {
            var ordered = ApplySortDescriptions(backing);
            backing.Clear();
            backing.AddRange(ordered);
            _currentPosition = -1;
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
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
}
