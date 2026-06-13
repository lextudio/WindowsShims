# DataGrid Port - Session 28

Date: 2026-06-12

## Goal

Make selection visible and interactive (session-27 next step #1): reflect
`DataGridRow.IsSelected` in the visual and wire pointer input to selection,
now that the generator can resolve containers.

## Outcome

Clicking a row selects it. Single-select semantics: the previously selected
row is cleared, the clicked row highlights (light-blue background), and
`DataGrid.SelectedItem` tracks the clicked item. Probe verifies the full
behavior including the highlight reflecting on/off.

## What Changed

### DataGridRow: selection visual + input

- `IsSelected` is now a real property (backing field) that calls
  `UpdateSelectionVisual()`: sets the row `Background` to the selection brush
  (or null) and syncs each cell's `IsSelected`.
- The row template wraps `PART_CellsHost` in a `Border` bound to
  `{TemplateBinding Background}` so the highlight paints behind the cells.
- `OnApplyTemplate` re-applies the selection visual after (re)building cells.
- `OnPointerPressed` routes to `DataGridOwner.HandleShimRowClicked(this)`.

### DataGrid: shim selection

- `HandleShimRowClicked(row)` implements single-select: clears `IsSelected`
  on every other generated row (iterating
  `ItemContainerGenerator.Containers`), selects the clicked row, and sets
  `SelectedItem` to its item.
- `ItemContainerGenerator.Containers` exposes the registered containers
  (read-only, display order) for the iteration.

### Sample probe + tests

- `InternalsVisibleTo` now includes `LeXtudio.Windows.Sample` so the probe
  can drive `HandleShimRowClicked` directly.
- Probe step "selection: single-select clears the previous row" clicks row0
  then row1, asserting the `IsSelected` flip, `SelectedItem` tracking, and the
  `Background` highlight appearing on the selected row / clearing on the old.
- Test `ShimSelectionSurfaceExists` pins the entry point and the settable
  `IsSelected`. 112 tests total.

## Verification

```bash
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
cd WindowsShims/src/LeXtudio.Windows.Sample && dotnet run --framework net10.0-desktop -- --probe
```

Result: build succeeded; 112 tests passed, 0 failed; probe `DONE failures=0`
— "row1 highlighted=True, row0 cleared=True".

## Notes / honest limits

- Selection is shim-driven and single-select only. No Ctrl/Shift multi-select,
  no cell-level selection unit (`DataGridCell.IsSelected` is synced from the
  row but there is no independent cell selection), no keyboard selection.
- It drives `SelectedItem` but does not run the full WPF `Selector`
  selection pipeline (`SelectedItems`, `SelectionChanged`, anchor/range).
  Those remain inert; the shim path and the WPF path are not yet unified.
- Pointer routing relies on `OnPointerPressed` reaching the row; there is no
  hit-testing for which cell was clicked.
- Headers are still plain `TextBlock`s; column widths still the flat fallback;
  no editing; no scroll-into-view.

## Next Session

1. Cell-level selection + click feedback: hit-test which `DataGridCell` was
   pressed and reflect `DataGridCell.IsSelected` distinctly (current cell
   highlight), honoring `SelectionUnit`.
2. Use `DataGridColumnHeader` controls for the header row and begin honoring
   column `Width` (`Auto`/pixel) over the flat 120px fallback.
3. Implement `DataGridRow.BringIntoView`/`ScrollCellIntoView` against the
   template `ScrollViewer` so navigation scrolls the selected row into view.
