#if DEBUG
using System.Threading;
using DataGrid.TestScenarios;
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
        // The scenario gallery (ScenarioGallery.cs) wraps _root in a left-nav +
        // card layout for manual visual inspection; _root itself stays the
        // single attachment point every headless HTTP probe below already
        // targets (page._root.Children.Add(grid)), so probe behavior is
        // unaffected by the UI wrapping it.
        Content = BuildGalleryUi();
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

        // Nested-path exercise for TextSearch.TextPath ("Owner.Name") — session 122
        // follow-up 3.
        public MetadataRowOwner? Owner { get; set; }
    }

    public sealed class MetadataRowOwner
    {
        public string Name { get; set; } = "";
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

    static T? FindDescendant<T>(Microsoft.UI.Xaml.DependencyObject root) where T : class
    {
        var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i);
            if (child is T match) return match;
            if (FindDescendant<T>(child) is { } nested) return nested;
        }
        return null;
    }

    static void EnsureGrid(MainPage page)
    {
        if (page._grid is null)
            throw new InvalidOperationException("DataGrid not created. Call datagrid.probe.create-grid first.");
    }

    // ─── Shared scenario factories ───────────────────────────────────
    // Extracted so both the headless HTTP probes below (used by
    // DataGrid.IntegrationTests) and the interactive scenario gallery (added
    // for manual visual inspection — see ScenarioGallery.cs) build identical
    // grids from one place, rather than the gallery duplicating construction
    // logic that could drift out of sync with what the tests actually exercise.

    internal static WpfDataGrid BuildMetadataGrid()
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
            Owner = new MetadataRowOwner { Name = $"Owner{i}" },
        }).ToList();

        return grid;
    }

    internal static WpfDataGrid BuildFilterGrid()
    {
        var grid = DataGridScenarios.BuildMetadataGrid();
        DataGridExtensions.DataGridFilter.SetIsAutoFilterEnabled(grid, true);
        foreach (var col in grid.Columns.Cast<System.Windows.Controls.DataGridColumn>())
        {
            DataGridExtensions.DataGridFilterColumn.SetTemplate(col, new DataGridExtensions.FilterControlTemplate(DataGridExtensions.FilterKind.Text));
        }

        return grid;
    }

    // ─── Probe: create-grid ─────────────────────────────────────────

    [DevFlowAction("datagrid.probe.create-grid", Description = "Create a DataGrid with sample metadata-style rows.")]
    public static string ProbeCreateGrid() => RunOnUi(page =>
    {
        var grid = DataGridScenarios.BuildMetadataGrid();
        // Match the Sample's basic-grid geometry: all seven columns fit, while
        // the 20 data rows require vertical scrolling. This keeps a horizontal
        // scrollbar from occupying the bottom 20.5px and changing which line
        // the last row is expected to meet.
        grid.Width = 800;
        grid.Height = 400;
        page._root.Children.Clear();
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
        var grid = DataGridScenarios.BuildFilterGrid();
        page._root.Children.Clear();
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

    // ─── Probe: double-click-time ────────────────────────────────────
    // Verifies session 122's SystemParameters.DoubleClickTime now queries the
    // real OS value (Windows: user32!GetDoubleClickTime; macOS: AppKit's
    // NSEvent.doubleClickInterval via the Objective-C runtime) instead of a
    // hardcoded 500ms guess.
    [DevFlowAction("datagrid.probe.double-click-time", Description = "Read System.Windows.SystemParameters.DoubleClickTime.")]
    public static string ProbeDoubleClickTime() => RunOnUi(page =>
        $"{{\"doubleClickTimeMs\":{System.Windows.SystemParameters.DoubleClickTime}}}");

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

    // ─── Probe: sort-glyph ────────────────────────────────────────────
    // Verifies the DataGridColumnHeader VSM enrichment (session 122): real WPF's
    // linked DataGridColumnHeader.ChangeVisualState calls VisualStateManager.
    // GoToState("SortAscending"/"SortDescending"/"Unsorted") — previously a silent
    // no-op since the template had no VisualStateGroups. Reads back the header's
    // "SortIcon" TextBlock Opacity/Text to confirm the state change now actually
    // renders.

    static Microsoft.UI.Xaml.FrameworkElement? FindNamed(Microsoft.UI.Xaml.DependencyObject root, string name)
    {
        var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i);
            if (child is Microsoft.UI.Xaml.FrameworkElement { } fe && fe.Name == name) return fe;
            if (FindNamed(child, name) is { } nested) return nested;
        }
        return null;
    }

    // ─── Probe: header-hover ──────────────────────────────────────────
    // Verifies the DataGridColumnHeader CommonStates VSM group (session 122):
    // real WPF's linked ButtonBase-style ChangeVisualState calls
    // VisualStateManager.GoToState("MouseOver"/"Pressed"/"Normal") on pointer
    // enter/press — previously a silent no-op for two independent reasons, both
    // fixed this session: (1) VisualStates.GoToState's `element is Control` type
    // check resolved to this shim's own System.Windows.Controls.Control, which
    // no ButtonBase-derived type (including DataGridColumnHeader) actually
    // inherits from — broadened to Microsoft.UI.Xaml.Controls.Control; (2) the
    // header's ControlTemplate is now force-applied eagerly (ApplyShimGridLines)
    // so its VisualStateGroups exist by the time any state-changing property is
    // first coerced, instead of only materializing later via the natural layout
    // pass. Forces IsMouseOver via reflection (real pointer-event synthesis is
    // unreliable in this headless host — see docs/uno-macos-synthetic-click-issue.md)
    // and calls ChangeVisualState directly.
    [DevFlowAction("datagrid.probe.header-hover", Description = "Force IsMouseOver on the first column header and call ChangeVisualState directly; read back the HoverRectangle's fill color before/after.")]
    public static string ProbeHeaderHover() => RunOnUi(page =>
    {
        EnsureGrid(page);
        var grid = page._grid!;
        grid.UpdateLayout();

        var headerBorder = FindNamed(grid, "HeaderBorder");
        var hoverRect = headerBorder is not null ? FindNamed(headerBorder, "HoverRectangle") as Microsoft.UI.Xaml.Shapes.Rectangle : null;
        System.Windows.Controls.Primitives.DataGridColumnHeader? headerControl = null;
        for (Microsoft.UI.Xaml.DependencyObject? p = headerBorder; p is not null; p = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(p))
        {
            if (p is System.Windows.Controls.Primitives.DataGridColumnHeader dch) { headerControl = dch; break; }
        }

        global::Windows.UI.Color ColorOf() => (hoverRect?.Fill as Microsoft.UI.Xaml.Media.SolidColorBrush)?.Color ?? default;
        var before = ColorOf();

        var isMouseOverField = typeof(System.Windows.Controls.Primitives.ButtonBase).GetField("_isPointerOver", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        isMouseOverField?.SetValue(headerControl, true);

        var changeVisualState = typeof(System.Windows.Controls.Primitives.ButtonBase).GetMethod(
            "ChangeVisualState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null, [typeof(bool)], null);
        changeVisualState?.Invoke(headerControl, [true]);
        grid.UpdateLayout();

        var after = ColorOf();

        return $"{{\"hasGrid\":true,\"headerControlFound\":{Jb(headerControl is not null)},\"hoverRectFound\":{Jb(hoverRect is not null)}," +
               $"\"beforeAlpha\":{before.A},\"afterAlpha\":{after.A}}}";
    });

    // ─── Probe: sort-arrow ────────────────────────────────────────────
    // Verifies real, pre-existing sort-arrow rendering (DataGrid.HeaderContent,
    // a Path glyph above the header text driven directly by column.SortDirection
    // at content-build time — no VisualStateManager involved) still shows exactly
    // once per sorted column after DataGrid.PerformSort, and that the earlier
    // VSM-based header-styling work did not leave a second, duplicate arrow
    // behind (an actual regression a first attempt introduced and then removed —
    // see docs/session121.md).
    [DevFlowAction("datagrid.probe.sort-arrow", Description = "Sort a column via DataGrid.PerformSort; report how many sort-arrow Path glyphs are found in that column's header (should be exactly 1).")]
    public static string ProbeSortArrow(int columnIndex) => RunOnUi(page =>
    {
        EnsureGrid(page);
        var grid = page._grid!;
        grid.UpdateLayout();

        var column = grid.Columns[columnIndex];

        var performSort = typeof(WpfDataGrid).GetMethod("PerformSort", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        performSort?.Invoke(grid, [column]);
        grid.UpdateLayout();

        // Never hold a header reference across PerformSort (it rebuilds the header
        // row) — re-fetch fresh via TryGetCell-equivalent header lookup afterward.
        var header = grid.ColumnFromDisplayIndex(columnIndex) is { } c
            ? System.Linq.Enumerable.FirstOrDefault(
                System.Linq.Enumerable.OfType<System.Windows.Controls.Primitives.DataGridColumnHeader>(
                    EnumerateVisualTree(grid)), h => h.Column == c)
            : null;

        // HeaderContent's arrow Path is a logical (not necessarily visual-tree-walkable
        // in this harness) child of Content — assert via the object graph directly
        // rather than VisualTreeHelper, which has proven unreliable for this specific
        // Content-not-yet-materialized case elsewhere this session too.
        var contentGrid = header?.Content as Microsoft.UI.Xaml.Controls.Grid;
        var arrowCount = contentGrid is not null
            ? System.Linq.Enumerable.Count(contentGrid.Children, ch => ch is Microsoft.UI.Xaml.Shapes.Path)
            : -1;
        // No "SortIcon"-named element should exist anywhere anymore — that was the
        // redundant, since-removed VSM-driven arrow a first attempt introduced.
        var duplicateSortIconStillPresent = FindNamed(grid, "SortIcon") is not null;

        return $"{{\"hasGrid\":true,\"headerFound\":{Jb(header is not null)},\"sortDirection\":{Js(column.SortDirection?.ToString())}," +
               $"\"arrowCount\":{arrowCount},\"duplicateSortIconStillPresent\":{Jb(duplicateSortIconStillPresent)}}}";
    });

    // ─── Probe: text-search ───────────────────────────────────────────
    // Verifies session 122's TextSearch wiring: previously TextSearch.DoSearch
    // was never called from anywhere (ItemsControlSpine.OnTextInput was an
    // empty stub DataGrid never overrode) — the whole incremental-search
    // feature was dead code, not just "simplified" as the README claimed.
    // Now DataGrid.OnKeyDown maps unmodified letter/digit VirtualKeys to
    // DoSearch calls, and a match routes through the new
    // ItemsControl.NavigateToItem override (DataGrid's, which reuses
    // MoveSelectionToIndex — real scroll-into-view + selection, not just a
    // container-focus attempt that silently no-ops for virtualized rows).
    [DevFlowAction("datagrid.probe.text-search", Description = "Exercise DataGrid's TextSearch wiring end-to-end via reflection (key-to-char mapping + DoSearch + NavigateToItem selection).")]
    public static string ProbeTextSearch() => RunOnUi(page =>
    {
        EnsureGrid(page);
        var grid = page._grid!;
        grid.IsTextSearchEnabled = true;
        grid.SelectedIndex = -1;

        var keyToChar = typeof(WpfDataGrid).GetMethod("ShimVirtualKeyToChar", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var mappedA = keyToChar?.Invoke(null, [global::Windows.System.VirtualKey.A]) as string;
        var mappedEnter = keyToChar?.Invoke(null, [global::Windows.System.VirtualKey.Enter]) as string;

        var textSearchType = typeof(WpfDataGrid).Assembly.GetType("System.Windows.Controls.TextSearch")!;
        var ensureInstance = textSearchType.GetMethod("EnsureInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var instance = ensureInstance!.Invoke(null, [grid]);
        var doSearch = textSearchType.GetMethod("DoSearch", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        // All sample rows share the same MetadataRow.ToString() (no override), so
        // any single character matches index 0 — enough to confirm DoSearch reaches
        // NavigateToItem and actually moves selection, without needing per-item text.
        var firstChar = grid.Items[0]!.ToString()![0].ToString();
        doSearch!.Invoke(instance, [firstChar]);
        var selectedIndexToStringMatch = grid.SelectedIndex;

        // Session 122 (follow-up): TextSearch.TextPath — reflection-based
        // per-item property matching (MetadataRow.Name = "Type1".."Type20"),
        // exercising the new GetTextPath/GetItemText path instead of the
        // ToString()-only fallback above. "Type15" is unique (unlike "Type1",
        // which prefix-matches Type1/10-19), so a correct match must land
        // on the row whose Name is exactly "Type15" (RID 15 -> index 14).
        var textPathType = typeof(WpfDataGrid).Assembly.GetType("System.Windows.Controls.TextSearch")!;
        textPathType.GetMethod("SetTextPath", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)!
            .Invoke(null, [grid, "Name"]);
        grid.SelectedIndex = -1;
        var instance2 = ensureInstance.Invoke(null, [grid]);
        foreach (var ch in "Type15")
        {
            doSearch!.Invoke(instance2, [ch.ToString()]);
        }
        var selectedIndexByTextPath = grid.SelectedIndex;

        // Session 122 (follow-up 3): TextPath now resolves via
        // BindingExpression.EvaluatePath, a real dotted-path walker — reuses the
        // binding shim's own multi-segment resolution instead of a
        // single-property-only lookup. "Owner15" is unique per-item (each row's
        // Owner.Name is "Owner{RID}"), so a correct nested-path match must land
        // on RID 15 -> index 14, same as the flat-property case above.
        textPathType.GetMethod("SetTextPath", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)!
            .Invoke(null, [grid, "Owner.Name"]);
        grid.SelectedIndex = -1;
        var instance3 = ensureInstance.Invoke(null, [grid]);
        foreach (var ch in "Owner15")
        {
            doSearch!.Invoke(instance3, [ch.ToString()]);
        }
        var selectedIndexByNestedTextPath = grid.SelectedIndex;

        return $"{{\"hasGrid\":true,\"mappedA\":{Js(mappedA)},\"mappedEnterIsNull\":{Jb(mappedEnter is null)}," +
               $"\"selectionMovedByToString\":{Jb(selectedIndexToStringMatch == 0)}," +
               $"\"selectedIndexByTextPath\":{selectedIndexByTextPath},\"textPathMatchedType15\":{Jb(selectedIndexByTextPath == 14)}," +
               $"\"selectedIndexByNestedTextPath\":{selectedIndexByNestedTextPath},\"nestedTextPathMatchedOwner15\":{Jb(selectedIndexByNestedTextPath == 14)}}}";
    });

    static System.Collections.Generic.IEnumerable<Microsoft.UI.Xaml.DependencyObject> EnumerateVisualTree(Microsoft.UI.Xaml.DependencyObject root)
    {
        yield return root;
        var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            foreach (var d in EnumerateVisualTree(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i)))
            {
                yield return d;
            }
        }
    }

    static int CountDescendants<T>(Microsoft.UI.Xaml.DependencyObject root) where T : class
    {
        var count = 0;
        var childCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < childCount; i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i);
            if (child is T) count++;
            count += CountDescendants<T>(child);
        }
        return count;
    }

    // ─── Probe: cell-text ─────────────────────────────────────────────
    // Verifies a fix to DataGridCell.BuildVisualTree(): the bound TextBlock
    // DataGridBoundColumn.GenerateElement creates is a brand-new, unparented
    // element whose binding relies on inheriting DataContext once parented —
    // that inheritance was never actually reaching it (Content assignment sets
    // the DP but doesn't itself push DataContext down), so cell text silently
    // never resolved for any programmatically-built grid. Fixed by setting the
    // generated element's DataContext explicitly instead of relying on
    // inheritance timing.

    [DevFlowAction("datagrid.probe.cell-text", Description = "Read back the bound Text of a specific row/column's rendered cell content.")]
    public static string ProbeCellText(int rowIndex, int columnIndex) => RunOnUi(page =>
    {
        EnsureGrid(page);
        var grid = page._grid!;
        grid.UpdateLayout();

        var row = grid.ItemContainerGenerator.ContainerFromIndex(rowIndex) as System.Windows.Controls.DataGridRow;
        var cell = row?.TryGetCell(columnIndex);
        var text = (cell?.Content as Microsoft.UI.Xaml.Controls.TextBlock)?.Text;

        return $"{{\"hasGrid\":true,\"rowFound\":{Jb(row is not null)},\"cellFound\":{Jb(cell is not null)},\"text\":{Js(text)}}}";
    });

    [DevFlowAction("datagrid.probe.row-line-metrics", Description = "Read row header/cell/grid Y coordinates for grid-line alignment diagnostics.")]
    public static string ProbeRowLineMetrics(int rowIndex) => RunOnUi(page =>
    {
        EnsureGrid(page);
        var grid = page._grid!;
        grid.UpdateLayout();

        var row = grid.ItemContainerGenerator.ContainerFromIndex(rowIndex) as System.Windows.Controls.DataGridRow;
        var header = row?.RowHeader as Microsoft.UI.Xaml.FrameworkElement;
        var firstCell = row?.TryGetCell(0);
        var lastCell = row?.TryGetCell(Math.Max(0, grid.Columns.Count - 1));
        var scroller = grid.ShimGetRowsScrollViewer() as Microsoft.UI.Xaml.FrameworkElement;
        var outerBorder = grid.ShimGetOuterBorder() as Microsoft.UI.Xaml.FrameworkElement;

        static (double top, double bottom, double height) Bounds(Microsoft.UI.Xaml.FrameworkElement? element, Microsoft.UI.Xaml.UIElement relativeTo)
        {
            if (element is null)
            {
                return (double.NaN, double.NaN, double.NaN);
            }

            var p = element.TransformToVisual(relativeTo).TransformPoint(new global::Windows.Foundation.Point(0, 0));
            return (p.Y, p.Y + element.ActualHeight, element.ActualHeight);
        }

        var hb = Bounds(header, grid);
        var rb = Bounds(row, grid);
        var fb = Bounds(firstCell, grid);
        var lb = Bounds(lastCell, grid);
        var sb = Bounds(scroller, grid);
        var ob = Bounds(outerBorder, grid);
        var rowBottomDelta = Math.Max(Math.Abs(hb.bottom - fb.bottom), Math.Abs(hb.bottom - rb.bottom));
        var frameBottomDelta = Math.Abs(sb.bottom - ob.bottom);
        var rowFrameBottomDelta = Math.Abs(rb.bottom - ob.bottom);

        return $"{{\"hasGrid\":true,\"rowFound\":{Jb(row is not null)},\"headerFound\":{Jb(header is not null)},\"firstCellFound\":{Jb(firstCell is not null)}," +
               $"\"headerTop\":{Jn(hb.top)},\"headerBottom\":{Jn(hb.bottom)},\"headerHeight\":{Jn(hb.height)}," +
               $"\"rowTop\":{Jn(rb.top)},\"rowBottom\":{Jn(rb.bottom)},\"rowHeight\":{Jn(rb.height)}," +
               $"\"firstCellTop\":{Jn(fb.top)},\"firstCellBottom\":{Jn(fb.bottom)},\"firstCellHeight\":{Jn(fb.height)}," +
               $"\"lastCellBottom\":{Jn(lb.bottom)},\"scrollViewportBottom\":{Jn(sb.bottom)},\"outerBorderBottom\":{Jn(ob.bottom)}," +
               $"\"rowBottomDelta\":{Jn(rowBottomDelta)},\"frameBottomDelta\":{Jn(frameBottomDelta)}," +
               $"\"rowFrameBottomDelta\":{Jn(rowFrameBottomDelta)}}}";
    });

    [DevFlowAction("datagrid.probe.scroll-to-bottom", Description = "Scroll the current DataGrid to its maximum vertical offset.")]
    public static string ProbeScrollToBottom() => RunOnUi(page =>
    {
        EnsureGrid(page);
        var grid = page._grid!;
        grid.UpdateLayout();
        var scroller = grid.ShimGetRowsScrollViewer();
        if (scroller is null)
        {
            return "{\"hasGrid\":true,\"hasScroller\":false}";
        }

        scroller.ChangeView(null, scroller.ScrollableHeight, null, true);
        grid.UpdateLayout();
        return $"{{\"hasGrid\":true,\"hasScroller\":true,\"verticalOffset\":{Jn(scroller.VerticalOffset)}," +
               $"\"scrollableHeight\":{Jn(scroller.ScrollableHeight)},\"horizontalOffset\":{Jn(scroller.HorizontalOffset)}," +
               $"\"scrollableWidth\":{Jn(scroller.ScrollableWidth)},\"viewportHeight\":{Jn(scroller.ViewportHeight)}," +
               $"\"viewportWidth\":{Jn(scroller.ViewportWidth)},\"actualHeight\":{Jn(scroller.ActualHeight)}," +
               $"\"actualWidth\":{Jn(scroller.ActualWidth)}}}";
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

    internal static WpfDataGrid BuildHexFilterGrid()
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

        return grid;
    }

    [DevFlowAction("datagrid.probe.create-hex-filter-grid", Description = "Create a DataGrid with HEX filter templates on columns (RID, Token, Offset).")]
    public static string ProbeCreateHexFilterGrid() => RunOnUi(page =>
    {
        var grid = DataGridScenarios.BuildHexFilterGrid();
        page._root.Children.Clear();
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

    internal static WpfDataGrid BuildRowDetailsGrid()
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

        return grid;
    }

    [DevFlowAction("datagrid.probe.create-row-details-grid", Description = "Create a DataGrid with RowDetailsTemplate that renders a nested DataGrid.")]
    public static string ProbeCreateRowDetailsGrid() => RunOnUi(page =>
    {
        var grid = DataGridScenarios.BuildRowDetailsGrid();
        page._root.Children.Clear();
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

    // ─── Gap survey item 8: row-details variable-height rows under virtualization ──

    public sealed class VariableHeightRow
    {
        public int Id { get; set; }
        public bool IsTall { get; set; }
    }

    // Gives row 2 (0-based) a 150px-tall details panel; every other row gets none
    // (details Visibility stays Collapsed for them, matching a real "only some rows
    // have expandable detail content" scenario). A uniform-row-height virtualization
    // model would place row 3+ at `index * estimatedRowHeight`; the real cumulative
    // top must instead be pushed down by the extra ~150px row 2's details panel adds.
    sealed class VariableHeightDetailsSelector : DataTemplateSelector
    {
        protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is not VariableHeightRow { IsTall: true })
                return null; // no template -> BuildRowDetails leaves DetailsVisibility Collapsed

            return new System.Windows.Controls.ShimDataTemplate(_ => new Microsoft.UI.Xaml.Controls.Border
            {
                Height = 150,
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(global::Windows.UI.Color.FromArgb(0xFF, 0xDD, 0xEE, 0xFF)),
            });
        }
    }

    // ─── Probe: create-variable-height-grid ──────────────────────────

    internal static WpfDataGrid BuildVariableHeightGrid(int rowCount, int tallRowIndex)
    {
        var grid = new WpfDataGrid { AutoGenerateColumns = false };
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Id", Binding = new WpfBinding("Id"), Width = new System.Windows.Controls.DataGridLength(60) });

        grid.ItemsSource = Enumerable.Range(0, rowCount)
            .Select(i => new VariableHeightRow { Id = i, IsTall = i == tallRowIndex })
            .ToList();
        grid.RowDetailsVisibilityMode = System.Windows.Controls.DataGridRowDetailsVisibilityMode.Visible;
        grid.RowDetailsTemplateSelector = new VariableHeightDetailsSelector();

        return grid;
    }

    [DevFlowAction("datagrid.probe.create-variable-height-grid", Description = "Create a virtualized DataGrid where one row has a 150px RowDetails panel and the rest have none.")]
    public static string ProbeCreateVariableHeightGrid(int rowCount, int tallRowIndex) => RunOnUi(page =>
    {
        var grid = DataGridScenarios.BuildVariableHeightGrid(rowCount, tallRowIndex);
        page._root.Children.Clear();
        page._grid = grid;
        page._root.Children.Add(grid);
        grid.Width = 400;
        grid.Height = 300;
        grid.Measure(new global::Windows.Foundation.Size(400, 300));
        grid.Arrange(new global::Windows.Foundation.Rect(0, 0, 400, 300));
        grid.UpdateLayout();

        grid.ShimSetRowVirtualization(true);
        grid.UpdateLayout();

        // Headless Skia never fires EffectiveViewportChanged — force a real viewport
        // deterministically, same technique datagrid.probe.metadata-scroll-into-view uses.
        var rowsPresenter = FindDescendant<System.Windows.Controls.Primitives.DataGridRowsPresenter>(grid);
        rowsPresenter?.ShimForceViewport(0, 300);
        grid.UpdateLayout();
        grid.UpdateLayout();
        grid.UpdateLayout();

        return Snapshot(page);
    });

    // ─── Probe: variable-height-readback ──────────────────────────────

    // Reads the ScrollViewer's real ExtentHeight — VirtualizingStackPanel.MeasureOverride
    // returns it as the cumulative sum of every index's real-or-estimated height (see
    // OffsetOfIndex(itemCount, itemCount) in VirtualizingPanelStubs.cs), so it directly
    // reflects whether a taller row's actual measured height is really folded into the
    // total extent, rather than a uniform itemCount * estimatedRowHeight guess. (Per-row
    // screen-Y/DesiredSize reads were tried first but proved unreliable in this headless
    // Skia host — containers get recycled/remeasured between probe calls in ways that
    // reset DesiredSize before it can be read back; ExtentHeight, read once right after
    // layout settles, doesn't have that problem.)
    [DevFlowAction("datagrid.probe.variable-height-extent", Description = "Read back the virtualized rows ScrollViewer's ExtentHeight.")]
    public static string ProbeVariableHeightExtent() => RunOnUi(page =>
    {
        EnsureGrid(page);
        var grid = page._grid!;
        grid.UpdateLayout();
        var scroller = grid.ShimGetRowsScrollViewer();

        return $"{{\"hasGrid\":true,\"extentHeight\":{Jn(scroller?.ExtentHeight ?? -1)},\"viewportHeight\":{Jn(scroller?.ViewportHeight ?? -1)}}}";
    });

    // ─── Grouping: GroupStyle full-coverage model ────────────────────

    public sealed class GroupedRow
    {
        public string Country { get; set; } = "";
        public string Name { get; set; } = "";
    }

    // ─── Probe: create-grouped-style-grid ────────────────────────────
    // Exercises the GroupStyle surface raised to cover HeaderStringFormat,
    // HeaderTemplateSelector/ContainerStyleSelector, and GroupStyleSelector —
    // the gaps flagged in docs/session121.md's grouping arc. `mode` selects
    // which resolution path to exercise so each gets an isolated, unambiguous
    // read-back:
    //   "format"   — GroupStyle.HeaderStringFormat on the fixed fallback header
    //   "selector" — HeaderTemplateSelector + ContainerStyleSelector, recorded
    //                so the probe can confirm they were actually invoked
    //   "groupstyleselector" — ItemsControl.GroupStyleSelector taking
    //                precedence over the GroupStyle collection
    internal static WpfDataGrid BuildGroupedStyleGrid(string mode)
    {
        var grid = new WpfDataGrid { AutoGenerateColumns = false };
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Country", Binding = new WpfBinding("Country"), Width = new System.Windows.Controls.DataGridLength(80) });
        grid.Columns.Add(new WpfDataGridTextColumn { Header = "Name", Binding = new WpfBinding("Name"), Width = new System.Windows.Controls.DataGridLength(100) });

        // Single group only — GroupStyle mechanics (format/selector precedence)
        // are the point of this probe, not group ordering, and this shim's
        // grouping auto-syncs a SortDescription per grouped property (Slice 4),
        // which would otherwise reorder a multi-group scenario alphabetically.
        grid.ItemsSource = new List<GroupedRow>
        {
            new() { Country = "US", Name = "Alice" },
            new() { Country = "US", Name = "Bob" },
        };
        grid.Items.GroupDescriptions.Add(new System.Windows.Data.PropertyGroupDescription("Country"));
        grid.Items.Refresh();

        switch (mode)
        {
            case "format":
                grid.GroupStyle.Add(new System.Windows.Controls.GroupStyle { HeaderStringFormat = "{0} ({1} people)" });
                break;

            case "selector":
                grid.GroupStyle.Add(new System.Windows.Controls.GroupStyle
                {
                    HeaderTemplate = new Microsoft.UI.Xaml.DataTemplate(), // fallback — selector should win
                    HeaderTemplateSelector = new RecordingHeaderTemplateSelector(),
                    ContainerStyle = new Microsoft.UI.Xaml.Style(typeof(System.Windows.Controls.GroupItem)),
                    ContainerStyleSelector = new RecordingContainerStyleSelector(),
                });
                break;

            case "groupstyleselector":
                grid.GroupStyle.Add(new System.Windows.Controls.GroupStyle { HeaderStringFormat = "collection:{0}" });
                grid.GroupStyleSelector = (group, level) =>
                    new System.Windows.Controls.GroupStyle { HeaderStringFormat = "selector:{0}" };
                break;
        }

        return grid;
    }

    [DevFlowAction("datagrid.probe.create-grouped-style-grid", Description = "Create a DataGrid grouped by Country, with GroupStyle configured per `mode` (format|selector|groupstyleselector).")]
    public static string ProbeCreateGroupedStyleGrid(string mode) => RunOnUi(page =>
    {
        var grid = DataGridScenarios.BuildGroupedStyleGrid(mode);
        page._root.Children.Clear();
        page._grid = grid;
        page._root.Children.Add(grid);
        grid.Width = 800;
        grid.Height = 400;
        grid.ApplyTemplate();
        grid.UpdateLayout();
        grid.UpdateLayout();

        return Snapshot(page);
    });

    private sealed class RecordingHeaderTemplateSelector : Microsoft.UI.Xaml.Controls.DataTemplateSelector
    {
        internal static object? LastGroup;
        internal static readonly Microsoft.UI.Xaml.DataTemplate Selected = new();

        protected override Microsoft.UI.Xaml.DataTemplate SelectTemplateCore(object item, Microsoft.UI.Xaml.DependencyObject container)
        {
            LastGroup = item;
            return Selected;
        }
    }

    private sealed class RecordingContainerStyleSelector : System.Windows.Controls.StyleSelector
    {
        internal static object? LastGroup;
        internal static readonly Microsoft.UI.Xaml.Style Selected = new(typeof(System.Windows.Controls.GroupItem));

        public override Microsoft.UI.Xaml.Style SelectStyle(object item, Microsoft.UI.Xaml.DependencyObject container)
        {
            LastGroup = item;
            return Selected;
        }
    }

    // ─── Probe: grouped-style-readback ────────────────────────────────

    [DevFlowAction("datagrid.probe.grouped-style-readback", Description = "Read back the first group's rendered header (Content, ContentTemplate, Style) plus selector-invocation state.")]
    public static string ProbeGroupedStyleReadback() => RunOnUi(page =>
    {
        EnsureGrid(page);
        var grid = page._grid!;
        grid.UpdateLayout();

        var group = grid.Items.Groups.Count > 0 ? grid.Items.Groups[0] : null;
        var header = FindDescendant<System.Windows.Controls.GroupItem>(grid);
        var groupNames = string.Join(",", grid.Items.Groups.Select(g => g!.Name));

        return $"{{\"hasGroup\":{Jb(group is not null)},\"groupCount\":{grid.Items.Groups.Count},\"groupNames\":{Js(groupNames)}," +
               $"\"headerContent\":{Js(header?.Content?.ToString())}," +
               $"\"headerTemplateSelected\":{Jb(header is not null && ReferenceEquals(header.ContentTemplate, DataGridScenarios.RecordingHeaderTemplateSelector.Selected))}," +
               $"\"containerStyleSelected\":{Jb(header is not null && ReferenceEquals(header.Style, DataGridScenarios.RecordingContainerStyleSelector.Selected))}," +
               $"\"headerSelectorInvokedWithGroup\":{Jb(group is not null && ReferenceEquals(DataGridScenarios.RecordingHeaderTemplateSelector.LastGroup, group))}," +
               $"\"containerSelectorInvokedWithGroup\":{Jb(group is not null && ReferenceEquals(DataGridScenarios.RecordingContainerStyleSelector.LastGroup, group))}}}";
    });

    // ─── Probe: hides-if-empty-flatten ────────────────────────────────
    // HidesIfEmpty is meaningful only for a group with ItemCount == 0 — a shape
    // CollectionViewGroupBuilder.BuildGroups can never itself produce (it only
    // ever creates a bucket for a group name actually present among the
    // already-filtered items — see ItemCollection.Refresh()). So rather than
    // trying to coax BuildGroups into an unreachable state, this probe builds an
    // explicitly-empty CollectionViewGroupInternal directly (real internal API,
    // same one BuildGroups itself uses) and confirms FlattenWithHeaders — the
    // real function DataGrid's virtualized grouped-row path calls — omits it
    // when GroupStyle.HidesIfEmpty is set on the live grid used as the resolver
    // owner, and still renders it (header only) when not set.
    [DevFlowAction("datagrid.probe.hides-if-empty-flatten", Description = "Build one non-empty and one empty CollectionViewGroupInternal; report FlattenWithHeaders' slot count with the live grid's GroupStyle.HidesIfEmpty on/off.")]
    public static string ProbeHidesIfEmptyFlatten(bool hidesIfEmpty) => RunOnUi(page =>
    {
        var grid = new WpfDataGrid();
        grid.GroupStyle.Add(new System.Windows.Controls.GroupStyle { HidesIfEmpty = hidesIfEmpty });

        var nonEmpty = new MS.Internal.Data.CollectionViewGroupInternal(
            "US", new System.Windows.Data.PropertyGroupDescription("Country"), isBottomLevel: true);
        nonEmpty.AddLeaf(new DataGridScenarios.GroupedRow { Country = "US", Name = "Alice" });
        var empty = new MS.Internal.Data.CollectionViewGroupInternal(
            "UK", new System.Windows.Data.PropertyGroupDescription("Country"), isBottomLevel: true);

        var slots = MS.Internal.Data.CollectionViewGroupBuilder.FlattenWithHeaders([nonEmpty, empty], grid);

        return $"{{\"hasGrid\":true,\"slotCount\":{slots.Count},\"emptyGroupOmitted\":{Jb(slots.Count == 2)}}}";
    });

    // ─── Frozen columns: editable model ──────────────────────────────

    public sealed class FrozenEditRow
    {
        public string ColA { get; set; } = "";
        public string ColB { get; set; } = "";
        public string ColC { get; set; } = "";
        public string ColD { get; set; } = "";
    }

    // ─── Probe: create-frozen-edit-grid ──────────────────────────────
    // Closes the 3 remaining frozen-columns loose ends (vertical scroll,
    // real cell editing, boundary resize) with a standalone editable+tall
    // grid, under the real DataGridCellsPresenter cell host.

    internal static WpfDataGrid BuildFrozenEditGrid()
    {
        var rows = Enumerable.Range(0, 40)
            .Select(i => new FrozenEditRow { ColA = $"A{i}", ColB = $"B{i}", ColC = $"C{i}", ColD = $"D{i}" })
            .ToList();

        var grid = new WpfDataGrid { AutoGenerateColumns = false, ItemsSource = rows };
        foreach (var propertyName in new[] { "ColA", "ColB", "ColC", "ColD" })
        {
            grid.Columns.Add(new WpfDataGridTextColumn
            {
                Header = propertyName,
                Binding = new WpfBinding(propertyName),
                Width = new System.Windows.Controls.DataGridLength(220),
            });
        }

        return grid;
    }

    [DevFlowAction("datagrid.probe.create-frozen-edit-grid", Description = "Create a standalone, editable, 40-row DataGrid with FrozenColumnCount under the cells presenter.")]
    public static string ProbeCreateFrozenEditGrid(int frozenColumnCount) => RunOnUi(page =>
    {
        var grid = DataGridScenarios.BuildFrozenEditGrid();
        page._root.Children.Clear();
        page._grid = grid;
        page._root.Children.Add(grid);
        grid.Width = 800;
        grid.Height = 400;
        grid.Measure(new global::Windows.Foundation.Size(800, 400));
        grid.Arrange(new global::Windows.Foundation.Rect(0, 0, 800, 400));
        grid.UpdateLayout();

        grid.ShimSetCellsPresenterHost(true);
        grid.FrozenColumnCount = frozenColumnCount;
        grid.UpdateLayout();
        grid.UpdateLayout();

        return Snapshot(page);
    });

    // ─── Probe: frozen-edit-scroll ────────────────────────────────────

    [DevFlowAction("datagrid.probe.frozen-edit-scroll", Description = "Scroll the frozen-edit grid horizontally and vertically; report the resulting scroll offsets.")]
    public static string ProbeFrozenEditScroll(double scrollX, double scrollY) => RunOnUi(page =>
    {
        EnsureGrid(page);
        var grid = page._grid!;
        var scroller = grid.ShimGetRowsScrollViewer();
        if (scroller is null)
            return "{\"error\":\"no PART_ShimRowsScroll found\"}";

        scroller.ChangeView(scrollX, scrollY, null, true);
        grid.UpdateLayout();
        grid.UpdateLayout();

        return $"{{\"hasGrid\":true,\"scrollOffsetX\":{Jn(scroller.HorizontalOffset)},\"scrollOffsetY\":{Jn(scroller.VerticalOffset)}," +
               $"\"extentHeight\":{Jn(scroller.ExtentHeight)},\"viewportHeight\":{Jn(scroller.ViewportHeight)},\"scrollableHeight\":{Jn(scroller.ScrollableHeight)}}}";
    });

    // ─── Probe: frozen-edit-readback ──────────────────────────────────
    // Verifies: (1) a tracked row's frozen cell keeps its screen X position
    // (item-identity based, not "first row found" — resilient across scroll-
    // driven container recycling); (2) real cell editing (BeginEdit/CommitEdit)
    // on a presenter-hosted, non-frozen cell of the same row; (3) resizing at
    // the frozen/non-frozen boundary while FrozenColumnCount > 0.

    [DevFlowAction("datagrid.probe.frozen-edit-readback", Description = "Read back frozen-cell screen position (tracked by item identity), attempt real cell edit, and resize the frozen/non-frozen boundary columns.")]
    public static string ProbeFrozenEditReadback(int frozenColumnCount, int trackedRowIndex) => RunOnUi(page =>
    {
        EnsureGrid(page);
        var grid = page._grid!;
        grid.UpdateLayout();

        var trackedItem = grid.Items.Count > trackedRowIndex ? grid.Items[trackedRowIndex] : null;

        // Never hold a row/cell reference across a call that might trigger a
        // visual-tree rebuild (selection, resize) — re-fetch fresh each time
        // via ItemContainerGenerator.ContainerFromItem (item-identity based).
        System.Windows.Controls.DataGridRow? FreshRow() => trackedItem is not null
            ? grid.ItemContainerGenerator.ContainerFromItem(trackedItem) as System.Windows.Controls.DataGridRow
            : null;

        (double x, double y) ScreenPos(Microsoft.UI.Xaml.FrameworkElement? e)
        {
            if (e is null) return (double.NaN, double.NaN);
            var p = e.TransformToVisual(grid).TransformPoint(new global::Windows.Foundation.Point(0, 0));
            return (p.X, p.Y);
        }

        var trackedRow = FreshRow();
        var frozenCell = trackedRow?.TryGetCell(0);
        var (frozenX, frozenY) = ScreenPos(frozenCell);

        // Confirm the visual-tree ancestry the routed BeginEditCommand relies on
        // (CommandBinding.AppliesTo walks VisualTreeHelper parents looking for a
        // DataGrid ancestor) actually reaches this grid instance. Root cause of
        // the previously-open "real cell editing" gap: holding a row/cell
        // reference across the SelectedItem assignment below (which retemplates
        // the row) left `editingCell` pointing at a detached subtree whose
        // ancestor walk stopped at DataGridRow instead of reaching grid.
        bool gridIsAncestor = false;
        if (frozenCell is not null)
        {
            for (Microsoft.UI.Xaml.DependencyObject? p = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(frozenCell); p is not null; p = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(p))
            {
                if (ReferenceEquals(p, grid)) { gridIsAncestor = true; break; }
            }
        }

        grid.SelectedItem = trackedItem;
        grid.UpdateLayout();
        var isSelected = FreshRow()?.IsSelected ?? false;

        // Re-fetch fresh (not the pre-selection `trackedRow`/`frozenCell`) — see note above.
        var editingCell = FreshRow()?.TryGetCell(1);
        var editingCellIsReadOnly = editingCell?.IsReadOnly;
        var beganEdit = editingCell is not null && editingCell.BeginEdit(null);
        var isEditingAfterBegin = editingCell?.IsEditing ?? false;

        if (editingCell is { IsEditing: true } && editingCell.EditingElement is Microsoft.UI.Xaml.Controls.TextBox box)
        {
            box.Text = "EDITED";
        }

        var committed = editingCell is not null && editingCell.CommitEdit();
        var isEditingAfterCommit = editingCell?.IsEditing ?? false;
        grid.UpdateLayout();
        var committedValue = (trackedItem as DataGridScenarios.FrozenEditRow)?.ColB;

        var lastFrozenColumn = grid.Columns[Math.Max(0, frozenColumnCount - 1)];
        var firstNonFrozenColumn = grid.Columns[Math.Min(grid.Columns.Count - 1, frozenColumnCount)];
        var resizedFrozen = grid.ShimTryResizeColumn(lastFrozenColumn, 40.0);
        var resizedNonFrozen = grid.ShimTryResizeColumn(firstNonFrozenColumn, 40.0);
        grid.UpdateLayout();
        var frozenCellAfterResize = FreshRow()?.TryGetCell(0);
        var (frozenXAfterResize, _) = ScreenPos(frozenCellAfterResize);

        return $"{{\"trackedRowFound\":{Jb(trackedRow is not null)},\"frozenX\":{Jn(frozenX)},\"frozenY\":{Jn(frozenY)}," +
               $"\"gridIsAncestor\":{Jb(gridIsAncestor)},\"isSelected\":{Jb(isSelected)}," +
               $"\"editingCellIsReadOnly\":{Jb(editingCellIsReadOnly ?? false)}," +
               $"\"beganEdit\":{Jb(beganEdit)},\"isEditingAfterBegin\":{Jb(isEditingAfterBegin)}," +
               $"\"committed\":{Jb(committed)},\"isEditingAfterCommit\":{Jb(isEditingAfterCommit)},\"committedValue\":{Js(committedValue)}," +
               $"\"resizedFrozen\":{Jb(resizedFrozen)},\"resizedNonFrozen\":{Jb(resizedNonFrozen)},\"frozenXAfterResize\":{Jn(frozenXAfterResize)}}}";
    });
}
#endif
