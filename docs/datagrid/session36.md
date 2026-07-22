# DataGrid Port - Session 36

Date: 2026-06-12

## Goal

Reflect shim cell selection into the WPF-facing `CurrentCell` / `SelectedCells`
surface.

## What Changed

- `HandleShimCellClicked` (Cell mode) now sets `CurrentCell = new
  DataGridCellInfo(cell)` and resets `SelectedCells` to that single cell info,
  in addition to the visual cell highlight.
- Probe step extended: after cell selection it asserts `CurrentCell.Column`
  matches the clicked cell's column and `SelectedCells` holds exactly that
  cell. 116 tests; 19 probe steps; failures=0.

## Verification

Build succeeded; 116 passed/0 failed; probe `DONE failures=0` —
"CurrentCell column=City, SelectedCells=1".

## Notes / honest limits

- Single-cell only; `SelectedCells` always holds 0 or 1 entry (no range/multi).
- Row (`FullRow`) selection does not populate `SelectedCells` with the row's
  cells (WPF does); only Cell-mode selection writes the cell surface.
- `CurrentCell`/`SelectedCells` are written by the shim but the WPF
  `Selector`/current-cell coercion pipeline is still not driven, so derived
  notifications (`CurrentCellChanged`, `SelectedCellsChanged`) are not raised.
- Cell selection still not retained across rebuilds.

## Next Session

1. Retain current cell across rebuilds (by row item + column).
2. Raise `SelectedCellsChanged` / `CurrentCellChanged` from the shim writes.
3. Populate `SelectedCells` for `FullRow` selection (the row's cells).
