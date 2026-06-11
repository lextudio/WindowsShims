using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace System.Windows.Controls;

public partial class DataGrid : Control
{
    private readonly DataGridColumnCollection _columns;
    private SelectedCellsCollection? _selectedCells;

    public DataGrid()
    {
        _columns = new DataGridColumnCollection(this);
        _columns.CollectionChanged += OnColumnsChanged;
    }

    public ObservableCollection<DataGridColumn> Columns => _columns;

    // WPF inherits Items from ItemsControl. The shell keeps a plain item list
    // until the selector spine exists.
    public ItemCollection Items { get; } = new();

    public IList<DataGridCellInfo> SelectedCells => _selectedCells ??= new SelectedCellsCollection(this);

    public event SelectedCellsChangedEventHandler? SelectedCellsChanged;

    public DataGridGridLinesVisibility GridLinesVisibility { get; set; } = DataGridGridLinesVisibility.All;

    internal DataGridColumnCollection InternalColumns => _columns;

    // WPF inherits NewItemInfo from ItemsControl, which also resolves the
    // container/index from the item container generator. The shell has no
    // generator yet, so the info keeps whatever the caller passes.
    internal ItemsControl.ItemInfo NewItemInfo(object? item, DependencyObject? container = null, int index = -1)
        => new(item, container, index);

    // WPF inherits this from ItemsControl, where the container is resolved
    // from the item container generator.
    internal ItemsControl.ItemInfo? ItemInfoFromIndex(int index)
        => index >= 0 && index < Items.Count
            ? new ItemsControl.ItemInfo(Items[index], null, index)
            : null;

    // Subset of the WPF handler: no selection-unit validation or pending-change
    // coalescing until the selector spine exists; just notify listeners.
    internal void OnSelectedCellsChanged(
        NotifyCollectionChangedAction action,
        VirtualizedCellInfoCollection oldItems,
        VirtualizedCellInfoCollection newItems)
        => SelectedCellsChanged?.Invoke(this, new SelectedCellsChangedEventArgs(this, newItems, oldItems));

    internal DataGridColumn ColumnFromDisplayIndex(int displayIndex)
        => InternalColumns.ColumnFromDisplayIndex(displayIndex);

    internal int ColumnIndexFromDisplayIndex(int displayIndex)
        => InternalColumns.ColumnIndexFromDisplayIndex(displayIndex);

    internal void NotifyPropertyChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args,
        DataGridNotificationTarget target)
        => NotifyPropertyChanged(dependencyObject, string.Empty, args, target);

    internal void NotifyPropertyChanged(
        DependencyObject dependencyObject,
        string propertyName,
        DependencyPropertyChangedEventArgs args,
        DataGridNotificationTarget target)
    {
        if ((target & (DataGridNotificationTarget.Columns | DataGridNotificationTarget.ColumnCollection)) != 0)
        {
            InternalColumns.NotifyPropertyChanged(dependencyObject, propertyName, args, target);
        }
    }

    private void OnColumnsChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
    }
}
