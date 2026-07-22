# DataGrid Port - Session 70

Date: 2026-06-13

## Goal

Execute the next notification-focused batch from session 69:

1. Verify `RowBackground` live updates through the linked WPF notification path.
2. Verify `GridLinesVisibility` notification fires safely even though shim grid-line visuals are not implemented.
3. Verify `CanUserResizeColumns` / `CanUserReorderColumns` notifications fire safely while header gripper/reorder UI remains deferred.

## What Changed

Only the sample probe changed. No new shim behavior was needed because sessions
67-69 already made the WPF DP callbacks live through `FrameworkPropertyMetadata`
and wired row/cell/header notification dispatch.

New probe steps:

- **row background: even rows update live**  
  Sets `grid.RowBackground`, forces row0 to be unselected, verifies row0 receives
  the brush through `OnNotifyRowPropertyChanged(target=Rows)` ->
  `DataGrid.NotifyPropertyChanged` -> row tracking -> `DataGridRow.NotifyPropertyChanged`
  -> `ApplyShimRowBackground`.

- **grid-lines notification fires without requiring grid-line visuals**  
  Toggles `GridLinesVisibility` through `None` and `All`, calls `UpdateLayout`,
  and verifies the value is retained. WPF's linked callback regenerates item
  containers; the shim still does not render grid lines.

- **column resize/reorder options notify without header gripper support**  
  Toggles `CanUserResizeColumns` and `CanUserReorderColumns` false/true and
  verifies assigned values survive. Header resize grippers and column reordering
  are still deferred.

## Probe Adjustment

The existing alternating-row-background probe was made more robust:

- It now selects a different row before checking row1 striping so row1's
  background is not controlled by the selected-row visual.
- It compares the actual applied solid-brush color instead of relying on brush
  reference identity; the Uno DP path can normalize brush instances.

## Verification

```
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj --framework net10.0-desktop --no-restore
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --no-restore
dotnet run --project src/LeXtudio.Windows.Sample/LeXtudio.Windows.Sample.csproj --framework net10.0-desktop -- --probe
```

Results:

- Build: 0 errors, 0 warnings in the incremental run.
- Tests: 136 passed, 0 failed.
- Probe: `DONE failures=0` (45 steps).

## Still Deferred

- Actual grid-line rendering.
- Header resize gripper / reorder UI.
- Frozen-column behavior in the shim layout.
- Full `DataGridRow` / `DataGridCell` upstream source link.

## Next Batch

Session 71 completed initial frozen-column state propagation for realized
headers/cells, without frozen layout.

Next preferred batch: another notification reuse batch covering `CellStyle`,
`RowStyle`, and `ColumnHeaderStyle` live updates. These are related enough to
batch together and avoid forcing the larger WPF panel/scrolling substrate.
