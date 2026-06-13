# DataGrid Port - Session 73

Date: 2026-06-13

## Goal

Continue the style reuse batch by adding column-level style precedence for
realized cells and column headers.

## What Changed

- `DataGridCell.ApplyShimCellStyle()` now computes effective style as
  `Column.CellStyle ?? DataGrid.CellStyle`.
- `DataGridColumnHeader.ApplyShimColumnHeaderStyle()` now computes effective
  style as `Column.HeaderStyle ?? DataGrid.ColumnHeaderStyle`.
- `DataGridCell.NotifyPropertyChanged` now responds to
  `DataGridColumn.CellStyleProperty` as well as `DataGrid.CellStyleProperty`.
- `DataGridColumnHeader.NotifyPropertyChanged` now responds to
  `DataGridColumn.HeaderStyleProperty` as well as
  `DataGrid.ColumnHeaderStyleProperty`.

This reuses the linked `DataGridColumn` notification callbacks:

- `DataGridColumn.CellStyle` -> `OnNotifyCellPropertyChanged` ->
  `DataGridNotificationTarget.Columns | Cells`.
- `DataGridColumn.HeaderStyle` -> `OnNotifyColumnHeaderPropertyChanged` ->
  `DataGridNotificationTarget.Columns | ColumnHeaders`.

The shim still computes effective style directly because
`DataGridHelper.TransferProperty` is intentionally a no-op under the current
Uno property bridge.

## Probe

Added one sample probe step:

- Sets grid-level `CellStyle` and `ColumnHeaderStyle`.
- Sets column-level `CellStyle` / `HeaderStyle` on the first realized display
  column.
- Verifies the realized cell/header use the column style by reference.
- Clears the column-level styles.
- Verifies the realized cell/header fall back live to the grid styles.

## Verification

```
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj --framework net10.0-desktop --no-restore
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --no-restore
dotnet run --project src/LeXtudio.Windows.Sample/LeXtudio.Windows.Sample.csproj --framework net10.0-desktop -- --probe
```

Results:

- Build: 0 errors, existing full-build warning set.
- Tests: 136 passed, 0 failed.
- Probe: `DONE failures=0` (48 steps).

## Still Deferred

- Applying style setters to row/cell/header visuals.
- `RowStyleSelector` / `ItemContainerStyleSelector` behavior.
- Full WPF transfer-property coercion semantics.
- Editing element style-marker coverage for generated edit controls.

## Next Batch

Two practical options:

1. Add narrow style-selector substrate (`RowStyleSelector`) if the existing
   `StyleSelector` bridge can be exercised cheaply.
2. Move to visible grid-line rendering for `GridLinesVisibility`, now that the
   notification path is already pinned.
