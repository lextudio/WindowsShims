# DataGrid Port - Session 64

Date: 2026-06-13

## Goal

Continue the column-area cleanup after linking `DataGridColumn.cs`: reduce the
remaining `DataGridColumnCollection` shim by importing the WPF display-index
model while leaving layout/virtualization-heavy width computation deferred.

## What Changed

- Reworked local `DataGridColumnCollection` around WPF-style display-index map
  maintenance:
  - add/remove/replace/move update the map,
  - duplicate and out-of-range display indices are validated,
  - `ColumnFromDisplayIndex` / `ColumnIndexFromDisplayIndex` resolve through
    the map,
  - display-index changes notify selected-cell column collections,
  - frozen-column state is updated by display order.
- Added `SR.DataGrid_DuplicateDisplayIndex`.
- Changed the shim render path to snapshot visible columns in display-index
  order, then reuse that order for headers, cells, width passes, and cell
  selection identity.
- Changed `DataGridRow.BuildCells()` to use the owner grid's display-order
  column enumeration instead of raw collection order.
- Added a sample probe step that moves the Age column to display index 0 and
  verifies `ColumnFromDisplayIndex(0)`, the first header, and the first row cell
  all resolve to that column.

## Uno DP Gap

WPF normally reaches `DataGridColumnCollection.OnColumnDisplayIndexChanged`
through the `DisplayIndexProperty` changed callback. The Uno DP shim does not
reliably run that callback for direct property sets, so the collection refreshes
the display-index map from current column values when the render/map boundary
asks for display order. This preserves direct `column.DisplayIndex = ...`
behavior without adding a parallel local property system.

## Still Deferred

The full upstream `DataGridColumnCollection.cs` is still not linked. Its
remaining large regions depend on WPF layout/virtualization internals:
realized-column block lists, cells-panel scrolling, star/auto redistribution,
and delayed width computation. The current session intentionally kept those
methods as local stubs because the existing shim width pass already owns the
runtime behavior verified by the probe.

## Verification

```
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj --framework net10.0-desktop --no-restore
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --no-restore
dotnet run --project src/LeXtudio.Windows.Sample/LeXtudio.Windows.Sample.csproj --framework net10.0-desktop -- --probe
```

Results:

- Build: 0 errors (existing warnings only).
- Tests: 126 passed, 0 failed.
- Probe: `DONE failures=0`.

## Next Batch

1. Batch row/cell container cleanup next: upstream `DataGridCell` edit/current
   cell surfaces and `DataGridRow` container state are now a better target than
   forcing the width/virtualization-heavy remainder of
   `DataGridColumnCollection`.
2. Return to column collection width internals only when the shim width pass is
   ready to be replaced by upstream measure/realization behavior.
