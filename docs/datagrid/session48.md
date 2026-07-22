# DataGrid Port - Session 48

Date: 2026-06-12

## Goal

Row-level validation: run `DataGrid.RowValidationRules` on row commit and show
a row error indicator, blocking the commit on failure.

## What Changed

- `DataGrid.CommitRowEdit` now runs each `RowValidationRules` rule
  (`ValidationRule.Validate(item, culture)`) before `RowEditEnding`/`EndEdit`;
  the first failure calls `row.SetRowError(message)` and returns false (commit
  refused, row stays in edit). A clean pass clears the error and commits.
- `DataGridRow` exposes `HasRowValidationError` / `RowValidationError` and
  `SetRowError`/`ClearRowError`, which paint a red border (the row template's
  `Border` now `TemplateBinding`s `BorderBrush`/`BorderThickness`).
- Sample: a `MinAgeRule : ValidationRule` (Age ≥ 18) added to
  `RowValidationRules`. New probe step: Age=10 (valid per cell rule 0..150 but
  invalid per row rule) flags the row + blocks commit; Age=30 commits and
  clears. Test guards for `SetRowError`/`HasRowValidationError`. 121 tests; 29
  probe steps; failures=0.

## Verification

Build succeeded; 121 passed/0 failed; probe `DONE failures=0` —
"row error after Age=10: True (Age must be at least 18)".

## Notes / honest limits

- The cell value is written (and the cell-level `IDataErrorInfo` passes) before
  the row rule runs; the row rule then blocks the row commit and flags it,
  matching the layered cell-then-row validation order.
- `ValidationRule.Validate` is called with the row item and current culture
  only (no `BindingGroup` overload, no `ValidationStep`); `ItemBindingGroup`
  precedence over `RowValidationRules` is not modeled.
- Row error is a border indicator; no WPF row-header error glyph / adorner or
  `Validation.Errors` attached collection.

## Next Session

1. Multi-cell row editing (keep the row transaction open across cells; commit
   on leaving the row).
2. Row-header error glyph (`DataGridRowHeader`) reflecting
   `HasRowValidationError`.
3. Route checkbox/combo writes through the row transaction + validation.
