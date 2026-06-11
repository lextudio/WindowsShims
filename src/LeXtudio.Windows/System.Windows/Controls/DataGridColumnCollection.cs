using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace System.Windows.Controls;

internal sealed class DataGridColumnCollection : ObservableCollection<DataGridColumn>
{
    private readonly DataGrid _dataGridOwner;

    internal DataGridColumnCollection(DataGrid dataGridOwner)
    {
        _dataGridOwner = dataGridOwner ?? throw new ArgumentNullException(nameof(dataGridOwner));
    }

    internal DataGrid DataGridOwner => _dataGridOwner;

    internal DataGridColumn ColumnFromDisplayIndex(int displayIndex)
    {
        if (displayIndex < 0 || displayIndex >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(displayIndex));
        }

        for (var i = 0; i < Count; i++)
        {
            if (this[i].DisplayIndex == displayIndex)
            {
                return this[i];
            }
        }

        return this[displayIndex];
    }

    internal int ColumnIndexFromDisplayIndex(int displayIndex)
    {
        if (displayIndex < 0 || displayIndex >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(displayIndex));
        }

        for (var i = 0; i < Count; i++)
        {
            if (this[i].DisplayIndex == displayIndex)
            {
                return i;
            }
        }

        return displayIndex;
    }

    protected override void InsertItem(int index, DataGridColumn item)
    {
        ValidateNewColumn(item);
        base.InsertItem(index, item);
        AttachColumn(item, index);
        NormalizeDisplayIndexes();
    }

    protected override void SetItem(int index, DataGridColumn item)
    {
        ValidateNewColumn(item, this[index]);
        var oldColumn = this[index];
        DetachColumn(oldColumn);
        base.SetItem(index, item);
        AttachColumn(item, index);
        NormalizeDisplayIndexes();
    }

    protected override void RemoveItem(int index)
    {
        var oldColumn = this[index];
        DetachColumn(oldColumn);
        base.RemoveItem(index);
        NormalizeDisplayIndexes();
    }

    protected override void ClearItems()
    {
        foreach (var column in this)
        {
            DetachColumn(column);
        }

        base.ClearItems();
    }

    internal void NotifyPropertyChanged(
        DependencyObject dependencyObject,
        string propertyName,
        DependencyPropertyChangedEventArgs args,
        DataGridNotificationTarget target)
    {
        if ((target & DataGridNotificationTarget.Columns) == 0)
        {
            return;
        }

        foreach (var column in this)
        {
            column.NotifyPropertyChanged(dependencyObject, args, DataGridNotificationTarget.Columns);
        }
    }

    private void ValidateNewColumn(DataGridColumn item, DataGridColumn? currentItem = null)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.DataGridOwner is not null && !ReferenceEquals(item.DataGridOwner, _dataGridOwner))
        {
            throw new ArgumentException(SR.Format(SR.DataGrid_InvalidColumnReuse, item.Header), nameof(item));
        }

        if (item.DataGridOwner is not null && !ReferenceEquals(item, currentItem))
        {
            throw new ArgumentException(SR.Format(SR.DataGrid_InvalidColumnReuse, item.Header), nameof(item));
        }
    }

    private void AttachColumn(DataGridColumn column, int index)
    {
        column.DataGridOwner = _dataGridOwner;
        if (column.DisplayIndex < 0)
        {
            column.DisplayIndex = index;
        }
    }

    private static void DetachColumn(DataGridColumn column)
    {
        column.DataGridOwner = null;
        column.DisplayIndex = -1;
    }

    private void NormalizeDisplayIndexes()
    {
        for (var i = 0; i < Count; i++)
        {
            if (this[i].DisplayIndex < 0 || this[i].DisplayIndex >= Count)
            {
                this[i].DisplayIndex = i;
            }
        }
    }
}
