# DataGrid Port - Session 27

Date: 2026-06-12

## Goal

Route container lookups to the rendered rows (session-26 next step #1): make
`ItemContainerGenerator` and `DataGrid.ContainerFromItemInfo` return the
`DataGridRow`s the render path builds, so the linked WPF selection / scroll /
row-details code can find its containers.

## Outcome

The generator resolves real containers. Probe confirms round-trip:
`ItemContainerGenerator.Status == ContainersGenerated`,
`ContainerFromIndex(0)` and `ContainerFromItem(item0)` return the same
`DataGridRow`, `IndexFromContainer(row) == 0`, and `ItemFromContainer(row)`
is `item0`.

## What Changed

### ItemContainerGenerator gains a registry

- `ItemContainerGenerator` now holds parallel `item`/`container` lists in
  display order:
  - `Status` → `ContainersGenerated` when non-empty, else `NotStarted`.
  - `ContainerFromIndex` / `ContainerFromItem` (item identity via
    `ItemsControl.EqualsEx`) / `IndexFromContainer` / `ItemFromContainer`
    resolve against the registry.
  - `StatusChanged` is now a real event.
  - Internal `ResetContainers()` / `RegisterContainer(item, container)` /
    `NotifyContainersGenerated()` for the render path.

### Render path registers rows

- `DataGrid.BuildShimVisualTree`: resets the generator registry, registers
  each `(item, DataGridRow)` as it builds rows, and raises
  `NotifyContainersGenerated()` at the end.

### Container lookups route through the generator

- `ItemsControl.ContainerFromItemInfo(info)`: prefer `ContainerFromIndex(
  info.Index)`, fall back to `ContainerFromItem(info.Item)`.
- `ItemsControl.ItemInfoFromContainer(container)`: now carries the resolved
  item, container, and index (was item-only).

### Sample probe + tests

- Probe step "container generation: rows resolve via ItemContainerGenerator"
  asserts the status and the index/item round-trip.
- Tests: `EmptyGeneratorReportsNotStarted` (real generator instance, no UI
  thread needed) and `GeneratorRegistrySurfaceExists`. 111 tests total.

## Verification

```bash
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
cd WindowsShims/src/LeXtudio.Windows.Sample && dotnet run --framework net10.0-desktop -- --probe
```

Result: build succeeded; 111 tests passed, 0 failed; probe `DONE failures=0`
— generator status `ContainersGenerated`, `IndexFromContainer(row0)=0`,
`ItemFromContainer` matches `item0`.

## Notes / honest limits

- The registry is rebuilt wholesale on each render (no recycling/
  virtualization); `RecyclableContainers` is still an unused queue.
- Resolution is now wired, but the behaviors that *consume* it (selection,
  scroll-into-view, row details) are still mostly inert — e.g.
  `DataGridRow.ScrollCellIntoView`/`BringIntoView` are no-ops, selection
  visuals don't update. Those are the next behavioral rungs; the generator
  no longer blocks them.
- Headers remain plain `TextBlock`s; column widths still the flat fallback;
  no editing.

## Next Session

1. Make selection visible and interactive: reflect
   `DataGridRow.IsSelected` / `DataGridCell.IsSelected` in the visual
   (background brush) and wire pointer input → selection through the existing
   WPF selection code (which can now resolve containers).
2. Replace header `TextBlock`s with `DataGridColumnHeader` controls; begin
   honoring column `Width` (`Auto`/pixel) over the flat 120px fallback.
3. Implement `DataGridRow.BringIntoView`/`ScrollCellIntoView` against the
   template's `ScrollViewer` so keyboard/scroll navigation works.
