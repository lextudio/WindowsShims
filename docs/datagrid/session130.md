# Session 130 — Per-property coercion activation (item 5, first slice)

Date: 2026-08-12. DataGrid suite 69/69, RichTextBox 238/238, model tests 234/234.

## Goal

todo.md item 5: `CoerceValue` is a universal no-op except on
`DataGridColumnHeader`. Activate coercion with the smallest blast radius, one
property at a time. First slice: `FrozenColumnCount` and `AlternationCount` on
`DataGrid` — both are pure value fixes with no interaction with the shim's
parallel width/selection logic.

## Changes

### `src/LeXtudio.Windows/System.Windows/Controls/DataGrid.cs`

`internal new void CoerceValue(DependencyProperty property)` — hides the base
no-op (same pattern as DataGridColumnHeader, session 121), whitelist of two:

- `FrozenColumnCountProperty` → `OnCoerceFrozenColumnCount`
- `AlternationCountProperty` → `OnCoerceAlternationCount`

`SetCoerced` helper: run the callback, `SetValue` only when changed.

### `src/LeXtudio.Windows/System.Windows/Controls/ItemsControl.cs`

`AlternationCount` was a plain auto-property (`{ get; set; }`) — the shim's
spine registers `AlternationCountProperty` as a real DP, but the CLR property
never read/wrote it, so any `CoerceValue` result was invisible to getters.
Rewired to `GetValue`/`SetValue(AlternationCountProperty)`.

### Host probes (`tests/DataGrid.IntegrationTestHost/MainPage.cs`)

- `datagrid.probe.coercion-readback` — frozenColumnCount, alternationCount,
  alternatingRowBackground, rowBackground.
- `datagrid.probe.set-frozen-column-count(count)` — sets `FrozenColumnCount`
  beyond the column count, **then adds a column**: coercion runs on the
  column-collection-changed path (upstream DataGrid.cs:263) and first measure
  (7639), not on plain `SetValue`. Without the column add the value stays 99.

### Tests (`tests/DataGrid.IntegrationTests/DataGridIntegrationTests.cs`)

- `Coercion_FrozenColumnCountClampsToColumnCount` — set 99, expect clamp to
  `Columns.Count`.
- `Coercion_AlternationCountPromotesToTwoWhenAlternatingBackgroundSet` —
  `create-alternating-row-grid`, expect `AlternationCount >= 2`.

## Findings

1. **`OverrideMetadata` is a project-wide no-op** (WinUI has no per-type
   metadata). The upstream `AlternationCountProperty.OverrideMetadata(...,
   OnCoerceAlternationCount)` at DataGrid.cs:54 never registered anything, so
   looking up the callback via `property.GetMetadata(GetType())` returns
   metadata without a `CoerceValueCallback`. The callbacks are therefore
   invoked **directly** from the whitelist. Worth remembering for future
   slices: any coercion registered via `OverrideMetadata` needs this treatment.
2. **Coercion triggers come from upstream call sites, not `SetValue`.** The
   `FrozenColumnCount` probe had to add a column to reach the
   column-collection-changed path; plain set + UpdateLayout never calls
   `CoerceValue`.
3. **Debugging trace** (`ShimCoerceCallCount` / `ShimCoerceAlternationTrace`)
   showed `CoerceValue` ran, the value was coerced to 2 in the DP, yet the
   readback said 0 — the `ItemsControl.AlternationCount` auto-property was the
   culprit, not the property system.

## Verification

- `Coercion_*` tests: green (first FrozenColumnCount failed → column-add
  trigger fixed it; then AlternationCount failed → auto-property fixed).
- Full DataGrid suite: 69/69 (67 + 2 new).
- RichTextBox 238/238, model tests 234/234: unchanged.

## Next

Second slice options (todo item 5): `DataGridColumn.Width`/`DisplayIndex`
coercion, or `DataGridCell.IsEditing`-family — pick one that doesn't overlap
the shim's parallel width logic. Remaining ~25 dormant registrations stay
no-op until the parallel logic is retired.
