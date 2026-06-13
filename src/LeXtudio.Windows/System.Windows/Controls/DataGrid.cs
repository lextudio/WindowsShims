using System.Windows.Controls.Primitives;

namespace System.Windows.Controls;

// Uno-specific additions to the WPF DataGrid control root. Only members that
// do NOT appear in the linked upstream DataGrid.cs should live here. The
// upstream file is compiled as a partial on HAS_UNO so both parts merge.
public partial class DataGrid
{
    // UpdateVisualState: the upstream calls this (0-arg) which calls the
    // virtual ChangeVisualState. Provide the 0-arg overload in the shim part.
    internal void UpdateVisualState() => ChangeVisualState(true);

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

        HookShimChangeNotifications();

        host.Children.Clear();
        ItemContainerGenerator.ResetContainers();
        host.Children.Add(BuildHeaderRow());

        var selectionStillPresent = false;
        var cellSelectionStillPresent = false;

        foreach (var item in OrderedItems())
        {
            if (item is null)
            {
                continue;
            }

            var row = new DataGridRow();
            row.PrepareRow(item, this);
            // Re-apply selection across rebuilds (sort / reactivity) by item
            // identity, so the highlighted row follows its data item.
            if (_shimSelectedItem is not null && EqualsEx(item, _shimSelectedItem))
            {
                row.IsSelected = true;
                selectionStillPresent = true;
            }

            if (_shimSelectedCellItem is not null && EqualsEx(item, _shimSelectedCellItem))
            {
                cellSelectionStillPresent = true;
            }

            // Register the row so the linked WPF code can resolve containers
            // (selection, scroll-into-view, row details) via the generator.
            ItemContainerGenerator.RegisterContainer(item, row);
            host.Children.Add(row);
        }

        // The selected item left the collection — drop the stale selection.
        if (_shimSelectedItem is not null && !selectionStillPresent)
        {
            _shimSelectedItem = null;
            if (SelectedItem is not null)
            {
                SelectedItem = null;
            }
        }

        // Same for a retained cell selection whose item is gone.
        if (_shimSelectedCellItem is not null && !cellSelectionStillPresent)
        {
            _shimSelectedCell = null;
            _shimSelectedCellItem = null;
            _shimSelectedColumn = null;
            CurrentCell = DataGridCellInfo.Unset;
            SelectedCells.Clear();
        }

        ItemContainerGenerator.NotifyContainersGenerated();
    }

    // Items in display order: sorted by the active sort column if one is set,
    // otherwise the collection's own order.
    private IEnumerable<object?> OrderedItems()
    {
        var items = Items.Cast<object?>();
        if (_activeSortColumn is { } col && col.SortDirection is { } dir)
        {
            object? KeySelector(object? item) => item is null ? null : GetSortValue(col, item);
            items = dir == System.ComponentModel.ListSortDirection.Ascending
                ? items.OrderBy(KeySelector, Comparer<object?>.Default)
                : items.OrderByDescending(KeySelector, Comparer<object?>.Default);
        }

        return items.ToList();
    }

    private static object? GetSortValue(DataGridColumn column, object item)
    {
        var path = column.SortMemberPath;
        if (string.IsNullOrEmpty(path)
            && column is DataGridBoundColumn bound
            && bound.Binding is Data.Binding binding
            && binding.Path is { } propertyPath)
        {
            path = propertyPath.Path;
        }

        return string.IsNullOrEmpty(path)
            ? null
            : item.GetType().GetProperty(path)?.GetValue(item);
    }

    private Microsoft.UI.Xaml.Controls.StackPanel BuildHeaderRow()
    {
        var header = new Microsoft.UI.Xaml.Controls.StackPanel
        {
            Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal,
        };

        foreach (var column in Columns)
        {
            if (!column.IsVisible)
            {
                continue;
            }

            header.Children.Add(new DataGridColumnHeader
            {
                Column = column,
                Content = HeaderContent(column),
                Width = ShimColumnWidth(column),
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Microsoft.UI.Xaml.Thickness(4, 2, 4, 2),
            });
        }

        return header;
    }

    // Header content with a sort-direction glyph when this column is the
    // active sort.
    private object? HeaderContent(DataGridColumn column)
    {
        if (ReferenceEquals(column, _activeSortColumn) && column.SortDirection is { } dir)
        {
            var glyph = dir == System.ComponentModel.ListSortDirection.Ascending ? " ▲" : " ▼";
            return (column.Header?.ToString() ?? string.Empty) + glyph;
        }

        return column.Header;
    }

    // Column width: explicit pixel widths are honored; Auto/SizeToCells/
    // SizeToHeader/Star are not computed yet, so they fall back to a default.
    // ActualWidth (if a width-computation pass ever sets it) wins.
    internal double ShimColumnWidth(DataGridColumn column)
    {
        if (column.ActualWidth > 0)
        {
            return column.ActualWidth;
        }

        var width = column.Width;
        return width.IsAbsolute && width.Value > 0 ? width.Value : 120;
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
    }

    private void OnShimContentChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        => BuildShimVisualTree();

    // ── Session 28: shim selection ───────────────────────────────────────────
    // Pointer press on a row routes here. Single-select for now: clear every
    // generated row, select the clicked one, and reflect into SelectedItem.
    // Retained selection, by item identity, so it survives render rebuilds.
    private object? _shimSelectedItem;

    // Cell-level selection (SelectionUnit.Cell / CellOrRowHeader). Routes a
    // cell click to row selection in FullRow mode, otherwise selects the
    // single cell (clearing the previously cell-selected cell). The selection
    // is retained by (item, column) so it survives render rebuilds.
    private DataGridCell? _shimSelectedCell;
    private object? _shimSelectedCellItem;
    private DataGridColumn? _shimSelectedColumn;

    // Called by a row as it (re)builds a cell, so a retained cell selection
    // re-applies to the new cell instance after a rebuild.
    internal bool TryReselectCell(DataGridCell cell)
    {
        if (_shimSelectedCellItem is null
            || !EqualsEx(cell.RowDataItem, _shimSelectedCellItem)
            || !ReferenceEquals(cell.Column, _shimSelectedColumn))
        {
            return false;
        }

        cell.IsSelected = true;
        _shimSelectedCell = cell;
        return true;
    }

    internal void HandleShimCellClicked(DataGridCell cell)
    {
        if (SelectionUnit == DataGridSelectionUnit.FullRow)
        {
            if (cell.RowOwner is { } row)
            {
                HandleShimRowClicked(row);
            }

            return;
        }

        // Cell mode supersedes row selection — clear any selected row.
        foreach (var container in ItemContainerGenerator.Containers)
        {
            if (container is DataGridRow r && r.IsSelected)
            {
                r.IsSelected = false;
            }
        }

        _shimSelectedItem = null;

        if (_shimSelectedCell is { } previous && !ReferenceEquals(previous, cell))
        {
            previous.IsSelected = false;
        }

        cell.IsSelected = true;
        _shimSelectedCell = cell;
        _shimSelectedCellItem = cell.RowDataItem;
        _shimSelectedColumn = cell.Column;

        // Reflect into the WPF-facing cell-selection surface.
        var info = new DataGridCellInfo(cell);
        CurrentCell = info;
        SelectedCells.Clear();
        SelectedCells.Add(info);

        cell.RowOwner?.BringIntoView();
    }

    internal void HandleShimRowClicked(DataGridRow clicked)
    {
        foreach (var container in ItemContainerGenerator.Containers)
        {
            if (container is DataGridRow row && !ReferenceEquals(row, clicked))
            {
                row.IsSelected = false;
            }
        }

        clicked.IsSelected = true;
        _shimSelectedItem = clicked.Item;
        clicked.BringIntoView();

        if (clicked.Item is not null)
        {
            SelectedItem = clicked.Item;
        }
    }

    // ── Session 30: header-click sort ────────────────────────────────────────
    // Toggle the clicked column's sort: none → Ascending → Descending →
    // Ascending. Clears the other columns' direction (single sort key), then
    // re-renders in sorted order.
    private DataGridColumn? _activeSortColumn;

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

    internal void HandleShimHeaderClicked(DataGridColumn column)
    {
        if (!column.CanUserSort)
        {
            return;
        }

        var next = column.SortDirection == System.ComponentModel.ListSortDirection.Ascending
            ? System.ComponentModel.ListSortDirection.Descending
            : System.ComponentModel.ListSortDirection.Ascending;

        foreach (var other in Columns)
        {
            if (!ReferenceEquals(other, column))
            {
                other.SortDirection = null;
            }
        }

        column.SortDirection = next;
        _activeSortColumn = column;
        BuildShimVisualTree();
    }
}
