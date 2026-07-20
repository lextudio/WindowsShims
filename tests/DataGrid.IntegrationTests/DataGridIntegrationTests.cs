using System.Text.Json;

using Xunit;

namespace DataGrid.IntegrationTests;

// DataGrid behavioral tests ported from Roma.IntegrationTests.
// These exercise the linked WPF DataGrid shim through DevFlow probe actions
// against a standalone DataGrid (no ILSpy metadata dependency).
[Collection("DataGrid app")]
public sealed class DataGridIntegrationTests
{
    readonly DataGridAppFixture _app;
    public DataGridIntegrationTests(DataGridAppFixture app) => _app = app;

    static int Rows(JsonElement s) => s.GetProperty("rows").GetInt32();
    static int Columns(JsonElement s) => s.GetProperty("columns").GetInt32();
    static bool HasGrid(JsonElement s) => s.GetProperty("hasGrid").GetBoolean();

    [Fact]
    public async Task CreateGrid_RendersWithColumns()
    {
        var state = await _app.InvokeAsync("datagrid.probe.create-grid");

        Assert.True(HasGrid(state), "DataGrid should exist after create-grid");
        Assert.True(Rows(state) >= 1, $"expected >=1 row, got {Rows(state)}");
        Assert.True(Columns(state) >= 1, $"expected >=1 column, got {Columns(state)}");
    }

    [Fact]
    public async Task State_ReturnsGridSnapshot()
    {
        await _app.InvokeAsync("datagrid.probe.create-grid");
        var state = await _app.InvokeAsync("datagrid.probe.state");

        Assert.True(HasGrid(state));
        Assert.True(Rows(state) >= 1);
        Assert.True(Columns(state) >= 1);
    }

    [Fact]
    public async Task ColumnResize_ChangesWidth()
    {
        await _app.InvokeAsync("datagrid.probe.create-grid");
        var state = await _app.InvokeAsync("datagrid.probe.resize-column", 0, 40.0);

        var raw = state.ToString();
        Assert.False(state.TryGetProperty("error", out _), $"resize failed: {raw}");
        Assert.True(HasGrid(state));
        Assert.True(state.GetProperty("resized").GetBoolean(), $"resize should be accepted: {raw}");
        Assert.True(state.GetProperty("after").GetDouble() > state.GetProperty("before").GetDouble(),
            $"column should grow: {raw}");
    }

    [Fact]
    public async Task ColumnResizeViaShim_ChangesWidth()
    {
        await _app.InvokeAsync("datagrid.probe.create-grid");
        var state = await _app.InvokeAsync("datagrid.probe.resize-via-shim", 0, 20.0);

        var raw = state.ToString();
        Assert.False(state.TryGetProperty("error", out _), $"shim resize failed: {raw}");
        Assert.True(HasGrid(state));
        Assert.True(state.GetProperty("resized").GetBoolean(), $"ShimTryResizeColumn should accept delta: {raw}");
        Assert.True(state.GetProperty("after").GetDouble() > state.GetProperty("before").GetDouble(),
            $"column should grow: {raw}");
    }

    [Fact]
    public async Task ColumnAutoSize_ExpandsBestFitWidth()
    {
        await _app.InvokeAsync("datagrid.probe.create-grid");
        var state = await _app.InvokeAsync("datagrid.probe.autosize-column", 0);

        var raw = state.ToString();
        Assert.False(state.TryGetProperty("error", out _), $"auto-size failed: {raw}");
        Assert.True(HasGrid(state));
        Assert.True(state.GetProperty("resized").GetBoolean(), $"auto-size should be accepted: {raw}");
        Assert.True(state.GetProperty("after").GetDouble() > state.GetProperty("before").GetDouble(),
            $"column should grow: {raw}");
    }

    [Fact]
    public async Task LeftHeaderEdge_ResizesPreviousColumn()
    {
        await _app.InvokeAsync("datagrid.probe.create-grid");
        var state = await _app.InvokeAsync("datagrid.probe.resize-left-edge", 1, 100.0);

        var raw = state.ToString();
        Assert.False(state.TryGetProperty("error", out _), $"left-edge resize failed: {raw}");
        Assert.True(HasGrid(state));
        Assert.Equal(0, state.GetProperty("columnIndex").GetInt32());
        Assert.True(state.GetProperty("resized").GetBoolean(), $"left edge resize should target the previous column: {raw}");
    }

    [Fact]
    public async Task HeaderGripperDrag_ChangesWidth()
    {
        await _app.InvokeAsync("datagrid.probe.create-grid");
        var state = await _app.InvokeAsync("datagrid.probe.gripper-resize-column", 0, 40.0);

        var raw = state.ToString();
        Assert.False(state.TryGetProperty("error", out _), $"gripper resize failed: {raw}");
        Assert.True(HasGrid(state));
        Assert.True(state.GetProperty("resized").GetBoolean(), $"gripper drag should be accepted: {raw}");
        Assert.True(state.GetProperty("after").GetDouble() > state.GetProperty("before").GetDouble(),
            $"column should grow: {raw}");
    }

    [Fact]
    public async Task HeaderGripperDoubleClick_AutoSizesWidth()
    {
        await _app.InvokeAsync("datagrid.probe.create-grid");
        var state = await _app.InvokeAsync("datagrid.probe.gripper-autosize-column", 0);

        var raw = state.ToString();
        Assert.False(state.TryGetProperty("error", out _), $"gripper auto-size failed: {raw}");
        Assert.True(HasGrid(state));
        Assert.True(state.GetProperty("resized").GetBoolean(), $"gripper double-click should auto-size: {raw}");
        Assert.True(state.GetProperty("after").GetDouble() > state.GetProperty("before").GetDouble(),
            $"column should grow: {raw}");
    }

    [Fact]
    public async Task HeaderGripperDoubleClick_ShrinksWideColumnToBestFit()
    {
        await _app.InvokeAsync("datagrid.probe.create-grid");
        var state = await _app.InvokeAsync("datagrid.probe.gripper-autosize-wide-column", 0);

        var raw = state.ToString();
        Assert.False(state.TryGetProperty("error", out _), $"wide auto-size failed: {raw}");
        Assert.True(HasGrid(state));
        Assert.True(state.GetProperty("resized").GetBoolean(), $"gripper double-click should auto-size a wide column: {raw}");
        Assert.True(state.GetProperty("after").GetDouble() < state.GetProperty("before").GetDouble(),
            $"wide column should shrink: {raw}");
    }

    [Fact]
    public async Task CopySelectedRow_ProducesSelectionState()
    {
        await _app.InvokeAsync("datagrid.probe.create-grid");
        var state = await _app.InvokeAsync("datagrid.probe.copy-selection");

        var raw = state.ToString();
        Assert.False(state.TryGetProperty("error", out _), $"copy probe failed: {raw}");
        Assert.True(HasGrid(state), $"DataGrid should render: {raw}");
        Assert.True(state.GetProperty("copied").GetBoolean(), $"copy should select a row: {raw}");
        Assert.True(state.GetProperty("selectedItems").GetInt32() > 0, $"probe should select a row: {raw}");
    }

    [Fact]
    public async Task KeyboardSelection_SelectsCellsAndMovesCurrentCell()
    {
        await _app.InvokeAsync("datagrid.probe.create-grid");
        var state = await _app.InvokeAsync("datagrid.probe.keyboard-selection");

        var raw = state.ToString();
        Assert.False(state.TryGetProperty("error", out _), $"keyboard selection probe failed: {raw}");
        Assert.True(HasGrid(state), $"DataGrid should render: {raw}");
        Assert.True(state.GetProperty("selected").GetBoolean(), $"select should be accepted: {raw}");
        Assert.True(state.GetProperty("selectedCells").GetInt32() > 0, $"should populate selected cells: {raw}");
    }

    [Fact]
    public async Task FilterGrid_HasFilterButtons()
    {
        await _app.InvokeAsync("datagrid.probe.create-filter-grid");
        var state = await _app.InvokeAsync("datagrid.probe.filter-buttons");

        var raw = state.ToString();
        Assert.True(HasGrid(state), $"DataGrid should render: {raw}");
        Assert.True(state.GetProperty("autoFilterEnabled").GetBoolean(), $"auto-filter should be enabled: {raw}");

        var buttons = state.GetProperty("hasFilterButtons").EnumerateArray()
            .Select(b => b.GetBoolean())
            .ToArray();
        Assert.NotEmpty(buttons);
        Assert.All(buttons, b => Assert.True(b, $"all columns should have filter buttons: {raw}"));
    }

    [Fact]
    public async Task HexFilter_ReducesRows()
    {
        await _app.InvokeAsync("datagrid.probe.create-hex-filter-grid");
        var state = await _app.InvokeAsync("datagrid.probe.hex-filter-apply", "0001");

        var raw = state.ToString();
        Assert.True(HasGrid(state), $"DataGrid should render: {raw}");
        Assert.True(state.GetProperty("beforeRows").GetInt32() > state.GetProperty("afterRows").GetInt32(),
            $"HEX filter should reduce visible rows: {raw}");
        Assert.True(state.GetProperty("afterRows").GetInt32() > 0,
            $"HEX filter should keep matching rows: {raw}");
        Assert.False(string.IsNullOrWhiteSpace(state.GetProperty("filterText").GetString()),
            $"HEX filter text should be reported: {raw}");
    }

    [Fact]
    public async Task HexFilterClear_RestoresRows()
    {
        await _app.InvokeAsync("datagrid.probe.create-hex-filter-grid");
        var before = await _app.InvokeAsync("datagrid.probe.hex-filter-apply", "0001");
        var beforeRows = before.GetProperty("afterRows").GetInt32();

        var after = await _app.InvokeAsync("datagrid.probe.hex-filter-clear");

        var afterRows = after.GetProperty("rows").GetInt32();
        Assert.True(afterRows > beforeRows,
            $"clearing filter should restore rows: after={afterRows} before={beforeRows}");
    }

    [Fact]
    public async Task RowDetails_RendersNestedDataGrid()
    {
        await _app.InvokeAsync("datagrid.probe.create-row-details-grid");
        var state = await _app.InvokeAsync("datagrid.probe.row-details-state");

        var raw = state.ToString();
        Assert.True(HasGrid(state), $"outer DataGrid should render: {raw}");
        Assert.True(state.GetProperty("rows").GetInt32() > 0, $"outer grid should contain rows: {raw}");
        Assert.True(state.GetProperty("hasSelector").GetBoolean(), $"row-details selector should be present: {raw}");
        Assert.True(state.GetProperty("detailsRendered").GetBoolean(), $"row details should be rendered: {raw}");
        Assert.True(state.GetProperty("detailsGrid").GetBoolean(), $"row details should render nested DataGrid: {raw}");
        Assert.True(state.GetProperty("detailsRows").GetInt32() > 0, $"nested details grid should contain rows: {raw}");
        Assert.True(state.GetProperty("detailsColumns").GetInt32() > 0, $"nested details grid should contain columns: {raw}");
    }

    [Fact]
    public async Task ColumnWidths_AreReasonable()
    {
        await _app.InvokeAsync("datagrid.probe.create-grid");
        var state = await _app.InvokeAsync("datagrid.probe.column-widths");

        var raw = state.ToString();
        Assert.True(HasGrid(state), $"DataGrid should render: {raw}");

        var widths = state.GetProperty("columnWidths").EnumerateArray()
            .Select(w => w.GetDouble())
            .ToArray();
        Assert.NotEmpty(widths);
        foreach (var (w, i) in widths.Select((w, i) => (w, i)))
        {
            Assert.True(w > 20, $"column [{i}] width {w} should be > 20px: {raw}");
        }
    }
}
