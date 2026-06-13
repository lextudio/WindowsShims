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

    private Microsoft.UI.Xaml.Controls.TextBox? _editingBox;

    // Session 39: minimal text-cell editing. BeginEdit swaps the display
    // element for a TextBox seeded with the current value; CommitEdit writes
    // it back to the item property (reflection + type conversion); CancelEdit
    // restores without writing.
    internal bool BeginEdit(RoutedEventArgs? editingEventArgs)
    {
        if (IsEditing || IsReadOnly || Column is not DataGridBoundColumn bound
            || bound.BindingPath is not { Length: > 0 } path
            || RowDataItem is not { } item)
        {
            return false;
        }

        var current = item.GetType().GetProperty(path)?.GetValue(item);
        _editingBox = new Microsoft.UI.Xaml.Controls.TextBox { Text = current?.ToString() ?? string.Empty };
        Content = _editingBox;
        IsEditing = true;
        _editingBox.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        _editingBox.SelectAll();
        return true;
    }

    internal void CancelEdit()
    {
        if (!IsEditing)
        {
            return;
        }

        IsEditing = false;
        _editingBox = null;
        BuildVisualTree();
    }

    internal bool CommitEdit()
    {
        if (!IsEditing)
        {
            return true;
        }

        var ok = true;
        if (_editingBox is { } box && Column is DataGridBoundColumn { BindingPath: { Length: > 0 } path }
            && RowDataItem is { } item)
        {
            var prop = item.GetType().GetProperty(path);
            if (prop is { CanWrite: true })
            {
                try
                {
                    var target = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    var value = target == typeof(string)
                        ? box.Text
                        : Convert.ChangeType(box.Text, target, System.Globalization.CultureInfo.CurrentCulture);
                    prop.SetValue(item, value);
                }
                catch (Exception)
                {
                    ok = false; // invalid input — keep editing
                }
            }
        }

        if (!ok)
        {
            return false;
        }

        IsEditing = false;
        _editingBox = null;
        BuildVisualTree();
        return true;
    }

    // Input: double-tap begins editing; Enter commits, Escape cancels.
    protected override void OnDoubleTapped(Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        base.OnDoubleTapped(e);
        if (BeginEdit(null))
        {
            e.Handled = true;
        }
    }

    protected override void OnKeyDown(Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (IsEditing)
        {
            switch (e.Key)
            {
                case global::Windows.System.VirtualKey.Enter:
                    CommitEdit();
                    e.Handled = true;
                    return;
                case global::Windows.System.VirtualKey.Escape:
                    CancelEdit();
                    e.Handled = true;
                    return;
            }
        }
        else if (e.Key == global::Windows.System.VirtualKey.F2)
        {
            if (BeginEdit(null))
            {
                e.Handled = true;
            }

            return;
        }

        base.OnKeyDown(e);
    }

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
