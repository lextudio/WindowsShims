# DataGrid Port - Session 97

Goal: move the next batch of `DataGridCellsPanel` state/helpers out of the fake
Uno path and into real WPF-shaped reusable substrate.

## Changes

- Extended `DataGridCellsPanel.uno.cs` with the WPF realized-column state
  accessors:
  - `RealizedColumnsBlockList`
  - `RealizedColumnsDisplayIndexBlockList`
  - `RebuildRealizedColumnsBlockList`
- Ported the linked WPF helper logic for:
  - `UpdateRealizedBlockLists(...)`
  - `BuildRealizedColumnsBlockList(...)`
- Added the panel helpers already queried through linked `DataGridHelper`:
  - `ComputeCellsPanelHorizontalOffset()`
  - `GetFrozenClipForChild(UIElement)`

## Why This Slice

This keeps reducing the amount of fake panel state on Uno while staying short
of the deep WPF generator/measure engine. The grid and presenters can now read
the same realized-column bookkeeping shape the upstream panel expects, and the
offset/clip helper calls no longer depend on missing guarded members.

## Still Deferred

- Full `MeasureOverride` / `ArrangeOverride` from upstream `DataGridCellsPanel`
- Child generation and recycling sessions
- Frozen-column arrange-time clip population
- Bring-column-into-view scrolling loop
- Swapping out the manual shim rows host for panel-driven item hosting

## Verification

```bash
dotnet build WindowsShims/src/LeXtudio.Windows/LeXtudio.Windows.csproj --nologo -v:minimal /clp:ErrorsOnly
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/ --nologo -v:minimal
dotnet run --project WindowsShims/src/LeXtudio.Windows.Sample/LeXtudio.Windows.Sample.csproj --framework net10.0-desktop -- --probe
```

Results:
- Build: 0 errors.
- Tests: 136/136 passed.
- Probe: `DONE failures=0`.
