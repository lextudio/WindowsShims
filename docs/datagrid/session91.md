# DataGrid Port - Session 91

Goal: take the next high-leverage WPF reuse step after row/header/details ports.

## Choice

The next best reuse target was `DataGridCellsPresenter`.

It sits between the linked `DataGridRow`/`DataGridCell` types and the still-local
layout layer. Reusing it pulls in WPF's column-count mirroring,
cell-container tracking, column-change synchronization, and notification
fan-out without forcing the larger `DataGridCellsPanel` virtualization rewrite.

## Changes

- Linked upstream WPF `Primitives/DataGridCellsPresenter.cs`.
- Linked upstream WPF `MultipleCopiesCollection.cs`, the presenter's repeated
  row-item source used to mirror the column collection.
- Removed the local `DataGridCellsPresenter` stub from
  `Primitives/DataGridPresenters.cs`.
- Added narrow bridge support:
  - `ItemsControl.FocusableProperty` metadata identity.
  - `DataGridCellsPanel.HasCorrectRealizedColumns` and
    `InternalBringIndexIntoView` stubs.
  - `DataGridCell.Tracker`, `PrepareCell`, and `ClearCell` glue.
  - `MS.Internal.Helper.InvalidateMeasureOnPath` shim.
  - `SR.DataGrid_ReadonlyCellsItemsSource`.

## Upstream Edits

- Marked `DataGridCellsPresenter` as `partial`.
- Changed `OnApplyTemplate` to `protected override` for WinUI compatibility.
- Guarded only the WPF `DrawingContext` grid-line `OnRender` override under
  `#if !HAS_UNO`; Uno already renders grid lines through cell/header border
  thickness in the shim visual path.

## What This Unlocks

- The linked WPF presenter now owns `SyncProperties`, `OnColumnsChanged`,
  `NotifyPropertyChanged`, `MultipleCopiesCollection` mirroring, and active
  cell tracking.
- Direct-hosted rows still render through the existing shim `DataGridRow`
  cell-building path, so the current probe coverage remains stable.

## Still Deferred

- Real `DataGridCellsPanel` measure/arrange, realized-column blocks, and
  horizontal virtualization.
- Remaining guarded `DataGridCell` regions: WPF edit signatures, read-only
  coercion, selection-update edge cases, WPF input, and DrawingContext grid
  rendering.
- `DataGridColumnHeadersPresenter` replacement of manual header generation.

## Verification

```
dotnet build WindowsShims/src/LeXtudio.Windows/LeXtudio.Windows.csproj --nologo -v:minimal /clp:ErrorsOnly
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/ --nologo -v:minimal
dotnet run --project WindowsShims/src/LeXtudio.Windows.Sample/LeXtudio.Windows.Sample.csproj --framework net10.0-desktop -- --probe
```

Results:
- Build: 0 errors.
- Tests: 136/136 passed.
- Probe: `DONE failures=0`.
