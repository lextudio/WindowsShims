# DataGrid Port - Session 38

Date: 2026-06-12

## Goal

Clear retained cell selection when its item leaves the collection (mirror of
session 32 for cells), completing the selection-cleanup model.

## What Changed

- `BuildShimVisualTree` tracks whether the retained cell-selection item is
  still present (alongside the existing row check). If gone, it clears
  `_shimSelectedCell` / `_shimSelectedCellItem` / `_shimSelectedColumn`, resets
  `CurrentCell` to `DataGridCellInfo.Unset`, and clears `SelectedCells`.
- Probe extended: after cell selection + retention, removes the cell-selected
  item and asserts `SelectedCells.Count == 0` and `CurrentCell.IsValid ==
  false`. 116 tests; 19 probe steps; failures=0.

## Verification

Build succeeded; 116 passed/0 failed; probe `DONE failures=0` —
"after remove: SelectedCells=0, CurrentCell.IsValid=False".

## Notes / honest limits

- The selection model (row + cell: select, single-select clearing, retention
  across rebuilds, cleanup on removal) is now coherent for the single-selection
  case. Multi-select, range, `*Changed` events, and the WPF `Selector`
  pipeline remain out of scope.

## Next Session

1. Cell editing for text columns (double-click/F2 → edit element → commit
   writes back) — the largest remaining interactive feature.
2. `Auto` column width (post-realization measure pass).
3. Multi-select (Ctrl/Shift) + `SelectedItems`.
