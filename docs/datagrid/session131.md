# Session 131 — item 5 completion: coercion activation, slices 6-10

Extends session 130's per-property coercion activation across the remaining
controls with real `CoerceValueCallback` call sites. Slices 6-10:

## Slice 6 — DataGrid.ItemContainerStyle / ItemContainerStyleSelector

- Patched `ext/wpf` DataGrid.cs:930/:1048 — both call sites pass `d` statically
  typed `DependencyObject`, which bound to the base no-op; now
  `((DataGrid)d).CoerceValue(...)`.
- `DataGrid` shim whitelist gained `ItemContainerStyleProperty` and
  `ItemContainerStyleSelectorProperty`, invoking `OnCoerceItemContainerStyle`
  / `OnCoerceItemContainerStyleSelector` directly (both return `RowStyle` /
  `RowStyleSelector` when non-default; the style family was verified safe to
  activate — the shim's `ApplyShimRowStyle` reads `RowStyle` itself, and the
  coerced `ItemContainerStyle` is only consumed by WPF-linked code).
- Probes: `set-row-style`, `set-row-style-selector`. The style is constructed
  as `Microsoft.UI.Xaml.Style` (no WPF `System.Windows.Style` type in the
  shim) and `System.Windows.Controls.StyleSelector` (plain class, compiles in).
- Tests: `Coercion_RowStyleDrivesItemContainerStyle`,
  `Coercion_RowStyleSelectorDrivesItemContainerStyleSelector`.

## Slice 7 — DataGridRow.Visibility

- `DataGridRow` whitelist gained `VisibilityProperty` → `OnCoerceVisibility`
  (upstream DataGridRow.cs:724; returns `PlaceholderVisibility` for the
  new-item placeholder row). Trigger sites: `PrepareRow` :982 and
  `UpdateNewItemPlaceholder` :3877 (receiver typed `DataGridRow`, binds to the
  override — no submodule patch).
- Probe: `set-can-user-add-rows`; test:
  `Coercion_PlaceholderRowVisibilityMirrorsCanUserAddRows`. Verified WPF
  semantics: with `CanUserAddRows=false` the placeholder is removed from the
  collection entirely (`NewItemPlaceholderPosition=None`), so the "hidden"
  assertion searches for the row via `IsNewItem` instead of the visibility.

## Slice 8 — DataGridColumn

- Replaced the shim's public no-op `CoerceValue` stub with a whitelist:
  - `ActualWidthProperty` → `OnCoerceActualWidth` (:484; clamps min/max,
    absolute widths take `Width.DisplayValue`).
  - `MaxWidthProperty` → `OnCoerceMaxWidth` (:432; star columns capped at
    `_starMaxWidth`). Uses `ShimCoerceBaseValue` (session 130 finding 4
    recurred: once coerced, the capped value would become the base on the next
    call — capture the first value per property; MaxWidth falls back to
    `double.PositiveInfinity`).
  - `IsFrozenProperty` → `OnCoerceIsFrozen` (:1274; `DisplayIndex <
    FrozenColumnCount`). Trigger: `DataGridColumnCollection.InsertItem`
    (:58/:84, receiver typed `DataGridColumn`, binds here).
  - Template `CanUserSortProperty` → `OnCoerceTemplateColumnCanUserSort`
    (forced off without `SortMemberPath`). Registered via `OverrideMetadata`
    (no-op), so invoked directly; the callback was made `internal` and is
    called type-qualified (`DataGridTemplateColumn.OnCoerceTemplateColumnCanUserSort`).
- Probes: `set-column-width`, `set-min-width`, `is-frozen-readback`,
  `template-can-user-sort`.
- Gotchas: `DataGridLengthUnitType` is `Pixel` (not `Absolute`); `Jn` renders
  `Infinity` as the string `"Infinity"` so the test must use `GetString()`;
  the shim's parallel auto-size (`ShimTryAutoSizeColumn`) resolves Auto
  columns to pixels immediately, so the ActualWidth test avoids unit
  assertions; `IsFrozen` readback is comma-joined (`"1,1,0,..."`).
- Tests: `Coercion_MaxWidthCappedOnStarColumn`,
  `Coercion_ActualWidthClampedByMinWidth`,
  `Coercion_IsFrozenFollowsFrozenColumnCount`,
  `Coercion_TemplateColumnCanUserSortForcedOffWithoutSortMemberPath`.

## Slice 9 — DataGridCell.Clip

- `DataGridCell` (shim partial, derives from shim `ContentControl`) gained an
  `internal new void CoerceValue` whitelisting `ClipProperty` →
  `OnCoerceClip` (upstream DataGridCell.cs:1054). `GetFrozenClipForCell`
  returns null without a `DataGridCellsPanel` parent, so outside the frozen
  boundary the coercion is a pure no-op (verified by
  `Coercion_CellClipAbsentWithoutFrozenColumns`).
- Patched `ext/wpf` DataGridCellsPanel.cs:1301/:1305 — the OldClippedChild /
  NewClippedChild receivers are statically typed `UIElement`, which bound to
  the base no-op; now guarded `is DataGridCell` casts.
- The clip only materializes when a non-frozen cell straddles the frozen
  boundary (`ArrangeChild` :1518), which happens during horizontal scroll
  (`ViewportStartX` = HorizontalScrollOffset - CellsPanelHorizontalOffset).
  The probe drives `DataGrid.HorizontalScrollOffsetProperty` directly (the
  shim's scroll path doesn't bind it), then reads cell (0,1)'s `Clip`.
- Probe: `clip-readback`; tests: `Coercion_CellClipCoercedForFrozenColumn`
  (clip rect starts at the scroll offset), `Coercion_CellClipAbsentWithoutFrozenColumns`.

## Slice 10 — DataGridRowHeader.IsRowSelected

- `DataGridRowHeader` (shim partial) gained `internal new void CoerceValue`
  whitelisting `IsRowSelectedProperty`. The upstream `OnCoerceIsRowSelected`
  walks `TemplatedParent` (`DataGridHelper.FindParent`), which the shim's
  manually-placed header doesn't provide, so the override reads
  `EffectiveRow` (session 122's visual-parent + explicit-owner fallback)
  instead.
- Two link repairs were needed for the trigger chain:
  - `AddOwner` is a no-op shim, so upstream `OnIsSelectedChanged` (the only
    code forwarding to `RowHeader.NotifyPropertyChanged`, :1062) never fires.
    The shim's existing `RegisterPropertyChangedCallback(IsSelectedProperty)`
    hook (which already replicated visuals + Selected/Unselected events) now
    also coerces the header mirror.
  - `DataGridRowHeader` derives from `Microsoft.UI.Xaml.Controls.Primitives.ButtonBase`,
    not the shim `Control`, so `SetValue(DependencyPropertyKey, ...)` doesn't
    exist — use `SetValue(key.DependencyProperty, ...)`.
- Probe: `row-header-is-selected-readback`; test:
  `Coercion_RowHeaderIsRowSelectedMirrorsRowSelection` (select → true,
  deselect → false).

## Regression

- Full DataGrid suite: **83/83** (was 73 in session 130; +10 new coercion
  tests).
- Two pre-existing resize tests failed after slice 8 because
  `OnCoerceActualWidth` now forces a pixel column's `ActualWidth` to its
  `Width.DisplayValue` (50) instead of the shim's old estimated width —
  `ColumnResize_ChangesWidth` and `HeaderGripperDrag_ChangesWidth` resized to
  40px and asserted growth. Updated both to 100px. Verified via a baseline
  worktree (f70901b + current submodule) that the tests passed before slice 8
  — this was a genuine behavior change, not a pre-existing failure.
- RichTextBox suite: **238/238**. Model unit suite: **234/234**.

## Files touched

- `ext/wpf/.../Controls/DataGrid.cs` — :930/:1048 cast patches (slice 6)
- `ext/wpf/.../Controls/DataGridTemplateColumn.cs` — `OnCoerceTemplateColumnCanUserSort` internal (slice 8)
- `ext/wpf/.../Controls/DataGridCellsPanel.cs` — :1301/:1305 `is DataGridCell` casts (slice 9)
- `src/LeXtudio.Windows/System.Windows/Controls/DataGrid.cs` — whitelist + ItemContainerStyle/Selector (slice 6)
- `src/LeXtudio.Windows/System.Windows/Controls/DataGridRow.cs` — Visibility whitelist (7), IsSelected→header hook (10)
- `src/LeXtudio.Windows/System.Windows/Controls/DataGridColumn.cs` — whitelist + `ShimCoerceBaseValue` (8)
- `src/LeXtudio.Windows/System.Windows/Controls/DataGridCell.cs` — Clip whitelist (9)
- `src/LeXtudio.Windows/System.Windows/Controls/Primitives/DataGridRowHeader.cs` — IsRowSelected whitelist (10)
- `tests/DataGrid.IntegrationTestHost/MainPage.cs` — 9 new probes
- `tests/DataGrid.IntegrationTests/DataGridIntegrationTests.cs` — 10 new tests, 2 fixed resize tests
- `docs/datagrid/todo.md` — item 5 marked DONE
