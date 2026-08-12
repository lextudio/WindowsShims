using System.Text.Json;
using Xunit;

namespace DataGrid.IntegrationTests;

// Session 127: DataGrid.RowHeight / MinRowHeight must reach the realized rows
// in manual (non-virtualized) mode. WPF flows these through the CellsPresenter
// coercion chain, which the manual path lacks, so the shim re-applies heights
// on RowHeight/MinRowHeight changes and again once each row's template (and
// thus its cells) exists.
[Collection("DataGrid app")]
public sealed class DataGridRowHeightTests
{
    readonly DataGridAppFixture _app;
    public DataGridRowHeightTests(DataGridAppFixture app) => _app = app;

    [Fact]
    public async Task RowHeights_RowHeightAppliesToAllRealizedRows()
    {
        await _app.InvokeAsync("datagrid.probe.create-grid");
        var state = await _app.InvokeAsync("datagrid.probe.set-row-height", 50.0, 0.0);
        var raw = state.ToString();

        Assert.Equal(21, state.GetProperty("rowCount").GetInt32());
        Assert.Equal(50.0, state.GetProperty("cellHeightAfterLayout").GetDouble());
        var actual = state.GetProperty("actualHeights").EnumerateArray()
            .Select(h => h.GetDouble()).ToList();
        Assert.All(actual, h => Assert.Equal(50.0, h));
    }

    [Fact]
    public async Task RowHeights_MinRowHeightRaisesRowsAboveRowHeight()
    {
        await _app.InvokeAsync("datagrid.probe.create-grid");
        var state = await _app.InvokeAsync("datagrid.probe.set-row-height", 50.0, 100.0);
        var raw = state.ToString();

        Assert.Equal(100.0, state.GetProperty("cellMinAfterLayout").GetDouble(), 3);
        var actual = state.GetProperty("actualHeights").EnumerateArray()
            .Select(h => h.GetDouble()).ToList();
        Assert.All(actual, h => Assert.Equal(100.0, h, 3));
    }

    [Fact]
    public async Task RowHeights_ResetToAutoRestoresContentSizedRows()
    {
        await _app.InvokeAsync("datagrid.probe.create-grid");
        await _app.InvokeAsync("datagrid.probe.set-row-height", 50.0, 0.0);
        var state = await _app.InvokeAsync("datagrid.probe.set-row-height", -1.0, 0.0);
        var raw = state.ToString();

        Assert.Equal("\"NaN\"", state.GetProperty("rowHeight").GetRawText());
        Assert.Equal("\"NaN\"", state.GetProperty("cellHeightAfterLayout").GetRawText());
        var actual = state.GetProperty("actualHeights").EnumerateArray()
            .Select(h => h.GetDouble()).ToList();
        Assert.All(actual, h => Assert.True(h > 20 && h < 40, $"row should return to content size, got {h}: {raw}"));
    }
}
