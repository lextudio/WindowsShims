# DataGrid Port - Session 39

Date: 2026-06-12

## Goal

Minimal text-cell editing — the largest remaining interactive feature:
double-click / F2 to edit, Enter to commit (write back to the item), Escape to
cancel.

## What Changed

- `DataGridBoundColumn.BindingPath` exposes the bound column's path.
- `DataGridCell` implements the previously-stubbed edit methods:
  - `BeginEdit`: if bound, writable, and not read-only, swaps the display
    element for a WinUI `TextBox` seeded with the item's current value, sets
    `IsEditing`, focuses and selects.
  - `CommitEdit`: writes `TextBox.Text` back to the item property via
    reflection with type conversion (`Convert.ChangeType`, nullable-aware);
    on conversion failure keeps editing (returns false). Restores the display
    element via `BuildVisualTree`.
  - `CancelEdit`: restores the display element without writing.
  - Input: `OnDoubleTapped` → BeginEdit; `OnKeyDown` Enter→commit,
    Escape→cancel, F2→begin (and defers to base for navigation otherwise).
- Probe: edits the Age cell (int) to "99", commits, asserts `item.Age == 99`,
  `IsEditing == false`, and the display element restored. The sample `Person`
  changed from a record to a mutable class so write-back is observable.
- Tests: `CellEditSurfaceExists`. 117 tests; 20 probe steps; failures=0.

## Verification

Build succeeded; 117 passed/0 failed; probe `DONE failures=0` —
"after edit: item.Age=99, IsEditing=False".

## Notes / honest limits

- Write-back is reflection-based (top-level property path only); it does not
  use the WPF editing-binding / `IEditableObject` / `BindingGroup` pipeline,
  so transactional row edit, validation, and `CellEditEnding`/`RowEditEnding`
  events are not honored.
- No commit-on-focus-lost (only Enter/F2/Escape/explicit); no edit triggers
  for non-text columns (checkbox/combo/template editing not implemented).
- The display element is `OneWay`-bound, so external changes to a mutable +
  `INotifyPropertyChanged` source would refresh, but the plain mutable sample
  item does not notify (the post-commit `BuildVisualTree` re-reads the value).
- `IsReadOnly` is honored at the cell level only (not coerced from
  `DataGrid.IsReadOnly` / column `IsReadOnly` here).

## Next Session

1. Commit-on-focus-lost and honor `DataGrid.IsReadOnly` / column `IsReadOnly`.
2. Raise `BeginningEdit` / `CellEditEnding` events; route through
   `IEditableObject` for row-level commit/cancel.
3. `Auto` column width; multi-select.
