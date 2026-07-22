# DataGrid Port - Session 43

Date: 2026-06-12

## Goal

Editing hardening — a themed batch grouping the related edit-lifecycle tasks:
read-only coercion, commit-on-blur, and the cancelable WPF edit events.

## What Changed

### Read-only coercion
- `DataGrid.IsCellEffectivelyReadOnly(column)` = `DataGrid.IsReadOnly ||
  column.IsReadOnly`. `DataGridCell.BeginEdit` returns false when the cell,
  column, or grid is read-only.

### Cancelable edit events (linked WPF events, raised by the shim)
- `DataGrid.RaiseBeginningEdit` / `RaiseCellEditEnding` forwarders construct
  the linked `DataGridBeginningEditEventArgs` / `DataGridCellEditEndingEventArgs`
  and invoke the protected `OnBeginningEdit` / `OnCellEditEnding` (which fire
  the public `BeginningEdit` / `CellEditEnding` events), returning the args so
  the cell can honor `Cancel`.
- `BeginEdit` raises `BeginningEdit`; if canceled, editing does not start.
- `CommitEdit` raises `CellEditEnding(Commit)`; if vetoed, the cell stays in
  edit mode and the value is not written.
- `CancelEdit` raises `CellEditEnding(Cancel)`.

### Commit-on-blur
- The editing `TextBox`'s `LostFocus` commits the edit; the handler is
  unsubscribed in `EndEdit` (shared teardown for commit/cancel).

## Verification

Build succeeded; 119 passed/0 failed; probe `DONE failures=0`. The probe
exercises: grid read-only blocks edit, column read-only blocks edit,
`BeginningEdit` cancel blocks, `CellEditEnding` veto keeps editing + discards,
then a clean commit writes back ("after vetoed+committed edit: Age=123,
IsEditing=False"). Test `CellEditSurfaceExists` extended with the new
forwarders. 24 probe steps.

## Notes / honest limits

- Commit-on-blur is wired via `LostFocus` but is not exercised headlessly by
  the probe (no focus changes in `--probe`); the cancelable-event and
  read-only paths are.
- Still row/cell-level only: no `RowEditEnding` / `IEditableObject`
  transactional row commit-cancel, no validation (`ValidationRule` /
  `BindingGroup` / error template), no edit for non-text column types.
- `PreparingCellForEdit` and `CellEditEnding`'s `EditingElement` reflect the
  shim `TextBox`, not a WPF-generated editing element.

## Next Session

1. A non-text column type end-to-end: `DataGridCheckBoxColumn` render + toggle
   write-back (its own editing path).
2. Re-run the width pass on grid `SizeChanged` (Star reflow on resize).
3. Validation surface: cell error state + `ValidationRule` hook.
