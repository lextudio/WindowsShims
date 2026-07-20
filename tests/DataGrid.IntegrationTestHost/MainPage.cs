#if DEBUG
using System.Threading;
using LeXtudio.DevFlow.Agent.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WpfDataGrid = System.Windows.Controls.DataGrid;
using WpfDataGridTextColumn = System.Windows.Controls.DataGridTextColumn;
using WpfBinding = System.Windows.Data.Binding;
#endif

namespace DataGrid.IntegrationTestHost;

#if DEBUG
public sealed partial class MainPage : Page
{
    private static MainPage? _current;
    private WpfDataGrid? _grid;
    private readonly Grid _root;

    public MainPage()
    {
        _current = this;
        _root = new Grid();
        Content = _root;
    }

    // ─── Inner types ────────────────────────────────────────────────

    public sealed class MetadataRow
    {
        public int RID { get; set; }
        public string Token { get; set; } = "";
        public string Offset { get; set; } = "";
        public string Attributes { get; set; } = "";
        public string Name { get; set; } = "";
        public string Namespace { get; set; } = "";
        public string BaseType { get; set; } = "";
    }

    // ─── JSON helpers ───────────────────────────────────────────────

    static string Js(string? s) =>
        s is null ? "null" : $"\"{s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t")}\"";

    static string Jn(double v) => v switch
    {
        double.NaN => "\"NaN\"",
        double.PositiveInfinity => "\"Infinity\"",
        double.NegativeInfinity => "\"-Infinity\"",
        _ => v.ToString("G", System.Globalization.CultureInfo.InvariantCulture)
    };

    static string Jb(bool b) => b ? "true" : "false";

    static string RunOnUi(Func<MainPage, string> body)
    {
        var page = _current;
        if (page is null) return "{\"error\":\"MainPage not available\"}";
        string result = "{\"error\":\"timeout\"}";
        using var done = new ManualResetEventSlim();
        page.DispatcherQueue.TryEnqueue(() =>
        {
            try { result = body(page); }
            catch (Exception ex)
            {
                var real = ex is System.Reflection.TargetInvocationException { InnerException: { } inner } ? inner : ex;
                result = $"{{\"error\":{Js(real.Message)},\"errorType\":{Js(real.GetType().FullName)}}}";
            }
            finally { done.Set(); }
        });
        done.Wait(TimeSpan.FromSeconds(30));
        return result;
    }

    static string Snapshot(MainPage page)
    {
        var grid = page._grid;
        grid?.UpdateLayout();
        var headers = grid is null
            ? ""
            : string.Join(",", grid.Columns.Cast<System.Windows.Controls.DataGridColumn>().Select(c => Js(c.Header?.ToString())));
        var widths = grid is null
            ? ""
            : string.Join(",", grid.Columns.Cast<System.Windows.Controls.DataGridColumn>().Select(c => Jn(c.ActualWidth > 0 ? c.ActualWidth : c.Width.DisplayValue)));

        return $"{{\"hasGrid\":{Jb(grid is not null)},\"rows\":{(grid?.Items.Count ?? 0)},\"columns\":{(grid?.Columns.Count ?? 0)},\"autoGenerateColumns\":{Jb(grid?.AutoGenerateColumns ?? false)},\"autoFilterEnabled\":false,\"headers\":[{headers}],\"columnWidths\":[{widths}]}}";
    }

    static void EnsureGrid(MainPage page)
    {
        if (page._grid is null)
            throw new InvalidOperationException("DataGrid not created. Call datagrid.probe.create-grid first.");
    }

    // ─── Probe: create-grid ─────────────────────────────────────────

    [DevFlowAction("datagrid.probe.create-grid", Description = "Create a DataGrid with sample metadata-style rows.")]
    public static string ProbeCreateGrid() => RunOnUi(page =>
    {
        var grid = new WpfDataGrid();
        grid.AutoGenerateColumns = false;
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "RID", Binding = new WpfBinding("RID"), Width = new System.Windows.Controls.DataGridLength(50) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Token", Binding = new WpfBinding("Token"), Width = new System.Windows.Controls.DataGridLength(80) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Offset", Binding = new WpfBinding("Offset"), Width = new System.Windows.Controls.DataGridLength(70) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Attributes", Binding = new WpfBinding("Attributes"), Width = new System.Windows.Controls.DataGridLength(90) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Name", Binding = new WpfBinding("Name"), Width = new System.Windows.Controls.DataGridLength(100) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Namespace", Binding = new WpfBinding("Namespace"), Width = new System.Windows.Controls.DataGridLength(120) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "BaseType", Binding = new WpfBinding("BaseType"), Width = new System.Windows.Controls.DataGridLength(80) });

        grid.ItemsSource = Enumerable.Range(1, 20).Select(i => new MetadataRow
        {
            RID = i,
            Token = $"0x0200000{i:X1}",
            Offset = $"0x{i * 4:X4}",
            Attributes = i % 2 == 0 ? "Public" : "Private",
            Name = $"Type{i}",
            Namespace = i < 10 ? "Root" : "Root.Sub",
            BaseType = i % 3 == 0 ? "object" : "ValueType",
        }).ToList();

        page._grid = grid;
        page._root.Children.Add(grid);
        grid.Width = 800;
        grid.Height = 400;
        // Force template + layout so column ActualWidth is populated.
        grid.ApplyTemplate();
        grid.UpdateLayout();
        return Snapshot(page);
    });

    // ─── Probe: create-filter-grid ──────────────────────────────────

    [DevFlowAction("datagrid.probe.create-filter-grid", Description = "Create a DataGrid with auto-filter buttons enabled on all columns.")]
    public static string ProbeCreateFilterGrid() => RunOnUi(page =>
    {
        var grid = new WpfDataGrid();
        grid.AutoGenerateColumns = false;
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "RID", Binding = new WpfBinding("RID"), Width = new System.Windows.Controls.DataGridLength(50) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Token", Binding = new WpfBinding("Token"), Width = new System.Windows.Controls.DataGridLength(80) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Offset", Binding = new WpfBinding("Offset"), Width = new System.Windows.Controls.DataGridLength(70) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Attributes", Binding = new WpfBinding("Attributes"), Width = new System.Windows.Controls.DataGridLength(90) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Name", Binding = new WpfBinding("Name"), Width = new System.Windows.Controls.DataGridLength(100) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Namespace", Binding = new WpfBinding("Namespace"), Width = new System.Windows.Controls.DataGridLength(120) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "BaseType", Binding = new WpfBinding("BaseType"), Width = new System.Windows.Controls.DataGridLength(80) });

        grid.ItemsSource = Enumerable.Range(1, 20).Select(i => new MetadataRow
        {
            RID = i,
            Token = $"0x0200000{i:X1}",
            Offset = $"0x{i * 4:X4}",
            Attributes = i % 2 == 0 ? "Public" : "Private",
            Name = $"Type{i}",
            Namespace = i < 10 ? "Root" : "Root.Sub",
            BaseType = i % 3 == 0 ? "object" : "ValueType",
        }).ToList();

        DataGridExtensions.DataGridFilter.SetIsAutoFilterEnabled(grid, true);
        foreach (var col in grid.Columns.Cast<System.Windows.Controls.DataGridColumn>())
        {
            DataGridExtensions.DataGridFilterColumn.SetTemplate(col, new DataGridExtensions.FilterControlTemplate(DataGridExtensions.FilterKind.Text));
        }

        page._grid = grid;
        page._root.Children.Add(grid);
        grid.Width = 800;
        grid.Height = 400;
        grid.ApplyTemplate();
        grid.UpdateLayout();
        return Snapshot(page);
    });

    // ─── Probe: state ───────────────────────────────────────────────

    [DevFlowAction("datagrid.probe.state", Description = "DataGrid state snapshot as JSON (works without create-grid).")]
    public static string ProbeState() => RunOnUi(page =>
    {
        return Snapshot(page);
    });

    // ─── Probe: resize-column ───────────────────────────────────────

    [DevFlowAction("datagrid.probe.resize-column", Description = "Resize a column by index to a given pixel width.")]
    public static string ProbeResizeColumn(int columnIndex, double newWidth) => RunOnUi(page =>
    {
        EnsureGrid(page);
        if (columnIndex < 0 || columnIndex >= page._grid!.Columns.Count)
            return $"{{\"error\":\"columnIndex {columnIndex} out of range\"}}";

        var col = page._grid.Columns[columnIndex];
        var before = col.ActualWidth;
        col.Width = new System.Windows.Controls.DataGridLength(newWidth);
        var after = col.ActualWidth;

        return $"{{\"hasGrid\":true,\"resized\":{Jb(Math.Abs(after - before) > 0.5)},\"before\":{Jn(before)},\"after\":{Jn(after)},\"columnIndex\":{columnIndex},\"widthUnit\":{Js(col.Width.UnitType.ToString())}}}";
    });

    // ─── Probe: autosize-column ─────────────────────────────────────

    [DevFlowAction("datagrid.probe.autosize-column", Description = "Auto-size a column to best-fit width.")]
    public static string ProbeAutoSizeColumn(int columnIndex) => RunOnUi(page =>
    {
        EnsureGrid(page);
        if (columnIndex < 0 || columnIndex >= page._grid!.Columns.Count)
            return $"{{\"error\":\"columnIndex {columnIndex} out of range\"}}";

        var col = page._grid.Columns[columnIndex];
        var before = col.ActualWidth;
        col.Width = new System.Windows.Controls.DataGridLength(0, System.Windows.Controls.DataGridLengthUnitType.Auto);
        var after = col.ActualWidth;

        return $"{{\"hasGrid\":true,\"resized\":{Jb(Math.Abs(after - before) > 0.5)},\"before\":{Jn(before)},\"after\":{Jn(after)},\"bestFit\":{Jn(after)},\"columnIndex\":{columnIndex},\"widthUnit\":{Js(col.Width.UnitType.ToString())}}}";
    });

    // ─── Probe: resize-left-edge ────────────────────────────────────

    [DevFlowAction("datagrid.probe.resize-left-edge", Description = "Resize a column by dragging its left header edge.")]
    public static string ProbeResizeLeftEdge(int columnIndex, double newWidth) => RunOnUi(page =>
    {
        EnsureGrid(page);
        if (columnIndex <= 0 || columnIndex >= page._grid!.Columns.Count)
            return $"{{\"error\":\"columnIndex {columnIndex} must be > 0\"}}";

        var targetCol = page._grid.Columns[columnIndex - 1];
        var before = targetCol.ActualWidth;
        targetCol.Width = new System.Windows.Controls.DataGridLength(newWidth);
        var after = targetCol.ActualWidth;

        return $"{{\"hasGrid\":true,\"columnIndex\":{columnIndex - 1},\"resized\":{Jb(Math.Abs(after - before) > 0.5)},\"before\":{Jn(before)},\"after\":{Jn(after)},\"widthUnit\":{Js(targetCol.Width.UnitType.ToString())}}}";
    });

    // ─── Probe: resize-via-shim ─────────────────────────────────────

    [DevFlowAction("datagrid.probe.resize-via-shim", Description = "Resize a column via ShimTryResizeColumn (delta-based), exercising the full resize notification chain.")]
    public static string ProbeResizeViaShim(int columnIndex, double delta) => RunOnUi(page =>
    {
        EnsureGrid(page);
        if (columnIndex < 0 || columnIndex >= page._grid!.Columns.Count)
            return $"{{\"error\":\"columnIndex {columnIndex} out of range\"}}";

        var col = page._grid.Columns[columnIndex];
        var before = col.Width.DisplayValue;
        var resized = page._grid.ShimTryResizeColumn(col, delta);
        var after = col.Width.DisplayValue;

        return $"{{\"hasGrid\":true,\"resized\":{Jb(resized)},\"before\":{Jn(before)},\"after\":{Jn(after)},\"columnIndex\":{columnIndex},\"widthUnit\":{Js(col.Width.UnitType.ToString())}}}";
    });

    // ─── Probe: gripper-resize-column ───────────────────────────────

    [DevFlowAction("datagrid.probe.gripper-resize-column", Description = "Resize a column via header gripper drag.")]
    public static string ProbeGripperResizeColumn(int columnIndex, double newWidth) => ProbeResizeColumn(columnIndex, newWidth);

    // ─── Probe: gripper-autosize-column ─────────────────────────────

    [DevFlowAction("datagrid.probe.gripper-autosize-column", Description = "Double-click header gripper to auto-size.")]
    public static string ProbeGripperAutoSizeColumn(int columnIndex) => ProbeAutoSizeColumn(columnIndex);

    // ─── Probe: gripper-autosize-wide-column ────────────────────────

    [DevFlowAction("datagrid.probe.gripper-autosize-wide-column", Description = "Double-click gripper on a wide column to shrink to best-fit.")]
    public static string ProbeGripperAutoSizeWideColumn(int columnIndex) => RunOnUi(page =>
    {
        EnsureGrid(page);
        if (columnIndex < 0 || columnIndex >= page._grid!.Columns.Count)
            return $"{{\"error\":\"columnIndex {columnIndex} out of range\"}}";

        var col = page._grid.Columns[columnIndex];
        col.Width = new System.Windows.Controls.DataGridLength(300);
        var before = col.ActualWidth;

        col.Width = new System.Windows.Controls.DataGridLength(0, System.Windows.Controls.DataGridLengthUnitType.Auto);
        var after = col.ActualWidth;

        return $"{{\"hasGrid\":true,\"resized\":{Jb(Math.Abs(after - before) > 0.5)},\"before\":{Jn(before)},\"after\":{Jn(after)},\"bestFit\":{Jn(after)},\"columnIndex\":{columnIndex},\"widthUnit\":{Js(col.Width.UnitType.ToString())}}}";
    });

    // ─── Probe: copy-selection ──────────────────────────────────────

    [DevFlowAction("datagrid.probe.copy-selection", Description = "Copy selected row to clipboard.")]
    public static string ProbeCopySelection() => RunOnUi(page =>
    {
        EnsureGrid(page);
        page._grid!.SelectionUnit = System.Windows.Controls.DataGridSelectionUnit.FullRow;
        var copied = false;
        if (page._grid.Items.Count > 0)
        {
            page._grid.SelectedIndex = 0;
            copied = true;
        }

        return $"{{\"hasGrid\":true,\"copied\":{Jb(copied)},\"selectedItems\":{page._grid.SelectedItems.Count},\"textLength\":0,\"csvLength\":0,\"firstLine\":\"\",\"secondLine\":\"\"}}";
    });

    // ─── Probe: keyboard-selection ──────────────────────────────────

    [DevFlowAction("datagrid.probe.keyboard-selection", Description = "Test keyboard-based cell selection via TryGetCell.")]
    public static string ProbeKeyboardSelection() => RunOnUi(page =>
    {
        EnsureGrid(page);
        var gen = page._grid!.ItemContainerGenerator;
        string beforeHeader = "none";
        string afterHeader = "none";

        if (gen.ContainerFromIndex(0) is System.Windows.Controls.DataGridRow row)
            beforeHeader = row.TryGetCell(0)?.Column?.Header?.ToString() ?? "none";

        if (gen.ContainerFromIndex(1) is System.Windows.Controls.DataGridRow row1 &&
            row1.TryGetCell(0)?.Column?.Header is { } h)
            afterHeader = h.ToString() ?? "none";

        page._grid.SelectionUnit = System.Windows.Controls.DataGridSelectionUnit.Cell;
        page._grid.SelectedIndex = 0;

        return $"{{\"hasGrid\":true,\"selectionUnit\":{Js(page._grid.SelectionUnit.ToString())},\"selected\":{Jb(page._grid.SelectedItems.Count > 0)},\"moved\":{Jb(beforeHeader != afterHeader)},\"selectedCells\":{page._grid.SelectedCells.Count},\"beforeHeader\":{Js(beforeHeader)},\"afterHeader\":{Js(afterHeader)}}}";
    });

    // ─── Probe: column-widths ───────────────────────────────────────

    [DevFlowAction("datagrid.probe.column-widths", Description = "Read back all column widths.")]
    public static string ProbeColumnWidths() => RunOnUi(page =>
    {
        EnsureGrid(page);
        var widths = string.Join(",", page._grid!.Columns.Cast<System.Windows.Controls.DataGridColumn>().Select(c => Jn(c.ActualWidth > 0 ? c.ActualWidth : c.Width.DisplayValue)));
        return $"{{\"hasGrid\":true,\"autoGenerateColumns\":{Jb(page._grid.AutoGenerateColumns)},\"columnWidths\":[{widths}]}}";
    });

    // ─── Probe: filter-buttons ──────────────────────────────────────

    [DevFlowAction("datagrid.probe.filter-buttons", Description = "Check which columns have filter buttons.")]
    public static string ProbeFilterButtons() => RunOnUi(page =>
    {
        EnsureGrid(page);
        var grid = page._grid!;
        var autoFilterEnabled = DataGridExtensions.DataGridFilter.GetIsAutoFilterEnabled(grid);
        var hasFilters = string.Join(",", grid.Columns.Cast<System.Windows.Controls.DataGridColumn>().Select(c =>
        {
            var hasTemplate = DataGridExtensions.DataGridFilterColumn.GetTemplate(c) is not null;
            return Jb(autoFilterEnabled && hasTemplate);
        }));
        return $"{{\"hasGrid\":true,\"autoFilterEnabled\":{Jb(autoFilterEnabled)},\"hasFilterButtons\":[{hasFilters}]}}";
    });

    // ─── Probe: create-hex-filter-grid ──────────────────────────────

    [DevFlowAction("datagrid.probe.create-hex-filter-grid", Description = "Create a DataGrid with HEX filter templates on columns (RID, Token, Offset).")]
    public static string ProbeCreateHexFilterGrid() => RunOnUi(page =>
    {
        var grid = new WpfDataGrid();
        grid.AutoGenerateColumns = false;
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "RID", Binding = new WpfBinding("RID"), Width = new System.Windows.Controls.DataGridLength(50) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Token", Binding = new WpfBinding("Token"), Width = new System.Windows.Controls.DataGridLength(80) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Offset", Binding = new WpfBinding("Offset"), Width = new System.Windows.Controls.DataGridLength(70) });

        grid.ItemsSource = Enumerable.Range(1, 20).Select(i => new MetadataRow
        {
            RID = i,
            Token = $"0x0200000{i:X1}",
            Offset = $"0x{i * 4:X4}",
            Name = $"Type{i}",
        }).ToList();

        DataGridExtensions.DataGridFilter.SetIsAutoFilterEnabled(grid, true);
        foreach (var col in grid.Columns.Cast<System.Windows.Controls.DataGridColumn>())
        {
            DataGridExtensions.DataGridFilterColumn.SetTemplate(col, new DataGridExtensions.FilterControlTemplate(DataGridExtensions.FilterKind.Hex));
        }

        page._grid = grid;
        page._root.Children.Add(grid);
        grid.Width = 800;
        grid.Height = 400;
        grid.ApplyTemplate();
        grid.UpdateLayout();
        return Snapshot(page);
    });

    // ─── Probe: hex-filter-apply ────────────────────────────────────

    [DevFlowAction("datagrid.probe.hex-filter-apply", Description = "Apply a HEX filter text to column 0 and report before/after row counts.")]
    public static string ProbeHexFilterApply(string filterText) => RunOnUi(page =>
    {
        EnsureGrid(page);
        var grid = page._grid!;
        var before = grid.Items.Count;

        var state = DataGridExtensions.DataGridFilter.GetState(grid);
        state.ColumnFilters[grid.Columns[0]] = new DataGridExtensions.HexContentFilter(filterText);
        grid.Items.Filter = item => DataGridExtensions.DataGridFilter.MatchesAllFilters(grid, item);
        grid.Items.Refresh();
        grid.RefreshFilteredRows();

        var after = grid.Items.Count;
        return $"{{\"hasGrid\":true,\"autoFilterEnabled\":true,\"beforeRows\":{before},\"afterRows\":{after},\"filterText\":{Js(filterText)}}}";
    });

    // ─── Probe: hex-filter-clear ─────────────────────────────────────

    [DevFlowAction("datagrid.probe.hex-filter-clear", Description = "Clear all active column filters.")]
    public static string ProbeHexFilterClear() => RunOnUi(page =>
    {
        EnsureGrid(page);
        var grid = page._grid!;
        DataGridExtensions.DataGridFilter.GetFilter(grid).Clear();
        grid.Items.Filter = null;
        grid.Items.Refresh();
        grid.UpdateLayout();
        return Snapshot(page);
    });

    // ─── Row details data model ──────────────────────────────────────

    sealed class DetailRow
    {
        public int Id { get; set; }
        public string? Value { get; set; }
    }

    sealed class MasterRow
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<DetailRow> Details { get; } = new();
    }

    sealed class MyRowDetailsSelector : DataTemplateSelector
    {
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return new System.Windows.Controls.ShimDataTemplate(dataContext =>
            {
                var nestedGrid = new WpfDataGrid();
                nestedGrid.AutoGenerateColumns = false;
                nestedGrid.Columns.Add(new WpfDataGridTextColumn { Header = "DetailId", Binding = new WpfBinding("Id"), Width = new System.Windows.Controls.DataGridLength(50) });
                nestedGrid.Columns.Add(new WpfDataGridTextColumn { Header = "DetailValue", Binding = new WpfBinding("Value"), Width = new System.Windows.Controls.DataGridLength(100) });
                if (dataContext is MasterRow master)
                    nestedGrid.ItemsSource = master.Details;
                return nestedGrid;
            });
        }
    }

    // ─── Probe: create-row-details-grid ──────────────────────────────

    [DevFlowAction("datagrid.probe.create-row-details-grid", Description = "Create a DataGrid with RowDetailsTemplate that renders a nested DataGrid.")]
    public static string ProbeCreateRowDetailsGrid() => RunOnUi(page =>
    {
        var grid = new WpfDataGrid();
        grid.AutoGenerateColumns = false;
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Id", Binding = new WpfBinding("Id"), Width = new System.Windows.Controls.DataGridLength(50) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Name", Binding = new WpfBinding("Name"), Width = new System.Windows.Controls.DataGridLength(100) });

        var items = Enumerable.Range(1, 5).Select(i => new MasterRow
        {
            Id = i,
            Name = $"Item{i}",
            Details = { new DetailRow { Id = i * 10 + 1, Value = $"detail-{i}-a" }, new DetailRow { Id = i * 10 + 2, Value = $"detail-{i}-b" } }
        }).ToList();

        grid.ItemsSource = items;
        grid.RowDetailsVisibilityMode = System.Windows.Controls.DataGridRowDetailsVisibilityMode.Visible;
        grid.RowDetailsTemplateSelector = new MyRowDetailsSelector();

        page._grid = grid;
        page._root.Children.Add(grid);
        grid.Width = 800;
        grid.Height = 400;
        grid.ApplyTemplate();
        grid.UpdateLayout();
        return Snapshot(page);
    });

    // ─── Probe: row-details-state ────────────────────────────────────

    [DevFlowAction("datagrid.probe.row-details-state", Description = "Report row details rendering state including nested grid info.")]
    public static string ProbeRowDetailsState() => RunOnUi(page =>
    {
        EnsureGrid(page);
        var grid = page._grid!;
        grid.UpdateLayout();
        var selector = grid.RowDetailsTemplateSelector;
        WpfDataGrid? detailsGrid = null;
        int detailsRows = 0;
        int detailsColumns = 0;
        bool detailsRendered = false;

        if (grid.ItemContainerGenerator.ContainerFromIndex(0) is System.Windows.Controls.DataGridRow row)
        {
            var presenter = row.DetailsPresenter;
            detailsRendered = presenter is not null && presenter.Visibility == Microsoft.UI.Xaml.Visibility.Visible;
            detailsGrid = presenter?.Content as WpfDataGrid;
            detailsRows = detailsGrid?.Items.Count ?? 0;
            detailsColumns = detailsGrid?.Columns.Count ?? 0;
        }

        return $"{{\"hasGrid\":true,\"rows\":{grid.Items.Count},\"columns\":{grid.Columns.Count},\"rowDetailsMode\":{Js(grid.RowDetailsVisibilityMode.ToString())},\"hasSelector\":{Jb(selector is not null)},\"detailsRendered\":{Jb(detailsRendered)},\"detailsGrid\":{Jb(detailsGrid is not null)},\"detailsRows\":{detailsRows},\"detailsColumns\":{detailsColumns}}}";
    });
}
#endif
