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

    internal void InvalidateDataGridCellsPanelMeasureAndArrange() { }
    internal void InvalidateDataGridCellsPanelMeasureAndArrange(bool withColumnVirtualization) { }

    internal void NotifyPropertyChanged(
        DependencyObject d,
        string propertyName,
        DependencyPropertyChangedEventArgs e,
        DataGridNotificationTarget target)
    {
    }

    internal DataGridCell? TryGetCell(int displayIndex) => null;
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
}

// DataGridRowHeader: left-edge row-state glyph. Stubs out notification and
// editing state until the presenter milestone.
public partial class DataGridRowHeader : ContentControl
{
    internal DataGrid? ParentDataGrid { get; set; }
    internal DataGridRow? ParentRow { get; set; }

    internal void NotifyPropertyChanged(
        DependencyObject d,
        string propertyName,
        DependencyPropertyChangedEventArgs e,
        DataGridNotificationTarget target)
    {
    }
}
