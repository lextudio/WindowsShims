# DataGrid Port - Session 94

Goal: link the remaining WPF column-header presenter now that its drag visual
dependencies are available.

## Changes

- Linked upstream WPF `Primitives/DataGridColumnHeadersPresenter.cs`.
- Linked upstream WPF `DataGridColumnHeaderCollection.cs`.
- Deleted the local `DataGridColumnHeadersPresenter` shell.
- Marked the upstream presenter `partial`.
- Changed `OnApplyTemplate` to `protected override` for WinUI compatibility.
- Patched two WPF-only geometry constructor calls to WinUI-compatible forms.
- Added bridge support:
  - `ItemsControl.GetLayoutClip`, `VisualChildrenCount`, `GetVisualChild`,
    `AddVisualChild`, and `RemoveVisualChild`.
  - `DataGridColumnHeadersPresenterAutomationPeer` shim.
  - `DoubleUtil.LessThanOrClose` and `GreaterThanOrClose`.
  - `DataGridColumnCollection.AverageColumnWidth` on the Uno partial.
  - `Separator : Control` so WPF drop separator remains assignable to `Control`.

## What This Unlocks

- The WPF column-header presenter now compiles and owns the source surface for
  header item-source wrapping, header container preparation/clearing,
  notification propagation, drag indicators, drop indicator placement, and
  display-index calculations.
- `DataGridColumnHeaderCollection` now mirrors column-header collection-change
  semantics directly from WPF.

## Still Deferred

- The current runtime template still uses `DataGrid.BuildHeaderRow` to manually
  construct headers in `PART_ShimRowsHost`.
- `ItemsControl.GetItemsOwner`, visual-child management, and the virtualizing
  host hooks remain compatibility shims, so the linked presenter is substrate
  for a later template/host swap rather than the live header-generation path.
- Full `DataGridCellsPanel` layout/virtualization remains the large next gap.

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
