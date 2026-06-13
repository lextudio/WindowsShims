using System.Windows.Data;

namespace System.Windows.Controls;

public partial class DataGridCell : ContentControl, IProvideDataGridColumn
{
    public bool IsEditing { get; set; }

    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            // Cell highlight (slightly stronger than the row tint) for
            // cell-level selection; transparent when not cell-selected.
            Background = _isSelected
                ? new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    global::Windows.UI.Color.FromArgb(0xFF, 0x9C, 0xC9, 0xF5))
                : null;
        }
    }

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

    // Pointer press routes to the owner; the grid applies SelectionUnit
    // (row vs cell). Marking handled stops the parent row from also selecting.
    protected override void OnPointerPressed(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (DataGridOwner is { } owner)
        {
            owner.HandleShimCellClicked(this);
            e.Handled = true;
        }
    }

    internal void NotifyCurrentCellContainerChanged(DataGridCell? oldCell = null, DataGridCellInfo currentCell = default) { }
}
