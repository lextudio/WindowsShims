# DataGrid Port - Session 71

Date: 2026-06-13

## Goal

Execute the frozen-column batch from session 70 while staying source-first:
reuse the linked WPF `FrozenColumnCount` / `DataGridColumnCollection`
notification path and add only the Uno-side state reflection needed by the
current shim render tree.

## What Changed

- `DataGridCell` and `DataGridColumnHeader` now expose an `IsFrozen` marker for
  realized shim visuals.
- Header and cell construction initializes the marker during `BuildHeaderRow`
  and `DataGridRow.BuildCells`.
- Live `FrozenColumnCount` changes now reach shim column headers by extending
  the `HAS_UNO` header notification branch in linked `DataGrid.cs` to include
  `ColumnHeadersPresenter` notifications.
- Cells and headers update their marker from `column.DisplayIndex <
  owner.FrozenColumnCount`, falling back to `Column.IsFrozen` only when
  detached.

The direct `DisplayIndex` / `FrozenColumnCount` calculation is intentional.
Linked WPF `DataGrid.NotifyPropertyChanged` forwards the
`FrozenColumnCountProperty` notification to realized rows/cells before the
linked `DataGridColumnCollection` target updates `DataGridColumn.IsFrozen`.
Using the owner count keeps realized cell/header state correct while still
letting the linked collection own column frozen-state updates.

## Probe

Added a sample probe step:

- Sets `FrozenColumnCount = 2`.
- Verifies the first two display columns report `DataGridColumn.IsFrozen`.
- Verifies the first two realized headers and cells report `IsFrozen`.
- Restores the previous `FrozenColumnCount`.

This pins the live notification path without claiming frozen layout support.

## Verification

```
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj --framework net10.0-desktop --no-restore
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --no-restore
dotnet run --project src/LeXtudio.Windows.Sample/LeXtudio.Windows.Sample.csproj --framework net10.0-desktop -- --probe
```

Results:

- Build: 0 errors, existing warning set.
- Tests: 136 passed, 0 failed.
- Probe: `DONE failures=0` (46 steps).

## Still Deferred

- True frozen-column layout during horizontal scrolling.
- Clipping / z-order behavior for frozen and scrolling column regions.
- Upstream `DataGridCellsPanel` layout reuse.
- Header grippers and real column reordering UI.

## Next Batch

Prefer a style-notification reuse batch before attempting frozen layout:

1. `CellStyle` live updates to realized cells.
2. `RowStyle` live updates to realized rows.
3. `ColumnHeaderStyle` live updates to realized headers.

These are related notification paths and should batch well without forcing the
larger WPF panel/scrolling substrate yet.
