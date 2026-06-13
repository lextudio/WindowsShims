# DataGrid Port - Session 46

Date: 2026-06-12

## Goal

Validation surface — `IDataErrorInfo`-based cell validation on commit, with an
error visual and queryable error state.

## What Changed

- `DataGridCell.CommitEdit`, after writing the converted value, validates via
  `IDataErrorInfo`: if `item[propertyName]` returns a non-empty error, the cell
  flags the error and stays in edit mode (commit returns false); a clean value
  clears the error and ends the edit.
- `DataGridCell.HasValidationError` / `ValidationError` expose the state;
  `SetValidationError` paints a red border + sets a `ToolTip` with the message;
  `ClearValidationError` reverts.
- Sample: `Person` implements `IDataErrorInfo` (Age must be 0..150). New probe
  step: editing Age to "999" refuses commit + flags the cell (still editing,
  message set); editing to "42" commits and clears. Test guard for the
  validation properties. 121 tests; 27 probe steps; failures=0.

## Verification

Build succeeded; 121 passed/0 failed; probe `DONE failures=0` —
"invalid: HasError=True, IsEditing=True, msg=Age must be between 0 and 150".

## Notes / honest limits

- `IDataErrorInfo` only (no `INotifyDataErrorInfo`, no binding `ValidationRule`
  collection, no `ValidatesOnExceptions`). Type-conversion failures are still
  handled separately (commit refused, no message).
- The invalid value is written to the source before validation (matches
  `ValidatesOnDataErrors` semantics) and the cell stays in edit mode; there is
  no row-level validation (`RowValidationRules` / `RowEditEnding`) or the WPF
  validation error template/adorner.
- Validation runs only through the text-edit commit path; checkbox/combo
  in-place writes are not validated.

## Next Session

1. Row-level validation (`DataGrid.RowValidationRules` / `RowEditEnding`) and a
   row error indicator.
2. Route checkbox/combo writes through commit (so they validate + raise events).
3. `INotifyDataErrorInfo` support.
