using System.Windows.Controls;

namespace System.Windows.Controls.Primitives;

// DataGridCellsPresenter: minimal shell. The real implementation virtualizes
// cells inside a row; this stub exists so the control root and the row shim
// compile. ContainerTracking wiring comes with the cell-generation milestone.
public partial class DataGridCellsPresenter : Panel
{
    internal ContainerTracking<DataGridCell>? CellTrackingRoot { get; set; }

    internal DataGridRow? DataGridRowOwner { get; set; }

    internal DataGrid? DataGridOwner { get; set; }

    internal object? Item { get; set; }

    internal ItemContainerGenerator ItemContainerGenerator { get; } = new();

    internal void InvalidateDataGridCellsPanelMeasureAndArrange() { }
    internal void InvalidateDataGridCellsPanelMeasureAndArrange(bool withColumnVirtualization) { }

    internal void NotifyPropertyChanged(
        DependencyObject d,
        string propertyName,
        DependencyPropertyChangedEventArgs e,
        DataGridNotificationTarget target)
    {
        DataGridRowOwner?.ShimNotifyCells(d, propertyName, e, target);
    }

    internal DataGridCell? TryGetCell(int displayIndex) => null;

    internal void SyncProperties(bool forcePrepareCells) { }

    internal void OnColumnsChanged(
        System.Collections.ObjectModel.ObservableCollection<DataGridColumn> columns,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        => DataGridRowOwner?.BuildCells();

    internal void ScrollCellIntoView(int index) { }
}

// DataGridRowsPresenter: minimal shell used in the static ctor template.
public partial class DataGridRowsPresenter : Panel
{
}


