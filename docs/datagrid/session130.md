# Session 130 — Per-property coercion activation (item 5, slices 1-4)

Date: 2026-08-12. DataGrid suite 72/72, RichTextBox 238/238, model tests 234/234.

## Goal

todo.md item 5: `CoerceValue` is a universal no-op except on
`DataGridColumnHeader`. Activate coercion with the smallest blast radius, one
property at a time. Slices: `FrozenColumnCount`, `AlternationCount`,
`IsSynchronizedWithCurrentItem`, `CanUserAddRows`, `CanUserDeleteRows`, and
`VirtualizingPanel.IsVirtualizing` on `DataGrid` — all pure value fixes with no
interaction with the shim's parallel width/selection logic.

## Changes

### `src/LeXtudio.Windows/System.Windows/Controls/DataGrid.cs`

`internal new void CoerceValue(DependencyProperty property)` — hides the base
no-op (same pattern as DataGridColumnHeader, session 121), whitelist of six:

- `FrozenColumnCountProperty` → `OnCoerceFrozenColumnCount`
- `AlternationCountProperty` → `OnCoerceAlternationCount`
- `IsSynchronizedWithCurrentItemProperty` → `OnCoerceIsSynchronizedWithCurrentItem`
- `CanUserAddRowsProperty` → `OnCoerceCanUserAddRows`
- `CanUserDeleteRowsProperty` → `OnCoerceCanUserDeleteRows`
- `VirtualizingPanel.IsVirtualizingProperty` → `OnCoerceIsVirtualizingProperty`

`SetCoerced` helper: run the callback, `SetValue` only when changed.
`ShimCoerceBaseValue` helper: capture the first (pre-coercion) base value per
property — see Findings 4. `ShimIsVirtualizing` readback convenience for the
WPF-only attached DP.

### `ext/wpf` submodule — `DataGrid.cs` `OnIsReadOnlyChanged` / `OnIsEnabledChanged`

Both callers invoke `d.CoerceValue(CanUserAddRowsProperty)` /
`d.CoerceValue(CanUserDeleteRowsProperty)` where `d` is statically typed
`DependencyObject` — that binds to the base no-op, never the `DataGrid`
override. Patched to `((DataGrid)d).CoerceValue(...)` (see Findings 3).

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
- `datagrid.probe.set-selection-unit(unit)` — sets `IsSynchronizedWithCurrentItem
  = true` then `SelectionUnit` (cell/cellorrownheader/fullrow). Cell unit must
  coerce the sync flag to false (upstream OnSelectionUnitChanged :4587 calls
  `CoerceValue`).
- `datagrid.probe.set-read-only(bool)` — toggles `IsReadOnly`; reports
  CanUserAddRows/CanUserDeleteRows. The `OnIsReadOnlyChanged` callback coerces
  both (upstream :2861-2862).
- `datagrid.probe.set-enable-row-virtualization(bool)` — toggles
  `EnableRowVirtualization`; reports the coerced `VirtualizingPanel.IsVirtualizing`
  attached DP via `ShimIsVirtualizing`.

### Tests (`tests/DataGrid.IntegrationTests/DataGridIntegrationTests.cs`)

- `Coercion_FrozenColumnCountClampsToColumnCount` — set 99, expect clamp to
  `Columns.Count`.
- `Coercion_AlternationCountPromotesToTwoWhenAlternatingBackgroundSet` —
  `create-alternating-row-grid`, expect `AlternationCount >= 2`.
- `Coercion_IsSynchronizedWithCurrentItemForcedOffInCellSelectionUnit` —
  `create-grid`, then `set-selection-unit cell` (expect false) and
  `fullrow` (expect true).
- `Coercion_CanUserAddDeleteRowsForcedOffWhenReadOnly` — `create-grid`, then
  `set-read-only true` (expect both false), then `set-read-only false` (expect
  both restored to true).
- `Coercion_RowVirtualizationMirrorsEnableRowVirtualization` — `create-grid`,
  then `set-enable-row-virtualization false` (expect IsVirtualizing false),
  then `true` (expect true).

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
   `CoerceValue`. `IsSynchronizedWithCurrentItem` gets its trigger for free:
   setting `SelectionUnit` fires `OnSelectionUnitChanged`, which calls
   `CoerceValue(IsSynchronizedWithCurrentItemProperty)`. `IsVirtualizing`
   likewise: `OnEnableRowVirtualizationChanged` calls
   `dataGrid.CoerceValue(VirtualizingPanel.IsVirtualizingProperty)` (:8180).
3. **Explicit-receiver call sites with `DependencyObject d` bind to the base
   no-op.** Slices 1-2 worked because the call sites use an implicit receiver
   (inside `DataGrid` methods) or a `DataGrid`-typed local (`dataGrid.CoerceValue`).
   `OnIsReadOnlyChanged`/`OnIsEnabledChanged` pass `d` typed as
   `DependencyObject`, so the `new` override is invisible to the binder.
   Patched the two call sites in the `ext/wpf` submodule (its HAS_UNO history
   already includes local patches). Pending: :930/:1045 (`ItemContainerStyle` /
   `ItemContainerStyleSelector`) still use the un-cast form — dormant anyway.
4. **Coercion base value must be pre-coercion.** `OnCoerceCanUserAddOrDeleteRows`
   only validates when `baseValue` is true; after a first coercion `SetValue`
   writes the coerced value into the local layer, so the next read-back sees
   `false` and the restore direction never recovers. WPF distinguishes base
   value from current value; Uno has no such layer. `ShimCoerceBaseValue`
   captures the first value seen per property and reuses it (falling back to
   `ReadLocalValue` only if the user set the property before any coercion ran).
5. **Debugging trace** (`ShimCoerceCallCount` / `ShimCoerceAlternationTrace`)
   showed `CoerceValue` ran, the value was coerced to 2 in the DP, yet the
   readback said 0 — the `ItemsControl.AlternationCount` auto-property was the
   culprit, not the property system.

## Verification

- `Coercion_*` tests: green (FrozenColumnCount failed → column-add trigger;
  AlternationCount failed → auto-property; CanUser* failed → explicit-receiver
  binding fix + base-value capture; IsVirtualizing passed on first run).
- Full DataGrid suite: 72/72 (67 + 5 new).
- RichTextBox 238/238, model tests 234/234: unchanged.

## Next

Slice 5 options (todo item 5): `DataGridColumn.CanUserSort`/`CanUserReorder`/
`CanUserResize` (DataGridHelper transfer-based, needs
`GetCoercedTransferPropertyValue` to be meaningful in the shim), or
`DataGridCell.Clip` / `DataGridRow.ShouldCacheContainerSize`. The
`ItemContainerStyle`/`ItemContainerStyleSelector` and width/frozen callbacks
stay dormant until the parallel logic is retired.
