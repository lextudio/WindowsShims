# Session 128 — Cell editing under the full presenter-hosted stack

Date: 2026-08-12. Status: done, DataGrid suite 67/67 + RichTextBox 238/238 +
model tests 234/234 green.

## What was wrong

todo.md item 12: editing with the *header* presenter + cells presenter +
virtualized rows combination had no coverage. The existing
`FrozenColumns_RealCellEditCommits` only enabled `ShimSetCellsPresenterHost`;
Roma's metadata grids run the full presenter stack.

## Finding while wiring the combination

The first run of the new combination failed with `gridIsAncestor: false`,
`beganEdit: false`. Diagnostics showed a split-brain generator registry:

- `ItemContainerGenerator.ContainerFromItem` (used by the probe's
  `FreshRow()`) resolved the tracked row to an instance parented under the
  manual path's `StackPanel` (`PART_ShimRowsHost` of the non-virtualized
  template), with no `CellsPresenter` and zero cells.
- The live virtualized rows (under `DataGridRowsPresenter`) had cells
  presenters and 4 realized cells each, but a different row instance.

Root cause: the virtualized branch of `BuildShimVisualTree` (`host is
DataGridRowsPresenter`, DataGrid.cs:335) skipped
`ItemContainerGenerator.ResetContainers()` — the manual branch (DataGrid.cs:372)
resets the registry before rebuilding, the virtualized branch didn't. So
containers registered by an earlier manual pass (or an earlier virtualized
window) lingered, and `ContainerFromItem` hit the stale entry.

Fix: reset the registry in the virtualized branch too, before
`InternalColumns.RefreshDisplayIndexMap()`.

## Test

`FrozenColumns_RealCellEditCommitsUnderHeaderPresenter`:
`datagrid.probe.create-frozen-edit-grid(1, 1)` builds the 40-row editable
grid with `ShimSetCellsPresenterHost(true)` + `ShimSetRowVirtualization(true)`
+ `ShimSetHeaderPresenterHost(true)` + `ShimForceViewport(0, 400)`, then the
existing `frozen-edit-readback` flow runs selection, BeginEdit → "EDITED" →
CommitEdit, and boundary resize.

Probe change: `create-frozen-edit-grid` gained an optional `headerPresenter`
int parameter (default 0); when non-zero it enables virtualization + header
presenter + a forced 400px viewport on the rows presenter (headless Skia
never fires EffectiveViewportChanged).

## Notes for future sessions

- `ContainerFromItem` returning stale containers after a template/path switch
  is now guarded by the virtualized-branch reset; if any future test shows a
  phantom row, check `ItemContainerGenerator.Containers` for entries from a
  previous render path.
- Header-presenter + editing works: the routed BeginEditCommand's ancestry
  walk reaches the grid through presenter-hosted cells.
