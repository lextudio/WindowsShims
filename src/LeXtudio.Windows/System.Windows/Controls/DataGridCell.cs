using System.Windows.Data;

namespace System.Windows.Controls;

public partial class DataGridCell : ContentControl, IProvideDataGridColumn
{
    // ── Backing fields for upstream properties ────────────────────────────────

    // Assigned in the upstream instance ctor; read by upstream Tracker property (guarded).
    private ContainerTracking<DataGridCell> _tracker;

    // Used by upstream SyncIsSelected to suppress CellIsSelectedChanged notifications.
    private bool _syncingIsSelected;

    // ── Local-only properties ─────────────────────────────────────────────────

    public bool IsReadOnly { get; set; }

    public bool IsFrozen { get; private set; }

    public bool HasShimGridLine { get; private set; }

    public Style? ShimAppliedCellStyle { get; private set; }

    internal DataGridRow? RowOwner { get; set; }

    internal DataGrid? DataGridOwner => RowOwner?.DataGridOwner ?? Column?.DataGridOwner;

    internal object? RowDataItem => RowOwner?.Item;

    internal FrameworkElement? EditingElement { get; set; }

    // Populate the cell's content from its column, binding against the row
    // item. The generated element (e.g. a bound TextBlock for a text column)
    // inherits DataContext from this cell, so its WinUI binding resolves.
    internal void BuildVisualTree()
    {
        if (Column is null || IsEditing)
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
        if (IsEditing || IsReadOnly
            || Column is not DataGridBoundColumn bound
            || bound.BindingPath is not { Length: > 0 } path
            || RowDataItem is not { } item)
        {
            return false;
        }

        // Read-only is coerced from the grid and the column.
        if (DataGridOwner?.IsCellEffectivelyReadOnly(Column) == true)
        {
            return false;
        }

        if (DataGridOwner is { ShimExecutingBeginEditCommand: false } owner)
        {
            owner.CurrentCellContainer = this;
            if (ReferenceEquals(RowDataItem, System.Windows.Data.CollectionView.NewItemPlaceholder)
                || ReferenceEquals(RowDataItem, DataGrid.NewItemPlaceholder))
            {
                return owner.ShimBeginEditPlaceholder(this, editingEventArgs);
            }

            if (owner.BeginEdit(editingEventArgs))
            {
                return true;
            }

            return false;
        }

        var current = item.GetType().GetProperty(path)?.GetValue(item);
        _editingBox = new Microsoft.UI.Xaml.Controls.TextBox { Text = current?.ToString() ?? string.Empty };
        _editingBox.LostFocus += OnEditingBoxLostFocus; // commit-on-blur
        EditingElement = _editingBox;
        Content = _editingBox;
        IsEditing = true;
        _editingBox.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        _editingBox.SelectAll();
        return true;
    }

    private void OnEditingBoxLostFocus(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => CommitEdit();

    internal void CancelEdit()
    {
        if (!IsEditing)
        {
            return;
        }

        if (DataGridOwner is { ShimExecutingCancelEditCommand: false } owner)
        {
            owner.CurrentCellContainer = this;
            owner.CancelEdit(DataGridEditingUnit.Row);
            return;
        }

        ClearValidationError();
        EndEdit();
    }

    internal bool CommitEdit()
    {
        if (!IsEditing)
        {
            return true;
        }

        if (DataGridOwner is { ShimExecutingCommitEditCommand: false } owner)
        {
            owner.CurrentCellContainer = this;
            return owner.CommitEdit(DataGridEditingUnit.Row, true);
        }

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
                    return false; // invalid input — keep editing
                }

                // Business-rule validation via IDataErrorInfo (session 46):
                // value is written, then validated; on error stay in edit mode.
                if (item is System.ComponentModel.IDataErrorInfo dataError)
                {
                    var error = dataError[path];
                    if (!string.IsNullOrEmpty(error))
                    {
                        SetValidationError(error);
                        return false;
                    }
                }
            }
        }

        ClearValidationError();
        EndEdit();
        return true;
    }

    // ── Session 46: validation surface ───────────────────────────────────────
    internal bool HasValidationError { get; private set; }

    internal string? ValidationError { get; private set; }

    private void SetValidationError(string error)
    {
        HasValidationError = true;
        ValidationError = error;
        BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            global::Windows.UI.Color.FromArgb(0xFF, 0xCC, 0x00, 0x00));
        BorderThickness = new Microsoft.UI.Xaml.Thickness(1);
        Microsoft.UI.Xaml.Controls.ToolTipService.SetToolTip(this, error);
    }

    private void ClearValidationError()
    {
        if (!HasValidationError)
        {
            return;
        }

        HasValidationError = false;
        ValidationError = null;
        BorderBrush = null;
        BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
        Microsoft.UI.Xaml.Controls.ToolTipService.SetToolTip(this, null);
    }

    private void EndEdit()
    {
        if (_editingBox is { } box)
        {
            box.LostFocus -= OnEditingBoxLostFocus;
        }

        IsEditing = false;
        EditingElement = null;
        _editingBox = null;
        BuildVisualTree();
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

    // Called by the upstream DataGrid.CurrentCellContainer setter whenever the
    // current cell changes; add/remove a focus-border so the current cell is
    // visually distinct from merely selected cells.
    internal void NotifyCurrentCellContainerChanged(DataGridCell? oldCell = null, DataGridCellInfo currentCell = default)
    {
        var isCurrent = DataGridOwner?.CurrentCellContainer == this;
        if (isCurrent)
        {
            // WinUI system accent — blue focus ring distinguishes current from selected.
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                global::Windows.UI.Color.FromArgb(0xFF, 0x00, 0x78, 0xD4));
            BorderThickness = new Microsoft.UI.Xaml.Thickness(2);
        }
        else if (!HasValidationError)
        {
            ApplyShimGridLines();
        }
    }

    // Called from DataGridRow.NotifyPropertyChanged when the upstream DataGrid
    // notification chain forwards a property change down to cells. Handles the
    // subset meaningful in the shim render path:
    //   • WidthProperty  — update cell width to match the new column width
    //   • IsReadOnlyProperty — re-evaluate effective read-only from grid + column
    //   • RefreshCellContent — rebuild visual content (template column change etc.)
    internal void NotifyPropertyChanged(
        DependencyObject dependencyObject,
        string propertyName,
        DependencyPropertyChangedEventArgs args,
        DataGridNotificationTarget target)
    {
        // Skip notifications from a different column — not ours.
        if (dependencyObject is DataGridColumn col && !ReferenceEquals(col, Column))
            return;

        if (DataGridHelper.ShouldNotifyCells(target))
        {
            if (args.Property == DataGridColumn.WidthProperty)
            {
                Width = DataGridOwner?.ShimColumnWidth(Column) ?? double.NaN;
            }
            else if (args.Property == DataGrid.IsReadOnlyProperty
                     || args.Property == DataGridColumn.IsReadOnlyProperty)
            {
                IsReadOnly = DataGridOwner?.IsCellEffectivelyReadOnly(Column) ?? false;
            }
            else if (args.Property == DataGrid.FrozenColumnCountProperty
                     || args.Property == DataGridColumn.IsFrozenProperty)
            {
                ApplyShimFrozenState();
            }
            else if (args.Property == DataGrid.CellStyleProperty)
            {
                ApplyShimCellStyle();
            }
            else if (args.Property == DataGridColumn.CellStyleProperty)
            {
                ApplyShimCellStyle();
            }
        }

        if (DataGridHelper.ShouldNotifyCellsPresenter(target)
            && args.Property == DataGrid.FrozenColumnCountProperty)
        {
            ApplyShimFrozenState();
        }

        if (DataGridHelper.ShouldRefreshCellContent(target))
        {
            BuildVisualTree();
        }
    }

    internal void ApplyShimFrozenState()
    {
        IsFrozen = Column is { DataGridOwner: { } owner } column
            ? column.DisplayIndex < owner.FrozenColumnCount
            : Column?.IsFrozen == true;
        Opacity = IsFrozen ? 0.96 : 1.0;
    }

    internal void ApplyShimCellStyle()
    {
        ShimAppliedCellStyle = Column?.CellStyle ?? DataGridOwner?.CellStyle;
    }

    internal void ApplyShimGridLines()
    {
        if (HasValidationError || DataGridOwner?.CurrentCellContainer == this)
        {
            return;
        }

        var owner = DataGridOwner;
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
