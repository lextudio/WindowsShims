# DataGrid Port - Session 92

Goal: continue the presenter-layer WPF reuse after `DataGridCellsPresenter`.

## Choice

The next reuse target was `DataGridRowsPresenter`.

It is much smaller than `DataGridCellsPanel`, but it owns important WPF row-host
contracts: `InternalItemsHost` hookup, `BringIndexIntoView`, viewport-size
notification, and cleanup policy for rows with validation errors.

## Changes

- Linked upstream WPF `Primitives/DataGridRowsPresenter.cs`.
- Marked the upstream class `partial`.
- Deleted the now-empty local `Primitives/DataGridPresenters.cs` stub file.
- Added narrow bridge support:
  - `VirtualizingStackPanel.BringIndexIntoView`,
    `OnIsItemsHostChanged`, `OnViewportSizeChanged`, and
    `OnCleanUpVirtualizedItem`.
  - `CleanUpVirtualizedItemEventArgs`.
  - `System.Windows.Controls.Primitives.IItemContainerGenerator` and
    `ItemContainerGenerator.GetItemContainerGeneratorForPanel`.
  - `ItemsControl.GetItemsOwner`.
  - `Validation.GetHasError`.

## What This Unlocks

- The row presenter is now owned by linked WPF source instead of a local shell.
- The real presenter can participate when the template/layout path is later
  switched from `PART_ShimRowsHost` to WPF-style item hosting.

## Still Deferred

- `ItemsControl.GetItemsOwner` currently returns null, so `DataGridRowsPresenter.Owner`
  only becomes meaningful after the real WPF item-host path exists.
- `VirtualizingStackPanel` methods are no-op compatibility hooks.
- The big remaining layout step is still `DataGridCellsPanel`.

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
