using System.Linq;

namespace System.Windows.Controls;

// Session 65: the upstream DataGridColumnCollection is now linked, contributing
// the real display-index model, frozen-column handling, notification
// propagation, and hidden-column helpers. The width-computation / column-resize
// / star-distribution / column-virtualization regions are fork-guarded out
// (#if !HAS_UNO) because the shim render path owns width behavior and there is
// no virtualizing cells panel. This partial supplies the members those guarded
// regions defined, as light stubs, so the kept regions and the linked DataGrid /
// DataGridColumn code still bind.
internal partial class DataGridColumnCollection
{
    // WPF maintains the display-index map incrementally via the
    // DisplayIndexProperty changed-callback. The Uno DP shim does not reliably
    // fire that callback for direct `column.DisplayIndex = n` sets, so the render
    // path calls this to rebuild the map from current column values on demand.
    internal void RefreshDisplayIndexMap()
    {
        _displayIndexMapInitialized = false;
        // Clear the backing field directly: the DisplayIndexMap *property* getter
        // lazily re-initializes when the flag is false, which would rebuild (and
        // set the flag) before our explicit init runs. Upstream asserts the map
        // is empty before (re)initializing.
        _displayIndexMap.Clear();
        InitializeDisplayIndexMap(null, -1, out _);
    }

    // ── Star / width-computation flags ───────────────────────────────────────
    internal bool ColumnWidthsComputationPending { get; set; }

    // The shim has no realized-column width cache; report star presence directly.
    // The upstream Reset path assigns this (to invalidate); the assignment is
    // ignored because the value is derived live from the columns.
    internal bool HasVisibleStarColumns
    {
        get => this.Any(c => c.IsVisible && c.Width.IsStar);
        set { }
    }

    internal double AverageColumnWidth
    {
        get
        {
            var visible = this.Where(c => c.IsVisible).ToArray();
            return visible.Length == 0 ? 0.0 : visible.Average(c => c.ActualWidth > 0 ? c.ActualWidth : c.Width.DisplayValue);
        }
    }

    internal void InvalidateAverageColumnWidth() { }

    internal void InvalidateHasVisibleStarColumns() { }

    internal void InvalidateColumnWidthsComputation() { }

    // invalidateForNonVirtualizedRows is irrelevant without virtualization.
    internal void InvalidateColumnRealization(bool invalidateForNonVirtualizedRows) { }

    private void OnCellsPanelHorizontalOffsetChanged(DependencyPropertyChangedEventArgs e) { }

    // ── Column-resize / redistribution (shim width pass owns the real math) ──
    internal void OnColumnResizeStarted() { }

    internal void OnColumnResizeCompleted(bool cancel) { }

    internal void RecomputeColumnWidthsOnColumnResize(
        DataGridColumn resizingColumn, double horizontalChange, bool retainAuto) { }

    internal void RedistributeColumnWidthsOnAvailableSpaceChange(
        double availableSpaceChange, double newTotalAvailableSpace) { }

    internal void RedistributeColumnWidthsOnWidthChangeOfColumn(
        DataGridColumn changedColumn, DataGridLength oldWidth) { }

    internal void RedistributeColumnWidthsOnMinWidthChangeOfColumn(
        DataGridColumn changedColumn, double oldMinWidth) { }

    internal void RedistributeColumnWidthsOnMaxWidthChangeOfColumn(
        DataGridColumn changedColumn, double oldMaxWidth) { }

    // ── Realized-column block lists (virtualization not bridged) ─────────────
    // The linked cells panel owns the typed block model. These lists are still
    // only populated if that panel becomes the live items host.
    internal List<RealizedColumnsBlock>? RealizedColumnsBlockListForNonVirtualizedRows { get; set; }

    internal List<RealizedColumnsBlock>? RealizedColumnsDisplayIndexBlockListForNonVirtualizedRows { get; set; }

    internal List<RealizedColumnsBlock>? RealizedColumnsBlockListForVirtualizedRows { get; set; }

    internal List<RealizedColumnsBlock>? RealizedColumnsDisplayIndexBlockListForVirtualizedRows { get; set; }

    internal bool RebuildRealizedColumnsBlockListForNonVirtualizedRows { get; set; }

    internal bool RebuildRealizedColumnsBlockListForVirtualizedRows { get; set; }
}
