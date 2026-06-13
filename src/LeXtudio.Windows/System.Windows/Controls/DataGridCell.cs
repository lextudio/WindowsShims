using System.Windows.Data;

namespace System.Windows.Controls;

public partial class DataGridCell : ContentControl, IProvideDataGridColumn
{
    public bool IsEditing { get; set; }

    public bool IsSelected { get; set; }

    public bool IsReadOnly { get; set; }

    public DataGridColumn? Column { get; set; }

    internal DataGridRow? RowOwner { get; set; }

    internal DataGrid? DataGridOwner => RowOwner?.DataGridOwner ?? Column?.DataGridOwner;

    internal object? RowDataItem => RowOwner?.Item;

    internal FrameworkElement? EditingElement { get; set; }

    // WPF UIElement.Focus() has no FocusState; route to programmatic focus.
    public bool Focus() => Focus(Microsoft.UI.Xaml.FocusState.Programmatic);

    // WPF UIElement.MoveFocus; routes to keyboard navigation.
    public bool MoveFocus(Input.TraversalRequest request) => false;

    public bool IsVisible => Visibility == Visibility.Visible;

    // Populate the cell's content from its column, binding against the row
    // item. The generated element (e.g. a bound TextBlock for a text column)
    // inherits DataContext from this cell, so its WinUI binding resolves.
    internal void BuildVisualTree()
    {
        if (Column is null)
        {
            return;
        }

        var item = RowOwner?.Item ?? DataContext;
        if (item is null)
        {
            return;
        }

        DataContext = item;
        Content = Column.BuildCellContent(this, item);
    }

    internal bool BeginEdit(RoutedEventArgs? editingEventArgs) => false;

    internal void CancelEdit() { }

    internal bool CommitEdit() => true;

    internal void SyncIsSelected(bool isSelected) => IsSelected = isSelected;

    internal void NotifyCurrentCellContainerChanged(DataGridCell? oldCell = null, DataGridCellInfo currentCell = default) { }
}
