using System.Windows.Controls.Primitives;

namespace System.Windows.Controls;

// Uno-specific additions to the WPF DataGrid control root. Only members that
// do NOT appear in the linked upstream DataGrid.cs should live here. The
// upstream file is compiled as a partial on HAS_UNO so both parts merge.
public partial class DataGrid
{
    // Session 52: when the linked WPF CommitEdit command calls back into the
    // current cell, let the cell run only its local value-write/validation/end
    // logic instead of re-entering DataGrid.CommitEdit().
    internal bool ShimExecutingCommitEditCommand { get; set; }

    // Same idea for the linked WPF CancelEdit command path.
    internal bool ShimExecutingCancelEditCommand { get; set; }

    // Same idea for the linked WPF BeginEdit command path.
    internal bool ShimExecutingBeginEditCommand { get; set; }

    internal bool ShimHandlingPlaceholderBeginEdit { get; set; }

    internal bool ShimValidateRowCommit(DataGridRow? row)
    {
        if (row is null)
        {
            return true;
        }

        foreach (var rule in RowValidationRules)
        {
            var result = rule.Validate(row.Item, System.Globalization.CultureInfo.CurrentCulture);
            if (!result.IsValid)
            {
                row.SetRowError(result.ErrorContent?.ToString());
                return false;
            }
        }

        row.ClearRowError();
        return true;
    }

    // UpdateVisualState: the upstream calls this (0-arg) which calls the
    // virtual ChangeVisualState. Provide the 0-arg overload in the shim part.
    internal void UpdateVisualState() => ChangeVisualState(true);

    // The shim render path is code-built rather than presenter-driven, so
    // option changes like CanUserAddRows need an explicit rebuild before the
    // normal layout pass if the visual tree is already realized.
    public new void UpdateLayout()
    {
        BuildShimVisualTree();
        base.UpdateLayout();
    }

    internal bool ShimBeginEditPlaceholder(DataGridCell placeholderCell, RoutedEventArgs? editingEventArgs)
    {
        if (ShimHandlingPlaceholderBeginEdit)
        {
            return false;
        }

        ShimHandlingPlaceholderBeginEdit = true;
        try
        {
            var placeholderColumn = placeholderCell.Column ?? Columns.Cast<DataGridColumn>().FirstOrDefault(column => column.IsVisible);
            if (placeholderColumn is null)
            {
                return false;
            }

            var newItem = Items.CurrentAddItem ?? AddNewItem();
            SetCurrentCellToNewItem(newItem, placeholderColumn);
            UpdateLayout();

            DataGridCell? newCell = null;
            var row =
                ItemContainerGenerator.ContainerFromItem(newItem) as DataGridRow ??
                FindShimRowForItem(newItem);

            row?.UpdateLayout();

            if (row is not null && placeholderCell.Column is { } column)
            {
                for (var i = 0; i < Columns.Count; i++)
                {
                    if (ReferenceEquals(Columns[i], column))
                    {
                        newCell = row.TryGetCell(i);
                        break;
                    }
                }
            }

            newCell ??= CurrentCellContainer;
            if (newCell is not null && !ReferenceEquals(newCell, placeholderCell))
            {
                CurrentCellContainer = newCell;
                var wasExecuting = ShimExecutingBeginEditCommand;
                ShimExecutingBeginEditCommand = true;
                try
                {
                    return newCell.BeginEdit(editingEventArgs);
                }
                finally
                {
                    ShimExecutingBeginEditCommand = wasExecuting;
                }
            }

            return false;
        }
        finally
        {
            ShimHandlingPlaceholderBeginEdit = false;
        }
    }

    private void SetCurrentCellToNewItem(object newItem, DataGridColumn fallbackColumn)
    {
        var column = CurrentCell.Column ?? fallbackColumn;
        var index = Items.IndexOf(newItem);
        var info = ItemInfoFromIndex(index);
        CurrentCell = info is not null
            ? new DataGridCellInfo(info, column, this)
            : new DataGridCellInfo(newItem, column, this);
    }

    private DataGridRow? FindShimRowForItem(object item)
    {
        if (GetTemplateChild("PART_ShimRowsHost") is not Microsoft.UI.Xaml.Controls.Panel host)
        {
            return null;
        }

        foreach (var child in host.Children)
        {
            if (child is DataGridRow row && ReferenceEquals(row.Item, item))
            {
                return row;
            }
        }

        return null;
    }

    // The WPF static-ctor OverrideMetadata(typeof(DataGrid)) call is a no-op
    // under the shim, and the library's Themes/Generic.xaml is not reliably in
    // the consumer's ms-appx resource map. So the shim assigns a minimal
    // ControlTemplate directly (built via XamlReader) — self-contained, no
    // dependency on default-style probing. The template root hosts
    // PART_ShimRowsHost, which the code render path populates.
    private const string ShimTemplateXaml =
        "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
        "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
        "<Border Background='White' BorderBrush='#CCCCCC' BorderThickness='1'>" +
        "<ScrollViewer HorizontalScrollBarVisibility='Auto' VerticalScrollBarVisibility='Auto'>" +
        "<StackPanel x:Name='PART_ShimRowsHost' MinWidth='120' MinHeight='40' />" +
        "</ScrollViewer></Border></ControlTemplate>";

    private void EnsureShimStyleKey()
    {
        if (Template is not null)
        {
            return;
        }

        try
        {
            Template = (Microsoft.UI.Xaml.Controls.ControlTemplate)
                Microsoft.UI.Xaml.Markup.XamlReader.Load(ShimTemplateXaml);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DataGrid] shim template load failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    // Session 25/26 — shim render path. Populates PART_ShimRowsHost with a
    // header row plus one DataGridRow per item. Each DataGridRow hosts its own
    // cells (session 26), so the on-screen tree matches the WPF row/cell APIs.
    // Still intentionally simple: no virtualization, no ItemContainerGenerator
    // containers, separate from the upstream PART_RowsPresenter machinery.
    internal void BuildShimVisualTree()
    {
        if (GetTemplateChild("PART_ShimRowsHost") is not Microsoft.UI.Xaml.Controls.Panel host)
        {
            return;
        }

        var editingItem = CurrentCellContainer is { IsEditing: true } editingCell
            ? editingCell.RowDataItem
            : null;
        var editingColumn = CurrentCellContainer is { IsEditing: true } ? CurrentCellContainer.Column : null;

        EnsureShimNewItemPlaceholderState();
        HookShimChangeNotifications();

        InternalColumns.RefreshDisplayIndexMap();
        _visibleColumns = ColumnsInDisplayOrder().Where(c => c.IsVisible).ToList();

        host.Children.Clear();
        ItemContainerGenerator.ResetContainers();
        // Reset the tracker chain before rebuilding so the upstream
        // DataGrid.NotifyPropertyChanged → _rowTrackingRoot iteration always
        // reflects the live set of realized rows. (Partial class shares the
        // private _rowTrackingRoot field with the upstream DataGrid.cs.)
        _rowTrackingRoot = null;
        host.Children.Add(BuildHeaderRow());

        var rowIndex = 0;
        foreach (var item in OrderedItems())
        {
            if (item is null)
            {
                continue;
            }

            var row = new DataGridRow();
            row.PrepareRow(item, this); // also initializes row.Tracker
            row.ShimRowIndex = rowIndex++;
            row.ApplyShimRowStyle();
            row.ApplyShimRowBackground();
            row.Tracker!.StartTracking(ref _rowTrackingRoot);
            // Re-apply selection across rebuilds (sort / reactivity) from the
            // real engine's SelectedItems, so the highlight follows the item(s).
            if (IsRowItemSelected(item))
            {
                row.IsSelected = true;
            }

            // Register the row so the linked WPF code can resolve containers
            // (selection, scroll-into-view, row details) via the generator.
            ItemContainerGenerator.RegisterContainer(item, row);
            host.Children.Add(row);
        }

        PruneRealRowSelection();
        PruneRealCellSelection();

        ItemContainerGenerator.NotifyContainersGenerated();

        if (editingItem is not null && editingColumn is not null)
        {
            RestoreEditingCellAfterRebuild(editingItem, editingColumn);
        }

        // Schedule an Auto-width pass if any visible column is non-absolute.
        if (_visibleColumns.Any(IsAutoWidth))
        {
            _autoWidthPending = true;
            if (!_autoWidthHooked)
            {
                LayoutUpdated += OnAutoWidthLayoutUpdated;
                _autoWidthHooked = true;
            }
        }
    }

    private void RestoreEditingCellAfterRebuild(object editingItem, DataGridColumn editingColumn)
    {
        if (ItemContainerGenerator.ContainerFromItem(editingItem) is not DataGridRow row)
        {
            return;
        }

        // Template may not be applied yet (OnApplyTemplate fires after the layout pass).
        // Force it now so TryGetCell can access the realized cells.
        row.ApplyTemplate();

        for (var i = 0; i < Columns.Count; i++)
        {
            if (!ReferenceEquals(Columns[i], editingColumn))
            {
                continue;
            }

            if (row.TryGetCell(i) is not { IsEditing: false } cell)
            {
                return;
            }

            CurrentCellContainer = cell;
            var wasExecuting = ShimExecutingBeginEditCommand;
            ShimExecutingBeginEditCommand = true;
            try
            {
                cell.BeginEdit(null);
            }
            finally
            {
                ShimExecutingBeginEditCommand = wasExecuting;
            }

            return;
        }
    }

    private void EnsureShimNewItemPlaceholderState()
    {
        if (Items.IsAddingNew)
        {
            return;
        }

        if (CanUserAddRows)
        {
            if (Items.NewItemPlaceholderPosition == System.ComponentModel.NewItemPlaceholderPosition.None)
            {
                Items.NewItemPlaceholderPosition = System.ComponentModel.NewItemPlaceholderPosition.AtEnd;
            }
        }
        else if (Items.NewItemPlaceholderPosition != System.ComponentModel.NewItemPlaceholderPosition.None)
        {
            Items.NewItemPlaceholderPosition = System.ComponentModel.NewItemPlaceholderPosition.None;
        }
    }

    // ── Session 41: Auto column width ────────────────────────────────────────
    // Absolute widths are honored directly (ShimColumnWidth). For Auto/Star/
    // SizeToCells/SizeToHeader, cells/headers are left auto-sized, then a
    // one-shot post-layout pass sets a uniform per-column width to the widest
    // realized content so columns size to content and stay aligned.
    private List<DataGridColumn> _visibleColumns = new();
    private readonly List<DataGridColumnHeader> _headerCells = new();
    private bool _autoWidthPending;
    private bool _autoWidthHooked;

    // Non-absolute columns (Auto/SizeTo*/Star) need the post-layout pass.
    private static bool IsAutoWidth(DataGridColumn column)
        => column.ActualWidth <= 0 && !column.Width.IsAbsolute;

    private static double Clamp(DataGridColumn column, double width)
        => Math.Clamp(width, column.MinWidth, column.MaxWidth);

    private void OnAutoWidthLayoutUpdated(object? sender, object e)
    {
        if (!_autoWidthPending)
        {
            return;
        }

        _autoWidthPending = false;

        var rows = ItemContainerGenerator.Containers.OfType<DataGridRow>().ToList();
        var widths = new double[_visibleColumns.Count];
        var starWeights = new double[_visibleColumns.Count];
        var fixedTotal = 0.0;
        var totalStar = 0.0;

        // Pass 1: fixed (absolute) + auto (measured) widths, clamped.
        for (var i = 0; i < _visibleColumns.Count && i < _headerCells.Count; i++)
        {
            var column = _visibleColumns[i];
            if (column.Width.IsStar)
            {
                starWeights[i] = column.Width.Value > 0 ? column.Width.Value : 1;
                totalStar += starWeights[i];
                continue;
            }

            double w;
            if (!IsAutoWidth(column))
            {
                w = column.ActualWidth > 0 ? column.ActualWidth : column.Width.Value;
            }
            else
            {
                w = _headerCells[i].DesiredSize.Width;
                foreach (var row in rows)
                {
                    if (row.TryGetCell(i) is { } cell)
                    {
                        w = Math.Max(w, cell.DesiredSize.Width);
                    }
                }
            }

            widths[i] = Clamp(column, w);
            fixedTotal += widths[i];
        }

        // Pass 2: distribute remaining viewport width among star columns.
        if (totalStar > 0)
        {
            var available = ActualWidth - 2; // border chrome
            var remaining = Math.Max(0, available - fixedTotal);
            for (var i = 0; i < _visibleColumns.Count; i++)
            {
                if (starWeights[i] > 0)
                {
                    widths[i] = Clamp(_visibleColumns[i], remaining * (starWeights[i] / totalStar));
                }
            }
        }

        // Apply.
        for (var i = 0; i < _visibleColumns.Count && i < _headerCells.Count; i++)
        {
            if (widths[i] <= 0)
            {
                continue;
            }

            _headerCells[i].Width = widths[i];
            foreach (var row in rows)
            {
                if (row.TryGetCell(i) is { } cell)
                {
                    cell.Width = widths[i];
                }
            }
        }
    }

    // Items in display order: sorted by the active sort column if one is set,
    // otherwise the collection's own order.
    // Display order is now the collection-view order: sorting is applied by
    // ItemCollection.SortDescriptions (driven by the WPF PerformSort path),
    // so Items already enumerates in sorted order. (Session 50 reuse.)
    private IEnumerable<object?> OrderedItems() => Items.Cast<object?>().ToList();

    private Microsoft.UI.Xaml.Controls.StackPanel BuildHeaderRow()
    {
        var header = new Microsoft.UI.Xaml.Controls.StackPanel
        {
            Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal,
        };

        // Top-left corner placeholder when row headers are visible, so column
        // headers line up with the row-header-indented cells.
        if (AreRowHeadersVisible)
        {
            header.Children.Add(new Microsoft.UI.Xaml.Controls.Border { Width = RowHeaderShimWidth });
        }

        _headerCells.Clear();
        foreach (var column in _visibleColumns)
        {
            var headerCell = new DataGridColumnHeader
            {
                Content = HeaderContent(column),
                Width = ShimColumnWidth(column),
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Microsoft.UI.Xaml.Thickness(4, 2, 4, 2),
            };
            headerCell.PrepareColumnHeader(column.Header, column);
            headerCell.Content = HeaderContent(column);
            headerCell.ApplyShimFrozenState();
            headerCell.ApplyShimColumnHeaderStyle();
            headerCell.ApplyShimGridLines();
            _headerCells.Add(headerCell);
            header.Children.Add(headerCell);
        }

        return header;
    }

    // Header content with a sort-direction glyph when this column is the
    // active sort.
    internal object? HeaderContent(DataGridColumn column)
    {
        if (column.SortDirection is { } dir)
        {
            var glyph = dir == System.ComponentModel.ListSortDirection.Ascending ? " ▲" : " ▼";
            return (column.Header?.ToString() ?? string.Empty) + glyph;
        }

        return column.Header;
    }

    // Session 69: row background for alternating rows.
    // WPF alternation: index 0 = RowBackground, index 1 = AlternatingRowBackground
    // (AlternationCount coerced to 2 when AlternatingRowBackground is set).
    internal Microsoft.UI.Xaml.Media.Brush? ShimRowBackground(int rowIndex)
        => rowIndex % 2 == 1 && AlternatingRowBackground is { } alt ? alt : RowBackground;

    // Column width: explicit pixel widths are honored; Auto/SizeToCells/
    // SizeToHeader/Star are not computed yet, so they fall back to a default.
    // ActualWidth (if a width-computation pass ever sets it) wins.
    // ── Session 49: row headers ──────────────────────────────────────────────
    internal bool AreRowHeadersVisible
        => (HeadersVisibility & DataGridHeadersVisibility.Row) == DataGridHeadersVisibility.Row;

    internal double RowHeaderShimWidth => RowHeaderWidth > 0 ? RowHeaderWidth : 24;

    internal double ShimColumnWidth(DataGridColumn column)
    {
        var width = column.Width;
        // Absolute widths take priority: the notification chain may deliver a
        // width change before the post-layout pass re-runs, so trust the new
        // Width.Value rather than the stale ActualWidth from the previous pass.
        if (width.IsAbsolute && width.Value > 0)
            return Clamp(column, width.Value);
        // Non-absolute (Auto/Star/SizeTo*): use the ActualWidth set by the
        // post-layout pass, or NaN to let Uno auto-size to content.
        return column.ActualWidth > 0 ? column.ActualWidth : double.NaN;
    }

    // ── Session 26: reactivity ───────────────────────────────────────────────
    // Re-render when Items or Columns change. Subscriptions are idempotent
    // (hooked once); the rebuild no-ops until the template provides
    // PART_ShimRowsHost, after which it refreshes the whole grid.
    private bool _shimChangeHooked;

    private void HookShimChangeNotifications()
    {
        if (_shimChangeHooked)
        {
            return;
        }

        _shimChangeHooked = true;
        ((System.Collections.Specialized.INotifyCollectionChanged)Items).CollectionChanged += OnShimContentChanged;
        ((System.Collections.Specialized.INotifyCollectionChanged)Columns).CollectionChanged += OnShimContentChanged;
        // Session 62: row selection visuals are now driven by the real engine's
        // SelectionChanged event instead of a manual visual pass.
        SelectionChanged += OnShimSelectionChanged;
    }

    // Reflect the engine's selection onto the realized row containers. This is
    // the single source of truth for live row highlight; rebuilds re-read
    // SelectedItems directly (containers don't exist yet when the batch runs).
    private void OnShimSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        foreach (var removed in e.RemovedItems)
        {
            if (ItemContainerGenerator.ContainerFromItem(removed) is DataGridRow row)
            {
                row.IsSelected = false;
            }
        }

        foreach (var added in e.AddedItems)
        {
            if (ItemContainerGenerator.ContainerFromItem(added) is DataGridRow row)
            {
                row.IsSelected = true;
            }
        }
    }

    // Row is selected per the real engine (Selector.SelectedItems).
    private bool IsRowItemSelected(object? item) => item is not null && SelectedItems.Contains(item);

    private void OnShimContentChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        => BuildShimVisualTree();

    // ── Session 28/63: input-to-selection bridge ────────────────────────────
    // Pointer press on a row routes to the linked WPF DataGrid selection engine.
    // The engine owns SelectedItems/SelectedItem/SelectionChanged and row
    // visuals reflect that state.

    // Called by a row as it (re)builds a cell, so a retained cell selection
    // re-applies to the new cell instance after a rebuild.
    internal bool TryReselectCell(DataGridCell cell)
    {
        var selected = IsCellSelectedByEngine(cell);
        if (selected)
        {
            cell.IsSelected = true;
        }

        return selected;
    }

    // ── Session 43: editing hardening ────────────────────────────────────────
    // Effective read-only: the grid or the column being read-only blocks edits.
    internal bool IsCellEffectivelyReadOnly(DataGridColumn? column)
        => IsReadOnly || (column?.IsReadOnly ?? false);

    // Forwarders so DataGridCell (a different class) can raise the protected
    // edit-lifecycle events on the linked control root and read cancellation.
    internal DataGridBeginningEditEventArgs RaiseBeginningEdit(
        DataGridColumn column, DataGridRow row, RoutedEventArgs? editingEventArgs)
    {
        var args = new DataGridBeginningEditEventArgs(column, row, editingEventArgs ?? new RoutedEventArgs());
        OnBeginningEdit(args);
        return args;
    }

    internal DataGridCellEditEndingEventArgs RaiseCellEditEnding(
        DataGridColumn column, DataGridRow row, FrameworkElement? editingElement, DataGridEditAction action)
    {
        var args = new DataGridCellEditEndingEventArgs(column, row, editingElement!, action);
        OnCellEditEnding(args);
        return args;
    }

    // ── Session 47: row edit transactions ───────────────────────────────────
    // A cell entering edit begins the row's edit transaction; committing /
    // canceling the cell ends it, driving IEditableObject and RowEditEnding.
    private DataGridRow? _editingRow;

    internal void BeginRowEdit(DataGridRow? row)
    {
        if (row is null)
        {
            return;
        }

        if (ReferenceEquals(_editingRow, row)
            && Items.IsEditingItem
            && ReferenceEquals(Items.CurrentEditItem, row.Item))
        {
            return;
        }

        _editingRow = row;
        row.IsEditing = true;
        Items.EditItem(row.Item);
    }

    internal bool CommitRowEdit(DataGridRow? row)
    {
        if (row is null || !ReferenceEquals(_editingRow, row))
        {
            return true;
        }

        // Row-level validation (session 48): run RowValidationRules against the
        // item; any failure flags the row and keeps it in edit.
        foreach (var rule in RowValidationRules)
        {
            var result = rule.Validate(row.Item, System.Globalization.CultureInfo.CurrentCulture);
            if (!result.IsValid)
            {
                row.SetRowError(result.ErrorContent?.ToString());
                return false;
            }
        }

        var args = new DataGridRowEditEndingEventArgs(row, DataGridEditAction.Commit);
        OnRowEditEnding(args);
        if (args.Cancel)
        {
            return false;
        }

        row.ClearRowError();
        Items.CommitEdit();
        row.IsEditing = false;
        _editingRow = null;
        return true;
    }

    internal void CancelRowEdit(DataGridRow? row)
    {
        if (row is null || !ReferenceEquals(_editingRow, row))
        {
            return;
        }

        var args = new DataGridRowEditEndingEventArgs(row, DataGridEditAction.Cancel);
        OnRowEditEnding(args);
        if (Items.IsEditingItem)
        {
            if (Items.CanCancelEdit)
            {
                Items.CancelEdit();
            }
            else
            {
                Items.CommitEdit();
            }
        }

        row.IsEditing = false;
        _editingRow = null;
    }

    internal void HandleShimCellClicked(DataGridCell cell)
    {
        var previousModifiers = Input.Keyboard.ModifiersOverride;
        Input.Keyboard.ModifiersOverride = Input.ModifierKeys.None;
        try
        {
            var oldCurrentCell = CurrentCellContainer;
            CurrentCellContainer = cell;
            HandleSelectionForCellInput(
                cell,
                startDragging: false,
                allowsExtendSelect: true,
                allowsMinimalSelect: true);
            SyncRealizedCellSelection();
            // The upstream setter's IsKeyboardFocusWithin guard skips
            // NotifyCurrentCellContainerChanged in headless/non-focused
            // scenarios. Drive the focus-border visual explicitly so that
            // the current cell always shows a visual indicator.
            if (oldCurrentCell is not null && !ReferenceEquals(oldCurrentCell, cell))
                oldCurrentCell.NotifyCurrentCellContainerChanged();
            cell.NotifyCurrentCellContainerChanged();
        }
        finally
        {
            Input.Keyboard.ModifiersOverride = previousModifiers;
        }
    }

    internal void HandleShimRowClicked(DataGridRow clicked)
        => HandleShimRowClicked(clicked, global::Windows.System.VirtualKeyModifiers.None);

    internal void HandleShimRowClicked(DataGridRow clicked, global::Windows.System.VirtualKeyModifiers modifiers)
    {
        var previousModifiers = Input.Keyboard.ModifiersOverride;
        Input.Keyboard.ModifiersOverride = ToWpfModifiers(modifiers);
        try
        {
            HandleSelectionForRowHeaderAndDetailsInput(clicked, startDragging: false);
        }
        finally
        {
            Input.Keyboard.ModifiersOverride = previousModifiers;
        }
    }

    private static Input.ModifierKeys ToWpfModifiers(global::Windows.System.VirtualKeyModifiers modifiers)
    {
        var result = Input.ModifierKeys.None;
        if ((modifiers & global::Windows.System.VirtualKeyModifiers.Control) != 0)
        {
            result |= Input.ModifierKeys.Control;
        }

        if ((modifiers & global::Windows.System.VirtualKeyModifiers.Shift) != 0)
        {
            result |= Input.ModifierKeys.Shift;
        }

        if ((modifiers & global::Windows.System.VirtualKeyModifiers.Menu) != 0)
        {
            result |= Input.ModifierKeys.Alt;
        }

        return result;
    }

    private void ClearRealRowSelection()
    {
        BeginUpdateSelectedItems();
        try
        {
            SelectedItems.Clear();
        }
        finally
        {
            EndUpdateSelectedItems();
        }
    }

    private void PruneRealRowSelection()
    {
        var present = Items.Cast<object?>().ToList();
        var removed = SelectedItems
            .Cast<object?>()
            .Where(selected => !present.Any(item => EqualsEx(item, selected)))
            .ToList();
        if (removed.Count == 0)
        {
            return;
        }

        BeginUpdateSelectedItems();
        try
        {
            foreach (var item in removed)
            {
                SelectedItems.Remove(item);
            }
        }
        finally
        {
            EndUpdateSelectedItems();
        }
    }

    private void PruneRealCellSelection()
    {
        var present = Items.Cast<object?>().ToList();
        var removed = SelectedCells
            .Cast<DataGridCellInfo>()
            .Where(cell => !present.Any(item => EqualsEx(item, cell.Item)))
            .ToList();

        if (removed.Count > 0)
        {
            using (UpdateSelectedCells())
            {
                foreach (var cell in removed)
                {
                    SelectedCells.Remove(cell);
                }
            }
        }

        if (CurrentCell.IsValid && !present.Any(item => EqualsEx(item, CurrentCell.Item)))
        {
            CurrentCell = DataGridCellInfo.Unset;
        }
    }

    private bool IsCellSelectedByEngine(DataGridCell cell)
        => (CurrentCell.IsValid
                && EqualsEx(CurrentCell.Item, cell.RowDataItem)
                && ReferenceEquals(CurrentCell.Column, cell.Column))
            || SelectedCells
            .Cast<DataGridCellInfo>()
            .Any(info => EqualsEx(info.Item, cell.RowDataItem) && ReferenceEquals(info.Column, cell.Column));

    private void SyncRealizedCellSelection()
    {
        foreach (var row in ItemContainerGenerator.Containers.OfType<DataGridRow>())
        {
            for (var i = 0; i < Columns.Count; i++)
            {
                if (row.TryGetCell(i) is { } realizedCell)
                {
                    realizedCell.SyncIsSelected(IsCellSelectedByEngine(realizedCell));
                }
            }
        }
    }

    // ── Session 33: keyboard navigation ──────────────────────────────────────
    // Up/Down arrows move the single selection between rows.
    private const int ShimPageSize = 5;

    protected override void OnKeyDown(Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.Key)
        {
            case global::Windows.System.VirtualKey.Up:
                MoveSelectionByOffset(-1);
                e.Handled = true;
                break;
            case global::Windows.System.VirtualKey.Down:
                MoveSelectionByOffset(1);
                e.Handled = true;
                break;
            case global::Windows.System.VirtualKey.Home:
                MoveSelectionToIndex(0);
                e.Handled = true;
                break;
            case global::Windows.System.VirtualKey.End:
                MoveSelectionToIndex(int.MaxValue);
                e.Handled = true;
                break;
            case global::Windows.System.VirtualKey.PageUp:
                MoveSelectionByOffset(-ShimPageSize);
                e.Handled = true;
                break;
            case global::Windows.System.VirtualKey.PageDown:
                MoveSelectionByOffset(ShimPageSize);
                e.Handled = true;
                break;
        }
    }

    internal void MoveSelectionByOffset(int delta)
    {
        var rows = ItemContainerGenerator.Containers.OfType<DataGridRow>().ToList();
        if (rows.Count == 0)
        {
            return;
        }

        var current = rows.FindIndex(r => r.IsSelected);
        var target = current < 0
            ? (delta > 0 ? 0 : rows.Count - 1)
            : Math.Clamp(current + delta, 0, rows.Count - 1);

        HandleShimRowClicked(rows[target]);
    }

    internal void MoveSelectionToIndex(int index)
    {
        var rows = ItemContainerGenerator.Containers.OfType<DataGridRow>().ToList();
        if (rows.Count == 0)
        {
            return;
        }

        HandleShimRowClicked(rows[Math.Clamp(index, 0, rows.Count - 1)]);
    }

    // Session 50 reuse: header click drives the real WPF sort path. PerformSort
    // raises the Sorting event, toggles direction, and updates
    // Items.SortDescriptions; ItemCollection.Refresh applies the sort and
    // raises Reset, which rebuilds the rendered rows in sorted order.
    internal void HandleShimHeaderClicked(DataGridColumn column) => PerformSort(column);

    // Session 67: column-header notification chain — mirrors the row-cell chain
    // from session 66. The upstream DataGrid.NotifyPropertyChanged would route
    // through ColumnHeadersPresenter (null in the shim) to headers; this shim
    // dispatch iterates _headerCells directly instead.
    private void ShimNotifyColumnHeaders(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        foreach (var header in _headerCells)
            header.NotifyPropertyChanged(d, e);
    }

    internal IEnumerable<DataGridColumn> ColumnsInDisplayOrder()
    {
        InternalColumns.RefreshDisplayIndexMap();
        for (var displayIndex = 0; displayIndex < Columns.Count; displayIndex++)
        {
            yield return ColumnFromDisplayIndex(displayIndex);
        }
    }
}
