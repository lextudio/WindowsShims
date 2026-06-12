using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Controls.Primitives;

namespace System.Windows.Controls;

// Rebased onto the linked MultiSelector spine in session 17: Items, item-info
// helpers, and the selection surface now come from Selector/ItemsControl.
public partial class DataGrid : MultiSelector
{
    private readonly DataGridColumnCollection _columns;
    private SelectedCellsCollection? _selectedCells;

    public DataGrid()
    {
        _columns = new DataGridColumnCollection(this);
        _columns.CollectionChanged += OnColumnsChanged;
    }

    public ObservableCollection<DataGridColumn> Columns => _columns;

    public IList<DataGridCellInfo> SelectedCells => _selectedCells ??= new SelectedCellsCollection(this);

    public event SelectedCellsChangedEventHandler? SelectedCellsChanged;

    public DataGridGridLinesVisibility GridLinesVisibility { get; set; } = DataGridGridLinesVisibility.All;

    internal DataGridColumnCollection InternalColumns => _columns;

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
