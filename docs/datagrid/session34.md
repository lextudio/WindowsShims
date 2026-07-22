# DataGrid Port - Session 34

Date: 2026-06-12

## Goal

Scroll selection into view, and add Home/End/PageUp/PageDown navigation.

## What Changed

- `DataGridRow.BringIntoView()` now calls WinUI `StartBringIntoView()` (was a
  no-op), so a selected row scrolls into the template `ScrollViewer`.
- `HandleShimRowClicked` calls `BringIntoView()` on the selected row, so both
  pointer and keyboard selection scroll it into view.
- `OnKeyDown` adds Home → first row, End → last row, PageUp/PageDown →
  ±`ShimPageSize` (5). New `MoveSelectionToIndex(int)` (clamped) backs
  Home/End.
- Probe step "Home/End move selection to first/last row"; test guard extended
  for `MoveSelectionToIndex`. 116 tests; 18 probe steps; failures=0.

## Verification

Build succeeded; 116 passed/0 failed; probe `DONE failures=0`.

## Notes / honest limits

- PageUp/PageDown use a fixed page of 5 rows, not a viewport-measured page.
- `BringIntoView` relies on WinUI `StartBringIntoView`; with all rows realized
  (no virtualization) and a small data set it is effectively a no-op visually,
  but the call path is correct for when content overflows.
- Still shim single-select; no cell focus movement; WPF `Selector` inert.

## Next Session

1. `Auto` column width (measure header + cell content; apply uniform width).
2. Cell-level selection with hit-testing (`SelectionUnit`).
3. Route shim selection/sort through WPF `Selector`/`SortDescriptions`.
