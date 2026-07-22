# DataGrid Port - Session 72

Date: 2026-06-13

## Goal

Batch the next notification-reuse work after session 71: `RowStyle`,
`CellStyle`, and `ColumnHeaderStyle` propagation to realized shim visuals.

## What Changed

- `DataGridRow` now tracks `ShimAppliedRowStyle` and applies it at row
  construction time.
- `DataGridCell` now tracks `ShimAppliedCellStyle` and applies it at cell
  construction time.
- `DataGridColumnHeader` now tracks `ShimAppliedColumnHeaderStyle` and applies
  it at header construction time.
- Live `CellStyle` changes reuse the linked WPF
  `OnNotifyColumnAndCellPropertyChanged` path and are forwarded through the
  existing row-to-cell shim dispatch.
- Live `ColumnHeaderStyle` changes reuse the linked WPF
  `OnNotifyColumnAndColumnHeaderPropertyChanged` path and the shim header
  dispatch from sessions 67 and 71.
- Live `RowStyle` changes now notify realized rows under `HAS_UNO`; upstream
  WPF only coerces `ItemContainerStyle`, but the shim's coercion path is a
  no-op without the full WPF item-container style engine.

The current target is style-object propagation, not setter application. The
project does not link WPF `Style.cs`; the public style type here is WinUI
`Microsoft.UI.Xaml.Style`, and the shim render path still owns visual property
application explicitly.

## Probe

Added one sample probe step:

- Creates distinct `Microsoft.UI.Xaml.Style` instances for row, cell, and
  column header target types.
- Assigns `RowStyle`, `CellStyle`, and `ColumnHeaderStyle` after the grid is
  realized.
- Verifies the realized row, first cell, and first column header receive the
  same style object by reference.
- Restores previous style values.

## Verification

```
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj --framework net10.0-desktop --no-restore
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --no-restore
dotnet run --project src/LeXtudio.Windows.Sample/LeXtudio.Windows.Sample.csproj --framework net10.0-desktop -- --probe
```

Results:

- Build: 0 errors, 0 warnings in the final incremental run.
- Tests: 136 passed, 0 failed.
- Probe: `DONE failures=0` (47 steps).

## Still Deferred

- Applying style setters to row/cell/header visuals.
- `RowStyleSelector` / `ItemContainerStyleSelector` behavior.
- Column-level cell style precedence.
- Full WPF item-container style engine.

## Next Batch

Prefer column-level style precedence and style-selector groundwork:

1. `DataGridColumn.CellStyle` / grid `CellStyle` precedence on realized cells.
2. `DataGridColumn.HeaderStyle` / grid `ColumnHeaderStyle` precedence on
   realized headers if the linked column base exposes the property cleanly.
3. If selector substrate is cheap, add a narrow `RowStyleSelector` probe;
   otherwise keep selector behavior deferred and move to grid-line visuals.
