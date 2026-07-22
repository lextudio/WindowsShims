# DataGrid Port - Session 35

Date: 2026-06-12

## Goal

Cell-level selection honoring `SelectionUnit` (FullRow vs Cell).

## What Changed

- `DataGridCell.IsSelected` now paints a (stronger) cell highlight; row-level
  selection no longer syncs cell `IsSelected` (`DataGridRow.UpdateSelectionVisual`
  only tints the row, cells stay transparent so the tint shows through).
- `DataGridCell.OnPointerPressed` → `DataGrid.HandleShimCellClicked(cell)` and
  marks the event handled (so the parent row does not also select).
- `DataGrid.HandleShimCellClicked`: in `FullRow` mode routes to
  `HandleShimRowClicked`; otherwise clears any row selection, clears the
  previously cell-selected cell, selects the clicked cell, and brings its row
  into view. Tracked in `_shimSelectedCell`.
- Probe step "cell-level selection honors SelectionUnit.Cell" (toggles
  `SelectionUnit`, clicks two cells, asserts single-cell highlight and no row
  selection); test guard for `HandleShimCellClicked`. 116 tests; 19 probe
  steps; failures=0.

## Verification

Build succeeded; 116 passed/0 failed; probe `DONE failures=0`
(`SelectionUnit = Cell`).

## Notes / honest limits

- Single-cell selection only (no cell range / Ctrl-cell / row-header unit
  specifics for `CellOrRowHeader`, which is treated like `Cell`).
- `DataGrid.SelectedCells` (the WPF collection) is not populated by the shim
  cell selection; only the visual + `_shimSelectedCell` track it.
- Cell selection is not preserved across rebuilds (unlike row selection);
  a sort/reactivity rebuild drops `_shimSelectedCell`.
- Keyboard navigation still moves row selection, not the current cell.

## Next Session

1. Preserve current cell across rebuilds (by row item + column), mirroring the
   row-selection retention.
2. Populate `DataGrid.SelectedCells` / `CurrentCell` from shim cell selection.
3. `Auto` column width (deferred: needs a post-realization measure pass).
