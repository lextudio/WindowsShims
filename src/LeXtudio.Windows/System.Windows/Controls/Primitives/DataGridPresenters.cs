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

// DataGridDetailsPresenter: expands below a row to show details template.
public partial class DataGridDetailsPresenter : ContentControl
{
    internal DataGrid? ParentDataGrid { get; set; }
    internal FrameworkElement? DetailsElement => Content as FrameworkElement;

    internal void NotifyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

    internal void SyncProperties() { }
}

// DataGridRowHeader: left-edge row-state glyph. Stubs out notification and
// editing state until the presenter milestone.
public partial class DataGridRowHeader : ContentControl
{
    internal DataGrid? ParentDataGrid { get; set; }
    internal DataGridRow? ParentRow { get; set; }

    public bool HasShimGridLine { get; private set; }

    internal void NotifyPropertyChanged(
        DependencyObject d,
        string propertyName,
        DependencyPropertyChangedEventArgs e,
        DataGridNotificationTarget target)
    {
    }

    internal void NotifyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
    }

    internal void SyncProperties() { }

    internal void ApplyShimGridLines()
    {
        var owner = ParentDataGrid ?? ParentRow?.DataGridOwner;
        var visibility = owner?.GridLinesVisibility ?? DataGridGridLinesVisibility.None;
        var horizontal = visibility is DataGridGridLinesVisibility.All or DataGridGridLinesVisibility.Horizontal;
        var vertical = visibility is DataGridGridLinesVisibility.All or DataGridGridLinesVisibility.Vertical;

        HasShimGridLine = horizontal || vertical;
        BorderThickness = HasShimGridLine
            ? new Microsoft.UI.Xaml.Thickness(0, 0, vertical ? 1 : 0, horizontal ? 1 : 0)
            : new Microsoft.UI.Xaml.Thickness(0);
        BorderBrush = HasShimGridLine
            ? (vertical ? owner?.VerticalGridLinesBrush : owner?.HorizontalGridLinesBrush)
            : null;
    }
}
