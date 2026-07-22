# DataGrid Port - Session 30

Date: 2026-06-12

## Goal

Header-click sorting (session-29 next step #3): clicking a column header
toggles its sort direction and re-renders the rows in sorted order.

## Outcome

Clicking a header sorts the grid. Probe: clicking the Age header orders the
four ages ascending `[36,39,41,45]`, clicking again gives descending
`[45,41,39,36]`. The active sort column shows a ▲/▼ glyph in its header.

## What Changed

### DataGrid: sort state + ordering

- `HandleShimHeaderClicked(column)`: no-op if `!CanUserSort`; otherwise
  toggles the column's `SortDirection` (Ascending ⇄ Descending; first click
  Ascending), clears every other column's direction (single sort key),
  records `_activeSortColumn`, and rebuilds.
- `OrderedItems()`: the render path now iterates this instead of `Items`
  directly — when a sort is active it `OrderBy`/`OrderByDescending` on the
  column's value, else preserves collection order. The generator registers
  rows in this display order, so container indices match what is shown.
- `GetSortValue(column, item)`: resolves the sort path from
  `SortMemberPath`, falling back to the bound column's `Binding.Path`, then
  reads the property off the item by reflection.
- `HeaderContent(column)`: appends " ▲"/" ▼" to the active sort column's
  header text.

### DataGridColumnHeader: input

- `OnPointerPressed` routes to `Column.DataGridOwner.HandleShimHeaderClicked`.

### Sample probe + tests

- Probe step "header click sorts rows (ascending then descending)" drives the
  Age header twice and checks the row order via the generator.
- Test `HeaderSortSurfaceExists` pins `HandleShimHeaderClicked` / `OrderedItems`.
  114 tests total.

## Verification

```bash
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
cd WindowsShims/src/LeXtudio.Windows.Sample && dotnet run --framework net10.0-desktop -- --probe
```

Result: build succeeded; 114 tests passed, 0 failed; probe `DONE failures=0`
— "ages ascending = [36,39,41,45]", "ages descending = [45,41,39,36]".

## Notes / honest limits

- Sorting is shim-side: it orders the rendered rows by reflecting the item
  property. It does **not** run the WPF `DataGrid.OnSorting` /
  `Items.SortDescriptions` / `DataGridSortingEventArgs` pipeline, so custom
  sort, `SortMemberPath` comparers, and the `Sorting` event are not honored.
- Single sort key only (no multi-column / Shift-click).
- Mixed value types in one column would throw via `Comparer<object?>.
  Default`; the items are assumed uniform per column.
- `GetSortValue` reflects top-level properties only (no nested paths,
  indexers, or value converters).
- Selection state is lost across a sort (rows are rebuilt); re-selecting by
  item identity after a rebuild is not yet wired.

## Next Session

1. Preserve selection across rebuilds (sort / reactivity): re-apply
   `IsSelected` to the row whose item matches the retained `SelectedItem`.
2. Route shim sort through the WPF `Sorting` event / `SortDescriptions` so
   custom and event-driven sorting work.
3. `Auto` column width (measure content) and cell-level selection with
   hit-testing.
