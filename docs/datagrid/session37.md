# DataGrid Port - Session 37

Date: 2026-06-12

## Goal

Retain cell selection across render rebuilds (by row item + column), mirroring
the row-selection retention from session 31.

## What Changed

- `DataGrid` retains the cell selection as `_shimSelectedCellItem` +
  `_shimSelectedColumn` (set in `HandleShimCellClicked`).
- `TryReselectCell(cell)`: a row calls this while (re)building each cell; if
  the cell's item+column match the retained selection it re-marks the cell
  `IsSelected` and refreshes the live `_shimSelectedCell` reference.
- `DataGridRow.BuildCells` invokes `owner.TryReselectCell(cell)` for each cell,
  so a sort/reactivity rebuild restores the highlighted cell on the new row
  instances.
- Probe extended: after selecting a cell it sorts (rebuild), re-lays-out, and
  asserts the cell at the same (item, column) is still selected. 116 tests;
  19 probe steps; failures=0.

## Verification

Build succeeded; 116 passed/0 failed; probe `DONE failures=0` —
"cell reselected after sort = True".

## Notes / honest limits

- Retains a single cell; no range retention.
- The stale `_shimSelectedCellItem`/`_shimSelectedColumn` is not cleared when
  the item leaves the collection (row selection has this cleanup; cell does
  not yet) — a removed cell-selected item leaves dangling retention until the
  next cell click.
- `*Changed` events still not raised; FullRow still doesn't populate cells.

## Next Session

1. Clear retained cell selection when its item leaves the collection (mirror
   session 32 for cells).
2. Raise `CurrentCellChanged` / `SelectedCellsChanged`.
3. `Auto` column width (post-realization measure pass).
