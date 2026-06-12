using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace System.Windows.Controls;

// Currency-tracking subset of WPF's ItemCollection (which is a full
// CollectionView over either direct items or an ItemsSource view). The shim
// keeps direct items only, raises collection-change notifications, and tracks
// a current item/position with the WPF clamping rules the selector spine
// relies on. View features (filtering, sorting, deferred refresh, hash-code
// optimization) are not supported.
public class ItemCollection : Collection<object?>, INotifyCollectionChanged
{
    private int _currentPosition = -1;

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
