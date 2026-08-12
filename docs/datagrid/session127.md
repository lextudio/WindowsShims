# Session 127 — DataGrid.RowHeight / MinRowHeight reach realized rows

Date: 2026-08-12. Status: done, DataGrid suite 66/66 + RichTextBox 238/238 +
model tests 234/234 green.

## What was wrong

`DataGrid.RowHeight` / `MinRowHeight` had no effect in manual
(non-virtualized) mode. WPF applies these through the CellsPresenter coercion
chain:

1. `DataGrid.RowHeightProperty` (default NaN) / `MinRowHeightProperty`
   (default 0) metadata carries `OnNotifyCellsPresenterPropertyChanged`
   (DataGrid.cs:479).
2. That raises `NotifyPropertyChanged(CellsPresenter)` (DataGrid.cs:598) on
   each row.
3. `DataGridCellsPresenter` overrides `HeightProperty` /
   `MinHeightProperty` metadata with `OnCoerceHeight` (line 192) /
   `OnCoerceMinHeight` (line 203), coercing via
   `DataGridHelper.GetCoercedTransferPropertyValue` to the grid's
   RowHeight/MinRowHeight.

The manual path has no CellsPresenter, so the chain was severed: cells stayed
content-sized (~32.5px) regardless of RowHeight.

## Fix

Re-apply the heights onto the realized rows directly, in two places:

- `DataGrid.ShimEnsureRowHeightHook()` — lazy once-off
  `RegisterPropertyChangedCallback` on `RowHeightProperty` and
  `MinRowHeightProperty`, forwarding to `ShimApplyRowHeightsToRealizedRows()`
  which walks `ItemContainerGenerator.Containers.OfType<DataGridRow>()`.
  Called from `BuildShimVisualTree`.
- `DataGridRow.OnApplyTemplate()` — re-applies via
  `DataGrid.ShimApplyRowHeightsToRow` once `PART_CellsHost` actually exists.
  This second hook is load-bearing: the manual path calls
  `ShimDecorateRow` *before* the row's template is applied, so at decorate
  time `EffectiveCells()` is empty and the height application is a no-op.
  `UpdateLayout` (and any other rebuild) creates rows whose cells only
  materialize inside `OnApplyTemplate`.

`DataGridRow.ShimApplyRowHeights(owner)` iterates `EffectiveCells()`;
`DataGridCell.ShimApplyRowHeight(rowHeight, minRowHeight)` sets
`Height = rowHeight` (or `ClearValue(HeightProperty)` for NaN → auto) and
`MinHeight = max(32, MinRowHeight)` when MinRowHeight > 0, else 32.

## Debugging trail (why the second hook was needed)

Initial probe (`datagrid.probe.row-heights` / `set-row-height`) showed:

- `RowHeight = 50` → property-change callback fired, all 147 cells written
  with Height=50 immediately after the set (cells read back 50).
- But after `grid.UpdateLayout()`, every cell read `NaN` again, and cell
  instance IDs jumped 442 → 589 (≈ 21 rows × 7 columns = 147 fresh
  instances): `UpdateLayout` had rebuilt the rows, and the fresh cells never
  got the height.
- `ShimDecorateRow`-time instrumentation (`ShimDecorateCellsTotal`) proved
  the rebuild path also decorates rows with **zero** cells (`EffectiveCells`
  empty) — the decorate-time application was a silent no-op, and the
  `RowHeight`-change callback had already run against the old instances.

Fix = re-apply from `OnApplyTemplate`, which runs after the cells exist.

## Tests

`tests/DataGrid.IntegrationTests/DataGridRowHeightTests.cs` (3 tests):

- `RowHeights_RowHeightAppliesToAllRealizedRows` — RowHeight=50 → all 21
  rows actual height 50 (create-grid has 20 items + placeholder row).
- `RowHeights_MinRowHeightRaisesRowsAboveRowHeight` — MinRowHeight=100 over
  RowHeight=50 → rows 100.
- `RowHeights_ResetToAutoRestoresContentSizedRows` — back to NaN → rows
  content-sized again.

Probe protocol notes:

- `datagrid.probe.set-row-height(rowHeight, minRowHeight)` accepts `-1` as a
  sentinel for NaN (JSON can't carry NaN via System.Text.Json).
- `Jn` renders NaN as the string `"NaN"` — assertions must use
  `GetRawText()`/`GetString()` comparisons, not `GetDouble()`.

Diagnostic scaffolding (ShimDecorateCount/ShimDecorateCellsTotal,
ShimApplyFrom*Count, ShimRowHeight*Seen, cell instance IDs, apply log) was
removed after the fix; `ShimRowHeightApplyCount` etc. were also dropped from
the row-heights probe's JSON.

## Notes for future sessions

- The stale collapse-root-cause comment above
  `FrozenColumns_TrackedRowKeepsFrozenXAcrossVerticalScroll` (DataGridIntegrationTests.cs:394)
  predates this fix; rows no longer collapse, so the comment can be dropped
  on the next touch of that file.
- MinRowHeight floor: WPF's effective floor is the current font size; this
  shim uses a fixed 32px floor for the default (non-cells-presenter) path.
  Matches the shim's existing 32px default min-height convention.
