# DataGrid Port - Session 47

Date: 2026-06-12

## Goal

Row edit transactions — tie the cell edit lifecycle to a row-level transaction
driving `IEditableObject` and the `RowEditEnding` event.

## What Changed

- `DataGrid` tracks `_editingRow` and adds:
  - `BeginRowEdit(row)` — marks the row editing and calls
    `IEditableObject.BeginEdit()` on the item (snapshot).
  - `CommitRowEdit(row)` — raises `RowEditEnding(Commit)` (cancelable; a veto
    returns false and keeps editing), then `IEditableObject.EndEdit()`.
  - `CancelRowEdit(row)` — raises `RowEditEnding(Cancel)`, then
    `IEditableObject.CancelEdit()` (rolls back the snapshot).
- `DataGridCell` wires these into editing: `BeginEdit` → `BeginRowEdit` (after
  the `BeginningEdit` event); `CommitEdit` → `CommitRowEdit` after the cell
  value validates (a `RowEditEnding` veto keeps the cell editing);
  `CancelEdit` → `CancelRowEdit`.
- Sample `Person` implements `IEditableObject` (snapshots Age; counts
  `EndEdit`/`CancelEdit`). New probe step: begin→commit fires `EndEdit` +
  `RowEditEnding(Commit)` and writes Age=55; begin→cancel fires `CancelEdit` +
  `RowEditEnding(Cancel)` and reverts the snapshot. Test guards for the three
  row-edit methods. 121 tests; 28 probe steps; failures=0.

## Verification

Build succeeded; 121 passed/0 failed; probe `DONE failures=0` —
"after cancel: InEdit=False, CancelEditΔ=1, cancels=1, Age=55".

## Notes / honest limits

- The row transaction spans a single cell edit (begin on cell-edit start, end
  on that cell's commit/cancel); WPF keeps the row in edit across multiple
  cells until the row is committed (Enter on last cell / leaving the row).
- Only one row edits at a time; starting an edit elsewhere does not first
  commit a previously-open row transaction.
- `RowValidationRules` / row-level validation error indicator are not yet
  implemented (cell-level `IDataErrorInfo` from session 46 still applies).
- Checkbox/combo in-place writes do not open a row transaction.

## Next Session

1. Row-level validation (`DataGrid.RowValidationRules`) on `CommitRowEdit`
   with a row error indicator.
2. Multi-cell row editing (keep the row open across cells).
3. Route checkbox/combo writes through the row transaction.
