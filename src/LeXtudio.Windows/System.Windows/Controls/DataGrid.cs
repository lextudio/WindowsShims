using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace System.Windows.Controls;

public partial class DataGrid : Control
{
    private readonly DataGridColumnCollection _columns;

    public DataGrid()
    {
        _columns = new DataGridColumnCollection(this);
        _columns.CollectionChanged += OnColumnsChanged;
    }

    public ObservableCollection<DataGridColumn> Columns => _columns;

    public DataGridGridLinesVisibility GridLinesVisibility { get; set; } = DataGridGridLinesVisibility.All;

    internal DataGridColumnCollection InternalColumns => _columns;

    // WPF inherits NewItemInfo from ItemsControl, which also resolves the
    // container/index from the item container generator. The shell has no
    // generator yet, so the info keeps whatever the caller passes.
    internal ItemsControl.ItemInfo NewItemInfo(object? item, DependencyObject? container = null, int index = -1)
        => new(item, container, index);

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
