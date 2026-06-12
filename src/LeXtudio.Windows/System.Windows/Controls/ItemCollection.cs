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

    public SortDescriptionCollection SortDescriptions => _sortDescriptions ??= [];

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
}
