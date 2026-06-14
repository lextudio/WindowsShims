# DataGrid Port - Session 96

Goal: replace the fake `DataGridCellsPanel` Uno placeholders with the next real
WPF reuse slice: items-host wiring, realized-column validation, and child-sync
bookkeeping.

## Changes

- Added missing WPF-shaped `Panel` substrate:
  - `IsItemsHost`
  - `InternalChildren`
  - `OnIsItemsHostChanged`
- Added missing `VirtualizingPanel` helpers:
  - `GetVirtualizationMode` / `SetVirtualizationMode`
  - `AddInternalChild`
  - `InsertInternalChild`
  - `RemoveInternalChildRange`
- Kept the upstream WPF `DataGridCellsPanel.cs` linked, but removed the Uno
  placeholder `HasCorrectRealizedColumns => true`.
- Added a Uno partial `DataGridCellsPanel.uno.cs` that ports the real WPF
  light-weight panel logic now supported by the shim substrate:
  - `HasCorrectRealizedColumns`
  - `OnIsItemsHostChanged`
  - `OnItemsChanged` remove/replace/move handling
  - `OnClearChildren`
  - virtualization-state sync from the parent presenter
  - realized-children bookkeeping
  - parent presenter / parent DataGrid resolution

## Why This Slice

This is the next useful reuse rung below the fully guarded panel body:

- it is real WPF behavior,
- it is already consumed by linked presenters,
- and it does not require the deep generator/measure/scroll/frozen-column
  engine yet.

That removes one more fake success path and gives future panel work the actual
host/validation plumbing to build on.

## Still Deferred

- Real WPF `DataGridCellsPanel.MeasureOverride` / `ArrangeOverride`
- Column generation/recycling sessions
- Frozen-column clipping/layout
- Horizontal bring-into-view / scroll-info retry path
- Full replacement of the manual shim rows host

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
