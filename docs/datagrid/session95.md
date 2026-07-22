# DataGrid Port - Session 95

Goal: bring `DataGridCellsPanel` into the linked WPF source set without
pretending the full WPF generator/layout stack exists on Uno.

## Changes

- Linked upstream WPF `DataGridCellsPanel.cs`.
- Marked the upstream class `partial`.
- Removed the local `DataGridCellsPanel` shell from `DataGridHelperStubs.cs`.
- Added the typed realized-column substrate:
  - `RealizedColumnsBlock`
  - `GeneratorPosition`
  - `ItemsChangedEventArgs`
- Changed `DataGridColumnCollection` realized-column placeholders from `object`
  to `List<RealizedColumnsBlock>?`.
- Added virtualizing-panel compatibility state:
  - `IsVirtualizing`
  - `InRecyclingMode`
  - `BringIndexIntoView`
  - `OnItemsChanged`
  - `OnClearChildren`

## Guarding Decision

The first compile pass showed the expected deep dependency set:

- real `IItemContainerGenerator` generation sessions,
- `InternalChildren` mutation,
- recycling generator APIs,
- scroll-info and hierarchical virtualization contracts,
- dirty-measure retry paths,
- frozen-column layout transforms.

Those are the actual WPF layout engine, not narrow API gaps. The Uno build now
keeps the source linked but guards the full WPF body under `#if !HAS_UNO`.
The `HAS_UNO` path provides only the currently consumed surface:

- `HasCorrectRealizedColumns => true`
- `InternalBringIndexIntoView(int)` no-op

## Still Deferred

- Real `DataGridCellsPanel` measure/arrange.
- Column virtualization and recycling.
- Frozen-column physical layout during horizontal scroll.
- Replacing the manual `PART_ShimRowsHost` tree with WPF item-hosted row/header
  layout.

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
