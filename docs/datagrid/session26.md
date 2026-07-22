# DataGrid Port - Session 26

Date: 2026-06-12

## Goal

Two rungs from the session-25 plan: re-render on collection changes
(reactivity) and promote `DataGridRow` to be the real visual host for its own
cells (so the on-screen tree matches the WPF row/cell APIs).

## Outcome

- **Rows host their own cells.** Each data row is now a `DataGridRow` with its
  own template (`PART_CellsHost`) that builds one `DataGridCell` per visible
  column. `DataGridRow.TryGetCell(index)` returns the real generated cells.
- **The grid reacts to changes.** Adding/removing items or columns now
  re-renders. Probe: adding a 4th item grows the host from 4 to 5 children
  (header + 4 rows), `DesiredSize` 386×96 → 386×119.5.

## What Changed

### DataGridRow becomes the visual container

- `DataGridRow` (shim shell, extends the shim `Control`): added a cached
  code-built `ControlTemplate` (`PART_CellsHost`, a horizontal `StackPanel`),
  assigned in an `InitializeDefaultStyleKey` override. Overrode
  `OnApplyTemplate` to call `BuildCells()`, which creates one `DataGridCell`
  per visible owner column (content from the column, bound to the item) and
  records them so `TryGetCell(int)` resolves real cells.
- `PrepareRow` now sets `DataContext = item` and triggers `BuildCells()`
  (no-ops until the row is templated, then OnApplyTemplate builds).

### DataGrid render path + reactivity

- `BuildShimVisualTree` now adds `DataGridRow` instances to
  `PART_ShimRowsHost` (header row stays a plain panel); the rows host their
  own cells rather than the grid building sibling cell panels.
- `ShimColumnWidth` promoted to `internal` so rows reuse the grid's column
  width (headers and cells stay aligned).
- `HookShimChangeNotifications` (idempotent) subscribes to `Items` and
  `Columns` `CollectionChanged`; `OnShimContentChanged` re-runs
  `BuildShimVisualTree`. Hooked the first time the template is applied.

### Sample probe

- New verification steps: rows are `DataGridRow` containers whose
  `PART_CellsHost` holds one cell per column (3), and adding an item grows the
  host child count (reactivity). All steps pass, `failures=0`.

### Tests

- `DataGridRowHostsItsOwnCells` (BuildCells / OnApplyTemplate / TryGetCell
  surface) and `DataGridReactsToCollectionChanges`
  (HookShimChangeNotifications / OnShimContentChanged). 109 tests total.

## Verification

```bash
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
cd WindowsShims/src/LeXtudio.Windows.Sample && dotnet run --framework net10.0-desktop -- --probe
```

Result: build succeeded; 109 tests passed, 0 failed; probe `DONE failures=0`
— header + 3 rows, 3 cells per row, 5 host children after add,
`DesiredSize=386,119.5`.

## Notes / honest limits

- Reactivity is a full rebuild (clear + re-add) on any `Items`/`Columns`
  change — correct but not incremental. Selection, sorting, column resize,
  and editing still do not re-render or function.
- Rows are still not produced through `ItemContainerGenerator`; the grid
  news up `DataGridRow`s directly. `ContainerFromIndex`/`ContainerFromItem`
  still return null, so WPF code paths that resolve containers through the
  generator (e.g. `ContainerFromItemInfo` in scrolling/selection) are not yet
  wired to these rows.
- Headers are plain `TextBlock`s, not `DataGridColumnHeader`; column widths
  are still the flat 120px fallback.
- No virtualization — every item gets a live row.

## Next Session

1. Route container lookups to the rendered rows: have
   `ItemContainerGenerator.ContainerFromIndex`/`ContainerFromItem` and
   `DataGrid.ContainerFromItemInfo` return the `DataGridRow`s the render path
   builds (track them in a list/map), so selection/scroll code can find them.
2. Replace the header `TextBlock`s with `DataGridColumnHeader` controls and
   begin honoring column `Width` (`Auto`/pixel) over the flat fallback.
3. Make selection visible: reflect `DataGridRow.IsSelected`/`DataGridCell.
   IsSelected` in the visual (background) and wire pointer input to selection.
