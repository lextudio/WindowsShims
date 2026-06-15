# DataGrid Port Plan

This file tracks the WPF `System.Windows.Controls.DataGrid` source-first port to
`LeXtudio.Windows`, following the RichTextBox pattern: link upstream WPF source
when it can compile cleanly, add narrow Uno bridges where the WPF framework
contract is missing, and avoid local rewrites until a source file proves too
coupled for the current milestone.

## Current Baseline

- Project: `src/LeXtudio.Windows/LeXtudio.Windows.csproj`
- Upstream WPF source root:
  `ext/wpf/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls`
- First build target: `net10.0-desktop`
- Session 22 build/test status: green with the upstream `DataGrid.cs`
  control root linked and compiling (`linked-upstream`, fork-guarded
  partial on `HAS_UNO`). The session-12 local shell is reduced to a minimal
  partial (`UpdateVisualState`); presenters, virtualizing-panel stubs, a
  WPF-shaped `Dispatcher` shadow on the spine, and the remaining helper
  internals were added as bridges. See `session22.md` for the inventory.
- Session 23 runtime status: new sample head
  `src/LeXtudio.Windows.Sample` with a `--probe` headless gate. All probe
  steps pass — the WPF static/instance constructors, column collection,
  items, selection/command surface, visual-tree attach, and measure all
  survive on Uno. Characterization: `DesiredSize=0,0`, zero visual
  children, no row containers — the next rungs are row generation and a
  visual pipeline (see `session23.md`).
- Session 24: rebased the foundational shim `ItemsControl` (and thus the
  whole `DataGrid → MultiSelector → Selector → ItemsControl` tower) from
  WinUI `FrameworkElement` onto WinUI `Control`, unlocking the template
  pipeline. Cost was 4 cleanable hiding warnings (redundant `IsEnabled`/
  `DefaultStyleKey`/`IsTabStop` DPs, now inherited from `Control`); the
  hand-built-visual alternative is dropped. 106 tests green; probe
  unchanged. Next rung: a default template/style + row container
  generation (see `session24.md`).
- Session 25: **the DataGrid renders.** A code-built `ControlTemplate`
  (assigned directly via `XamlReader`, sidestepping unresolved library
  generic.xaml packaging) hosts `PART_ShimRowsHost`, which the shim fills
  with a header row + one data row per item; cells are produced by the
  column's real element-generation path and data-bound to each item. Probe:
  4 host children, `DesiredSize=386×96` (was `0×0`). 107 tests green.
  Deliberately simple: no virtualization, no real container generation, no
  change-reactivity yet (see `session25.md`).
- Session 26: rows became real `DataGridRow` containers that host their own
  cells (`PART_CellsHost`; `TryGetCell` returns generated cells), and the
  grid now re-renders on `Items`/`Columns` changes. Probe: 3 cells per row,
  adding an item grows the host (reactivity), `DesiredSize` tracks row count.
  109 tests green. Still no `ItemContainerGenerator` containers, no
  `DataGridColumnHeader`, flat column widths, no virtualization/selection
  (see `session26.md`).
- Session 27: `ItemContainerGenerator` gained a real container registry the
  render path populates; `ContainerFromItem`/`ContainerFromIndex`/
  `IndexFromContainer`/`ItemFromContainer` and `ContainerFromItemInfo` now
  resolve the rendered `DataGridRow`s, and `Status` reports
  `ContainersGenerated`. Probe verifies the index/item round-trip. 111 tests
  green. Resolution is wired but consumers (selection, scroll-into-view, row
  details) are still inert — the next behavioral rungs (see `session27.md`).
- Session 28: row selection is visible and interactive. `DataGridRow.
  IsSelected` paints a highlight (template `Border` bound to `Background`);
  pointer press routes to `DataGrid.HandleShimRowClicked` (shim single-select)
  which clears other rows, highlights the clicked row, and sets `SelectedItem`.
  Probe verifies the flip + highlight. 112 tests green. Shim-driven only — no
  multi-select, cell-selection unit, keyboard selection, or full WPF
  `Selector` pipeline yet (see `session28.md`).
- Session 29: header row uses real `DataGridColumnHeader` controls and
  explicit pixel column widths are honored (`ShimColumnWidth` reads
  `DataGridLength.IsAbsolute`). Probe: a `Width=60` column renders 60px wide.
  113 tests green. Only absolute widths honored — `Auto`/`Star` still fall
  back to 120; no header sort/resize/reorder yet (see `session29.md`).
- Session 30: header-click sorting. `HandleShimHeaderClicked` toggles the
  column `SortDirection` (single key) and `OrderedItems` re-renders rows
  sorted by the column's reflected value; the active column shows a ▲/▼
  glyph. Probe verifies ascending/descending order. 114 tests green.
  Shim-side only — bypasses the WPF `Sorting`/`SortDescriptions` pipeline,
  single key, selection not preserved across rebuild (see `session30.md`).
- Session 31: selection survives render rebuilds. `_shimSelectedItem` retains
  the selected item by identity; `BuildShimVisualTree` re-applies `IsSelected`
  to the rebuilt row holding that item, so the highlight follows the item
  through sort/reactivity. Probe verifies highlight survives a re-sort. 115
  tests green. Single-item retention; stale reference not cleared on removal
  yet (see `session31.md`).
- Session 32: removing the selected item clears the selection.
  `BuildShimVisualTree` drops `_shimSelectedItem` and resets `SelectedItem`
  when no rebuilt row matches the retained item. Probe verifies `SelectedItem`
  becomes null after removing the selected item. 115 tests green (16 probe
  steps). No "select neighbor on delete" behavior yet (see `session32.md`).
- Session 33: keyboard navigation. `OnKeyDown` Up/Down →
  `MoveSelectionByOffset`, reusing the single-select path. Probe verifies
  Down/Up move the highlight. 116 tests green (17 probe steps). Up/Down only,
  no `BringIntoView` on navigation yet (see `session33.md`).
- Session 34: selection scrolls into view (`DataGridRow.BringIntoView` →
  `StartBringIntoView`, called on select), and Home/End/PageUp/PageDown
  navigation added (`MoveSelectionToIndex`, page=5). Probe verifies Home/End.
  116 tests green (18 probe steps). Fixed page size, no viewport measure (see
  `session34.md`).
- Session 35: cell-level selection honoring `SelectionUnit`. Cell pointer
  press → `HandleShimCellClicked`: FullRow routes to row selection, Cell
  selects the single cell (own highlight) and clears row selection. Probe
  verifies single-cell highlight under `SelectionUnit.Cell`. 116 tests green
  (19 probe steps). Cell selection not retained across rebuilds (see
  `session35.md`).
- Session 36: shim cell selection now writes `CurrentCell` and `SelectedCells`
  (single `DataGridCellInfo`). Probe verifies `CurrentCell.Column` and a
  one-entry `SelectedCells`. 116 tests green. Single-cell only; FullRow
  doesn't populate cells; `*Changed` events not raised (see `session36.md`).
- Session 37: cell selection retained across rebuilds by (item, column).
  `TryReselectCell` re-applies the highlight as rows rebuild their cells.
  Probe verifies the cell stays selected after a sort. 116 tests green. Stale
  retention not cleared on item removal yet (see `session37.md`).
- Session 38: retained cell selection clears when its item leaves the
  collection (`CurrentCell`→Unset, `SelectedCells` cleared). Probe verifies.
  116 tests green. Single-selection model now coherent end-to-end; multi-
  select/range/`*Changed`/WPF `Selector` pipeline still out (see
  `session38.md`).
- Session 62 follow-up: cell clicks now route through linked
  `HandleSelectionForCellInput` / `MakeCellSelection` after the shim supplies
  the missing focus/current-cell side effect. `_shimSelectedCell*` is retired;
  realized cell visuals reconcile from `CurrentCell` / `SelectedCells` by
  item+column across rebuilds and prune both surfaces on item removal. Add-new
  placeholder editing keeps an explicit column fallback after current-cell
  pruning. 126 tests green; probe `DONE failures=0`.
- Session 39: text-cell editing. Double-click/F2 → `TextBox`; Enter commits
  (reflection write-back with type conversion), Escape cancels. Probe edits
  Age→99 and verifies write-back + display restore. 117 tests green.
  Reflection-based (no WPF editing-binding/`IEditableObject`/validation/edit
  events); text columns only; no commit-on-blur (see `session39.md`).
- Session 40: multi-row selection (Extended mode). `HandleShimRowClicked(row,
  modifiers)` — Ctrl toggles, Shift ranges from anchor, plain click resets;
  `ShimSelectedItems` exposes the set. Probe verifies Ctrl/Shift/plain. 118
  tests green. Shim-side (WPF `SelectedItems`/`SelectionChanged` not driven);
  no Shift+Arrow yet (see `session40.md`).
- Session 41: `Auto` column width. Non-absolute columns auto-size; a one-shot
  post-layout pass (`OnAutoWidthLayoutUpdated`) sets a uniform per-column
  width to the widest realized content (header+cells aligned). Probe: Auto
  Name column = 49px header==cell. 119 tests green (see `session41.md`).
- Session 42: `Star` width distribution + `MinWidth`/`MaxWidth` clamping. The
  width pass distributes remaining viewport width among star columns by
  weight and clamps all widths. Probe: a `*` City column fills (357/486),
  honoring an 80px floor. 119 tests green. Star budget is a proxy
  (`ActualWidth-2`); no reflow on resize; `SizeToCells/Header` ≈ Auto (see
  `session42.md`).
- Session 43: editing hardening (themed batch). Read-only coercion
  (`DataGrid`/column `IsReadOnly` block edits), cancelable `BeginningEdit` /
  `CellEditEnding` events (shim raises the linked events; veto blocks/keeps
  editing), and commit-on-blur (`LostFocus`). Probe verifies read-only +
  both event cancellations + clean commit. 119 tests green. No
  `RowEditEnding`/`IEditableObject`/validation; text columns only (see
  `session43.md`).
- Session 44: first non-text column type. `DataGridCheckBoxColumn` renders a
  bound `CheckBox` (read-only-aware) and toggling writes back via reflection.
  Probe: 4 cells/row, toggle flips `IsActive`. 120 tests green. Checkbox
  edits in place (bypasses edit-lifecycle events); other column types still
  display-only (see `session44.md`).
- Session 45: `DataGridComboBoxColumn` render + selection write-back. Keeps
  the display bindings and writes the chosen value back via the effective
  binding target (SelectedItem→SelectedValue→Text), read-only-aware. Probe:
  5 cells/row, selecting "Paris" writes `City`. 121 tests green. Edits in
  place (bypasses edit-lifecycle); free-text combo entry not written (see
  `session45.md`).
- Session 46: validation surface. `DataGridCell.CommitEdit` validates the
  written value via `IDataErrorInfo`; an error keeps the cell editing, paints
  a red border + tooltip, and exposes `HasValidationError`/`ValidationError`;
  a valid value clears it. Probe: Age 999 flags, 42 clears. 121 tests green.
  `IDataErrorInfo` only; no row validation / `ValidationRule` / error template;
  checkbox/combo writes unvalidated (see `session46.md`).
- Session 47: row edit transactions. `BeginRowEdit`/`CommitRowEdit`/
  `CancelRowEdit` drive `IEditableObject` (snapshot/commit/revert) + the
  cancelable `RowEditEnding` event, tied into the cell edit lifecycle. Probe:
  commit fires EndEdit+RowEditEnding(Commit); cancel fires CancelEdit+revert.
  121 tests green. Single-cell transaction span; no `RowValidationRules` yet
  (see `session47.md`).
- Session 48: row-level validation. `CommitRowEdit` runs
  `DataGrid.RowValidationRules`; a failing rule flags the row (red border,
  `HasRowValidationError`/`RowValidationError`) and blocks the commit; a clean
  pass clears it. Probe: Age=10 fails (≥18 rule), Age=30 passes. 121 tests
  green. Border indicator only; no row-header glyph / `Validation.Errors`
  (see `session48.md`).
- Session 49: row headers. A separate `PART_RowHeader` renders a
  `DataGridRowHeader` (honoring `HeadersVisibility`, width `RowHeaderWidth`)
  with a current/editing/error glyph (▶/✎/⚠); a corner placeholder keeps
  columns aligned, and cells stay column-indexed. Probe: selected row shows ▶.
  121 tests green. Glyph-only, no header style/resize/drag-select (see
  `session49.md`).
- Session 51: reuse substrate — WPF command routing. `CommandBinding.
  AppliesTo` now walks the visual tree, so a class-scoped command (bound to
  `DataGrid`) routes to a descendant target (a cell) as in WPF — the
  prerequisite for editing/selection command reuse. Probe proves routing;
  122 tests green. Investigation: `OnExecutedBeginEdit` pulls in heavy
  unshimmed substrate (editable-items/placeholder/selected-cell regions), so
  full editing reuse is staged, not forced (see `session51.md`).
- Session 52: **reuse milestone — commit editing via the real WPF command
  path.** `CurrentCellContainer` is now kept in sync when a cell begins
  editing; cell-originated commits route through
  `DataGrid.CommitEdit(DataGridEditingUnit.Row, true)`; the linked
  `OnExecutedCommitEdit` now owns `CellEditEnding` / `RowEditEnding` /
  row-commit orchestration. Added invocation-target resolution for class
  command bindings, bridged `IEditableObject` through `ItemCollection`'s
  editable-view surface, and wired `RowValidationRules` into the routed commit
  path. Probe proves single event counts, `IEditableObject` commit/cancel, and
  row-validation blocking while keeping the cell editing. 122 tests green
  (see `session52.md`).
- Session 53: **reuse milestone — cancel editing via the real WPF command
  path.** `DataGridCell.CancelEdit()` now routes through
  `DataGrid.CancelEdit(DataGridEditingUnit.Row)`; the linked
  `OnExecutedCancelEdit` owns `CellEditEnding(Cancel)` /
  `RowEditEnding(Cancel)` / `BindingGroup.CancelEdit` / row rollback, while a
  new `ShimExecutingCancelEditCommand` guard keeps the cell callback to local
  teardown only. Probe proves one cancel cell-ending event, one cancel
  row-ending event, `IEditableObject.CancelEdit`, and clean exit from edit
  mode. 122 tests green (see `session53.md`).
- Session 54: **reuse milestone — begin editing via the real WPF command
  path.** `DataGridCell.BeginEdit()` now delegates to
  `DataGrid.BeginEdit(...)`, so the linked `OnExecutedBeginEdit` owns
  `BeginningEdit`, row-edit startup (`EditRowItem`), `BindingGroup.BeginEdit`,
  and row editing state; a new `ShimExecutingBeginEditCommand` guard keeps the
  cell callback to local editor creation only. This completes routed WPF
  ownership for begin/commit/cancel on the common non-placeholder path. Probe
  now asserts one successful `BeginningEdit`. 122 tests green; probe green
  (`DONE failures=0`) (see `session54.md`).
- Session 55: **add-new substrate batch.** `ItemCollection` now implements
  `IEditableCollectionViewAddNewItem` and owns placeholder/add-new state:
  `NewItemPlaceholderPosition`, `AddNewItem`, `AddNew`, `CommitNew`,
  `CancelNew`, `CurrentAddItem`, and placeholder preservation across sort.
  The shim render path re-synchronizes placeholder visibility before rebuilds,
  and `UpdateLayout()` now forces a shim rebuild first so `CanUserAddRows`
  changes surface visually. Probe verifies placeholder appearance plus direct
  add-new commit/restore behavior; tests and probe are green. The remaining
  rung is the full placeholder-cell `BeginEdit` path through upstream WPF
  `OnExecutedBeginEdit` (see `session55.md`).
- Sessions 61-62: row selection state and visuals moved onto the linked
  Selector/MultiSelector engine. `SelectedItems`, `SelectedItem`, and
  `SelectionChanged` are populated by the real engine, and realized row
  highlights are reflected from `SelectionChanged` / rebuild-time
  `SelectedItems` state instead of a manual visual pass.
- Session 62 follow-up: row clicks now route through the linked WPF
  `HandleSelectionForRowHeaderAndDetailsInput` -> `MakeFullRowSelection` path.
  Uno pointer modifiers are bridged into `Keyboard.Modifiers` for the duration
  of the call, so plain/Ctrl/Shift clicks use WPF's row selection logic.
  `_shimSelectedItems`, `_shimAnchorItem`, `_shimSelectedItem`, and
  `SyncRealSelectedItems()` are retired; probes assert `SelectedItems`
  directly. 126 tests green and probe reports `DONE failures=0` (see
  `session62.md`).
- Session 63: **base column source-link batch.** `DataGridColumn.cs` is now
  linked from upstream WPF with a guarded `partial` declaration. The local
  partial keeps only Uno bridge helpers (`BuildCellContent`,
  `BuildEditingCellContent`, `SetValue(DependencyPropertyKey, ...)`, and a
  no-op `CoerceValue`), while `DataGridHelper` / `DataGridColumnCollection`
  gained narrow compatibility contracts for the upstream body. Because the Uno
  DP layer does not run WPF coercion callbacks, linked `DefaultSort` has a
  `HAS_UNO` fallback to `DataGridBoundColumn.EffectiveSortMemberPath`. 126
  tests green and probe reports `DONE failures=0` (see `session63.md`).
- Session 64: **column collection display-index batch.** The local
  `DataGridColumnCollection` bridge now carries the WPF display-index map
  model: add/remove/replace/move maintenance, duplicate/out-of-range
  validation, `ColumnFromDisplayIndex`, selected-cell column-change
  notifications, and frozen-column updates. Because Uno does not raise WPF DP
  change callbacks for direct `column.DisplayIndex = ...`, the map refreshes
  from current column values at render/map boundaries. The shim render path now
  realizes headers and cells in display-index order. 126 tests green and probe
  reports `DONE failures=0` with a new display-index reorder step (see
  `session64.md`).
- Session 70: **notification probe batch.** Reused the live WPF DP notification
  bridge from sessions 67-69 to pin three more linked property paths:
  `RowBackground` live row updates, `GridLinesVisibility` no-crash callback
  execution, and `CanUserResizeColumns` / `CanUserReorderColumns` option
  notifications. No new rendering substrate was required; the sample probe now
  covers 45 steps. 136 tests green and probe reports `DONE failures=0` (see
  `session70.md`).
- Session 71: **initial frozen-column notification batch.** Reused linked WPF
  `FrozenColumnCount` / `DataGridColumnCollection` updates and added narrow
  Uno-side markers on realized `DataGridColumnHeader` and `DataGridCell`
  visuals. The shim computes realized frozen state from `DisplayIndex <
  FrozenColumnCount` because WPF notifies rows/cells before the collection
  target updates `DataGridColumn.IsFrozen`. This pins live frozen-state
  propagation without implementing frozen layout/scrolling. 136 tests green;
  probe covers 46 steps and reports `DONE failures=0` (see `session71.md`).
- Session 72: **style notification batch.** Reused linked WPF notification
  callbacks for `CellStyle` and `ColumnHeaderStyle`, added a narrow `HAS_UNO`
  row notification after `RowStyle` coerces `ItemContainerStyle`, and exposed
  realized-style markers on rows, cells, and column headers. The current scope
  is style-object propagation to shim visuals; style setter application and
  selectors remain deferred. 136 tests green; probe covers 47 steps and
  reports `DONE failures=0` (see `session72.md`).
- Session 73: **column style precedence batch.** Realized cells now compute
  effective style as `DataGridColumn.CellStyle ?? DataGrid.CellStyle`, and
  realized column headers use `DataGridColumn.HeaderStyle ??
  DataGrid.ColumnHeaderStyle`. The linked column notification callbacks already
  route these changes to cells/headers; the shim now updates its realized-style
  markers live and verifies column override plus fallback. 136 tests green;
  probe covers 48 steps and reports `DONE failures=0` (see `session73.md`).
- Session 74: **visible grid-line rendering batch.** Reused linked WPF
  `GridLinesVisibility` / grid-line brush callbacks, which regenerate item
  containers through `OnItemTemplateChanged`, and mapped the state to realized
  cell borders in the shim render path. Cells now draw bottom/right borders
  for horizontal/vertical/all modes while preserving current-cell and
  validation border priority. 136 tests green; probe reports
  `DONE failures=0` with visible grid-line assertions (see `session74.md`).
- Session 75: **header grid-line completion batch.** Extended the session-74
  grid-line rendering to realized `DataGridColumnHeader` and
  `DataGridRowHeader` surfaces, still using the linked WPF
  `GridLinesVisibility` container-regeneration callback. The sample now
  verifies cell, column-header, and row-header borders for none/horizontal/
  vertical/all modes. 136 tests green; probe reports `DONE failures=0` (see
  `session75.md`).
- Session 76: **row style-selector reuse batch.** Linked upstream WPF
  `StyleSelector.cs`, added the missing `ItemsControl` style/selector CLR
  accessors, and routed linked `RowStyleSelector` changes to realized rows.
  Rows now compute effective style with WPF precedence:
  `RowStyle ?? ItemContainerStyle ?? (RowStyleSelector ??
  ItemContainerStyleSelector)?.SelectStyle(...)`. Setter application remains
  deferred, but selector invocation and explicit-style override are verified.
  136 tests green; probe reports `DONE failures=0` (see `session76.md`).
- Session 91: **cells-presenter reuse batch.** Linked upstream WPF
  `DataGridCellsPresenter.cs` plus its `MultipleCopiesCollection` item-source
  helper. The local presenter stub is gone; the linked presenter now owns item
  mirroring, column-change sync, cell tracking, and notification propagation.
  Narrow Uno bridges provide direct-hosted-row owner fallback, cells-panel
  invalidation hooks, and `DataGridCell.PrepareCell`/`ClearCell` glue back to
  the existing shim visual path. WPF `DrawingContext` grid-line rendering is
  still guarded because the shim draws grid lines through border thickness.
  Build, 136 tests, and probe are green (see `session91.md`).
- Session 92: **rows-presenter reuse batch.** Linked upstream WPF
  `DataGridRowsPresenter.cs` and deleted the last local presenter stub file.
  Added narrow `VirtualizingStackPanel`/`CleanUpVirtualizedItemEventArgs`,
  `IItemContainerGenerator.GetItemContainerGeneratorForPanel`,
  `ItemsControl.GetItemsOwner`, and `Validation.GetHasError` bridges. The
  linked presenter now owns the WPF row-host surface for `InternalItemsHost`,
  viewport-size notification, bring-index-into-view, and validation cleanup
  policy. Current templates still use the direct shim rows host, so this is
  compile/runtime substrate for the later panel/template swap. Build, 136
  tests, and probe are green (see `session92.md`).
- Session 93: **header-drag visual dependency batch.** Linked upstream WPF
  `DataGridColumnDropSeparator.cs` and `DataGridColumnFloatingHeader.cs`,
  with a minimal `VisualBrush`/`BrushMappingMode` shim and
  `VisualTreeHelper.GetOffset` compatibility. These types are not used by the
  current shim-native reorder path, but they remove two dependencies blocking
  future `DataGridColumnHeadersPresenter` reuse. Build, 136 tests, and probe
  are green (see `session93.md`).
- Session 94: **column-headers-presenter source-link batch.** Linked upstream
  WPF `DataGridColumnHeadersPresenter.cs` plus
  `DataGridColumnHeaderCollection.cs`, deleting the local presenter shell.
  Added narrow visual-child/layout-clip hooks on `ItemsControl`, a presenter
  automation peer shim, `DoubleUtil` close-comparison helpers, and
  `DataGridColumnCollection.AverageColumnWidth` on the Uno partial. The linked
  presenter compiles and owns the WPF header-generation/notification/drag
  surface, but the live template still uses the manual shim header row until
  the row/header host architecture is swapped. Build, 136 tests, and probe are
  green (see `session94.md`).
- Session 95: **cells-panel guarded source-link batch.** Linked upstream WPF
  `DataGridCellsPanel.cs` and removed the last local `DataGridCellsPanel`
  shell from `DataGridHelperStubs.cs`. The deep WPF measure/arrange,
  generator, recycling, scrolling, and frozen-column layout body remains under
  `#if !HAS_UNO`; the Uno path keeps the minimal surface currently consumed by
  linked presenters (`HasCorrectRealizedColumns`, `InternalBringIndexIntoView`)
  plus typed realized-column block substrate. Build, 136 tests, and probe are
  green (see `session95.md`).
- Session 96: **cells-panel host/validation reuse batch.** Replaced the fake
  Uno `HasCorrectRealizedColumns` path with the real WPF realized-column check
  and ported the light-weight `DataGridCellsPanel` host logic into a Uno
  partial: items-host attach/detach, items-changed child cleanup, parent
  presenter/DataGrid resolution, and realized-children bookkeeping. Added the
  missing `Panel.IsItemsHost` / `InternalChildren` and `VirtualizingPanel`
  internal-child helpers needed by that slice. The heavy WPF measure/generator
  body is still deferred. Build, 136 tests, and probe are green (see
  `session96.md`).
- Session 97: **cells-panel realized-state helper batch.** Ported the linked
  WPF realized-column state accessors and helper methods into the Uno partial:
  block-list getters/setters, rebuild flags, `UpdateRealizedBlockLists`, and
  `BuildRealizedColumnsBlockList`. Also added the panel helpers already queried
  through linked `DataGridHelper` (`ComputeCellsPanelHorizontalOffset`,
  `GetFrozenClipForChild`). This is still below the full panel layout/generator
  layer, but it removes more fake state from the Uno surface. Build, 136
  tests, and probe are green (see `session97.md`).
- Session 98: **cells-panel measure-helper batch.** Ported the next WPF
  helper layer into the Uno partial: estimated column width summation,
  viewport-width discovery, parent-rows-presenter resolution, and the real
  `MeasureChild(UIElement, Size)` routine that drives DataGrid cell/header
  auto-sizing. The full `MeasureOverride` / generator / virtualization path is
  still guarded out on Uno, but this removes another local delta before that
  enablement step. Build, 136 tests, and probe are green (see `session98.md`).
- Session 99: **cells-panel child-generation substrate batch.** Reworked the
  local `ItemContainerGenerator` into a minimal WPF-shaped sequential generator
  (cursor session, `StartAt`, `GenerateNext`, `GeneratorPositionFromIndex`,
  `PrepareItemContainer`, `Remove`, basic recycle queue) and bound it to the
  owning `ItemsControl`. Ported the next `DataGridCellsPanel` helper slice into
  the Uno partial: generator-position mapping, child generation, block
  generation, and realized-child insertion/index translation. The guarded WPF
  `MeasureOverride` body is still off, but the panel now has the generator
  substrate that path expects. Build, 136 tests, and probe are green (see
  `session99.md`).
- Session 100: **cells-panel virtualization/cleanup substrate batch.** Ported
  the linked WPF child-cleanup layer into the Uno partial: `InBlockOrNextBlock`,
  `VirtualizeChildren` (block-list walk batching contiguous cleanup ranges while
  pinning editing/focused/own-container children), `CleanupRange`
  (recycle-or-remove through the session-99 generator), and
  `DisconnectRecycledContainers`. Added the narrow
  `ItemsControl.IsItemItsOwnContainerInternal` bridge the virtualization walk
  needs. This completes the generate (session 99) + cleanup pair as substrate;
  the guarded `MeasureOverride` body is still off. Build, 136 tests, and probe
  are green (see `session100.md`).
- Session 101: **cells-panel full-source reuse milestone.** Removed the outer
  `#if HAS_UNO` fork guard so the entire upstream `DataGridCellsPanel.cs`
  (measure, arrange, generation, virtualization, frozen-column layout) compiles
  on `HAS_UNO`, and deleted the ~750-line sessions 96-101 `DataGridCellsPanel.uno.cs`
  hand-port. Four narrow `#if HAS_UNO` fork-patches cover genuine WinUI gaps
  (cells-panel offset via `VisualTreeHelper.GetOffset`, viewport width via
  `ScrollContentPresenter.ActualWidth`, `Rect` construction, and deferring
  `BringIndexIntoView` scroll plumbing). Added `VirtualizingPanel.ItemContainerGenerator`/
  `MeasureDirty`/`MeasureDuringArrange`, `DoubleUtil.AreClose(Size,Size)`, and a
  local `DataGridHelper.TreeHasFocusAndTabStop`/`KeyboardNavigation.GetIsTabStop`
  bridge. The panel body is now the real WPF source; wiring it as the live
  layout host (vs. the manual shim rows host) remains the next step. Build (129
  warnings), 136 tests, and probe are green (see `session101.md`).
- Session 102: **DataGridHelper link start.** Linked the upstream
  `DataGridHelper.cs` (previously a fully local 327-line shim) as a partial and
  migrated its pure, platform-independent regions — **GridLines**
  (`SubtractFromSize`, `IsGridLineVisible`) and **Notification Propagation**
  (`ShouldNotify*`/`TestTarget`) — to the real WPF source, removing the matching
  local duplicates (verified byte-equivalent). The engine-coupled regions (tree/
  cells-panel helpers, property-transfer/coercion, binding-expression internals,
  flow-direction caching) stay under `#if !HAS_UNO` with the local shim, notably
  `TransferProperty`/`OnColumnWidthChanged` which carry real Uno render behavior.
  Build (129 warnings), 136 tests, and probe are green (see `session102.md`).
- Session 103: **DataGridHelper link continued.** Replaced the region-wide
  guard with per-method `#if !HAS_UNO` and migrated `FindVisualParent` plus the
  cells-panel accessors (`GetParentPanelForCell`, `GetFrozenClipForCell`,
  `GetParentCellsPanelHorizontalOffset`) to the linked upstream — the latter now
  genuine reuse since session 101 fully linked `DataGridCellsPanel`
  (behavior-equivalent to the prior `null`/`0` shims). `FindParent`
  (templated-parent vs visual-tree walk), `TreeHasFocusAndTabStop`
  (`UIElement.Focusable`/`ContentElement` deps), `OnColumnWidthChanged` (Uno
  render behavior), and the property-transfer/binding engine stay guarded with
  the local shim. Build (129 warnings), 136 tests, and probe are green (see
  `session103.md`).
- Session 104: **DataGridHelper link continued.** Moved the `Focusable` shim
  extension from `FrameworkElement` to `UIElement` and migrated
  `TreeHasFocusAndTabStop` (only its `ContentElement` branch stays inline
  `#if !HAS_UNO`), plus the Other Helpers `AreRowHeadersVisible`,
  `HasNonEscapeCharacters`, `IsImeProcessed`, and `_escapeChar`, to the linked
  upstream; removed the matching local shims (incl. the session-101
  `TreeHasFocusAndTabStop`). `CoerceToMinMax` stays local (upstream returns NaN
  vs the shim's NaN→0 clamp), alongside `FindParent`, `OnColumnWidthChanged`, and
  the property-transfer/binding engine. Build (129 warnings), 136 tests, and
  probe are green (see `session104.md`).
- Session 105: **DataGridHelper link — property-source substrate.** Fixed the
  `DependencyPropertyHelper.GetValueSource` bridge (was hardcoded to `Local`) to
  report `Default` vs `Local` from `ReadLocalValue`, and expanded
  `BaseValueSource` to the full WPF enum. Migrated `IsDefaultValue` to the linked
  upstream (removing the equivalent local shim); the value-source bridge is now
  truthful, also making linked `DataGridRow.PersistAttachedItemValue` accurate.
  The property-transfer/coercion engine and Binding regions stay `#if !HAS_UNO`
  (Uno `CoerceValue` is a no-op; local `TransferProperty` carries real render
  behavior). Build (129 warnings), 136 tests, and probe are green (see
  `session105.md`).
- Session 106: **DataGridHelper link — property-transfer engine.** Migrated the
  transfer-engine computation (`GetCoercedTransferPropertyValue` x2,
  `IsPropertyTransferEnabled`, `_propertyTransferEnabledMap`) to the linked
  upstream — now real WPF source over the session-105 `GetValueSource` bridge,
  behavior-neutral (only reachable via dormant `OnCoerce*` callbacks; map empty
  → returns baseValue as the stubs did). Investigation established that a real
  global `CoerceValue` is unsafe: 21 dormant `CoerceValue` callers (width/frozen/
  placeholder) would fire at once and fight the shim's asserted width logic, and
  the shim applies styles manually (not via DP setters) so coercion wouldn't
  produce visuals. So `TransferProperty` (visual) + `GetPropertyTransferEnabled-
  MapForObject` stay local/guarded and `CoerceValue` stays a no-op; activation is
  deferred to per-property work. Build (129 warnings), 136 tests, probe green
  (see `session106.md`).
- Session 107: **live-host milestone — catalog (no code).** Mapped the current
  shim render path (code-built `PART_ShimRowsHost` + `BuildShimVisualTree` manual
  header/rows + `ItemContainerGenerator.RegisterContainer`) against the WPF
  item-hosting target (`PART_RowsPresenter` → `DataGridRowsPresenter` →
  generator-driven `DataGridRow`s → `DataGridCellsPresenter`/`DataGridCellsPanel`
  → `DataGridColumnHeadersPresenter`). Proposed a 6-rung, probe-gated, flag-
  guarded sequence (R1 rows-presenter host → R2 drive generation → R3 cells via
  panel → R4 headers → R5 reconcile selection/editing/placeholder/styles → R6
  scroll/measure). Biggest risk: whether Uno drives the generator/VirtualizingPanel
  callbacks the linked presenters expect. Baseline unchanged (see `session107.md`).
- Session 108: **live-host milestone — R1 investigation (no code).** Found two
  decisive blockers: (1) the shim `ItemsControl` derives from WinUI `Control`,
  not WinUI `ItemsControl`, so there is **no items-generation pipeline** and
  nothing sets `IsItemsHost` — `DataGridRowsPresenter` can only attach manually;
  (2) `DataGridRowsPresenter.MeasureOverride` delegates to the stubbed
  `VirtualizingPanel` base, so there is **no row-generation engine** (the cell
  side is fine — `DataGridCellsPanel` is fully linked). So the live-host swap
  means hand-building a row item-hosting/generation driver or porting
  `VirtualizingStackPanel` — multiple large sub-milestones. Options A (hand-built
  row driver) / B (port virtualizing panel) / C (keep shim render as the
  pragmatic live host); recommended **C** pending user decision. Baseline
  unchanged (see `session108.md`).
- Session 109: **live-host milestone — VirtualizingStackPanel feasibility (no
  code).** `VirtualizingStackPanel.cs` is 13,052 lines (full `IScrollInfo`,
  ~5k-line measure region, delayed cleanup, hierarchical virtualization,
  `ScrollTracer`). Decisive blocker: even the 533-line `VirtualizingPanel` base
  overrides WPF `Panel` items-hosting internals (`GenerateChildren`,
  `OnItemsChangedInternal`, `OnClearChildrenInternal`) that WinUI `Panel` (the
  shim's `Panel`) does not have. So option B = reimplement the WPF `Panel`/
  `ItemsControl` items-hosting core on WinUI, then the 13k engine on top — an
  open-ended foundational sub-project to reach behavior identical to today's
  working grid. Recommended **C** (keep shim render as live host; linked
  presenters/cells-panel are substrate). Baseline unchanged (see `session109.md`).
- Session 110: **virtualization track — WinUI-native scope (no code).** Decided
  that on Uno virtualization comes from a WinUI virtualizing host, not a WPF
  `VirtualizingStackPanel` port. Recommended `ItemsRepeater` (lighter than
  `ListView`; shim already owns selection/cells/editing) inside the existing
  `ScrollViewer`, replacing the `PART_ShimRowsHost` `StackPanel`. 6-rung,
  flag-guarded, probe-gated plan: R1 template split (fixed header +
  `ItemsRepeater` rows) → R2 element factory + realize/recycle → R3 generator
  registry vs partial realization → R4 selection/editing on realize → R5 Auto/Star
  width under partial realization (trickiest) → R6 parity + flip default. Baseline
  unchanged (see `session110.md`).
- Session 50: **reuse milestone — sorting via the real WPF path.** Header
  click now calls the linked `DataGrid.PerformSort` (raises `Sorting`, runs
  `DefaultSort`, updates `Items.SortDescriptions`); `ItemCollection.Refresh`
  applies the sort descriptions (stable multi-key) and the reactivity rebuild
  renders sorted order. Deleted the shim reflection sort (`_activeSortColumn`/
  `GetSortValue`). Probe proves `Items.SortDescriptions`/`SortDirection` set by
  WPF. 121 tests green. Comparison still reflection-based; single-key (see
  `session50.md`).
- Control-root member catalog: 386 sites at session 18, 355 after session 19
  (command/metadata), 320 after session 20 (sorting/view), 248 after session
  21 (focus + automation), 0 after session 22 (helper/visual +
  row/cell/presenter/column-collection internals; third link attempt
  succeeded and the link is now permanent). The compile milestone is
  complete; the next phase is behavior — replacing the no-op bridge stubs
  (row generation, column widths, details, templates) driven by a runtime
  sample.
- Mechanism note (discovered in session 15): `ext/wpf` is a patched fork that
  uses `#if !HAS_UNO` guards inside upstream files (for example `Window.cs`,
  `AdornerLayer`, `TextBoxBase`). Fork-patching is an established third
  option alongside direct linking and local bridges.
- Control-shell direction (decided in session 12): pursue the linked
  `DataGrid.cs` path with guarded internals, growing bridges rung by rung,
  rather than a local shell over Uno `ListView`/`Grid`.

## Status Key

- `linked-upstream`: upstream WPF source compiles directly or with narrow
  bridges.
- `local-bridge`: compatibility layer used by linked upstream files.
- `blocked`: known required item that cannot be enabled yet.
- `deferred`: known WPF source outside the current milestone.

## Porting Strategy

DataGrid is a wider surface than RichTextBox. The control itself is not a good
first source file because `DataGrid.cs` immediately pulls on WPF
`ItemsControl`, `MultiSelector`, item containers, virtualization, collection
views, automation peers, command routing, and template parts. The first phase is
therefore an API-foundation pass: bring over independent value types and enums
that downstream code can compile against while we map the larger container
spine.

## Minimum DataGrid Spine

| Area | WPF source / family | Current WindowsShims source | Status | Notes / blocker |
|---|---|---|---|---|
| Sizing model | `DataGridLength`, `DataGridLengthUnitType`, `DataGridLengthConverter` | Linked WPF source plus `MS.Internal/DataGridSizingShims.cs` | `linked-upstream` / `local-bridge` | `DoubleUtil` and `PixelUnit` are bridged locally with the converter subset needed by these files. |
| Public mode enums | `DataGridClipboardCopyMode`, `DataGridEditAction`, `DataGridEditingUnit`, `DataGridGridLinesVisibility`, `DataGridHeadersVisibility`, `DataGridRowDetailsVisibilityMode`, `DataGridSelectionMode`, `DataGridSelectionUnit` | Linked WPF source | `linked-upstream` | Compiles independently. |
| Resource strings | `DataGridLength_Infinity`, `DataGridLength_InvalidType` | `System.Windows/Documents/SR.cs` | `local-bridge` | Added as invariant strings to match the existing SR shim approach. |
| Sizing tests | `DataGridLength` construction/conversion behavior | `LeXtudio.Windows.Tests/DataGridLengthTests.cs` | `linked-upstream` / verified | Covers pixels, star sizing, descriptive values, physical units, and invalid infinity values. |
| Binding bridge | `System.Windows.Data.BindingBase`, `Binding`, `PropertyPath`, mode/update enums | `System.Windows/Data/Binding.cs` | `local-bridge` | Stores WPF-shaped binding state and adapts to Uno `Microsoft.UI.Xaml.Data.Binding` at runtime. Plain NUnit cannot instantiate WinUI binding without a dispatcher, so tests cover state/mapping/converter adapter only. |
| Binding operations | `System.Windows.Data.BindingOperations` | `System.Windows/Data/Binding.cs` | `local-bridge` | WPF facade delegates `SetBinding` to Uno `Microsoft.UI.Xaml.Data.BindingOperations` and uses `ClearValue` for `ClearBinding`. |
| Column base | `DataGridColumn` | Linked WPF source plus `System.Windows/Controls/DataGridColumn.cs` partial | `linked-upstream` / `local-bridge` | Session 63 links the upstream body; the local partial keeps only Uno render/edit bridges and DP-key/coerce compatibility. Width/display-index transfer callbacks compile, but Uno property coercion remains a bridge no-op. |
| Bound column base | `DataGridBoundColumn`, `DataGridHelper` subset | Linked WPF source plus `System.Windows/Controls/DataGridBoundColumn.cs` partial | `linked-upstream` / `local-bridge` | Session 58 linked the upstream body. The local partial keeps `BindingPath` plus `EffectiveSortMemberPath` for Uno's missing sort-member coercion; style/binding application comes from WPF. |
| Cell shell | `DataGridCell` | `System.Windows/Controls/DataGridCell.cs` | `local-shell` | Minimal `ContentControl` shell with `IsEditing`, `Column`, and `BuildVisualTree()` for bound-column refresh paths. |
| Clipboard cell content | `DataGridClipboardCellContent` | Linked WPF source | `linked-upstream` | Enabled after the local column shell landed. |
| Text column | `DataGridTextColumn` | Linked WPF source | `linked-upstream` | Session 59 linked the upstream body over font-DP and input shims; display/edit generation now flows through WPF `ApplyBinding` and text-column helpers. |
| Checkbox column | `DataGridCheckBoxColumn` | Linked WPF source | `linked-upstream` | Session 60 linked the upstream body. A binding bridge promotes WPF two-way defaults for `ToggleButton.IsChecked`, so probe toggle write-back uses the linked path. |
| Template column | `DataGridTemplateColumn` | Linked WPF source | `linked-upstream` | Session 60 linked the upstream body. WPF sort coercion is still limited by the Uno DP layer, but sort fallback is now centralized through `EffectiveSortMemberPath` where applicable. |
| Column event args | `DataGridColumnEventArgs`, `DataGridSortingEventArgs`, `DataGridSortingEventHandler`, `DataGridColumnReorderingEventArgs`, `DataGridAutoGeneratingColumnEventArgs`, `DataGridCellClipboardEventArgs` | Linked WPF source | `linked-upstream` | Session 11 replaced the session-8 local shells with direct links; `ItemPropertyInfo` comes from linked WindowsBase `IItemProperties.cs`. |
| Row/edit event args | `DataGridRowEventArgs`, `DataGridBeginningEditEventArgs`, `DataGridCellEditEndingEventArgs`, `DataGridPreparingCellForEditEventArgs`, `DataGridRowDetailsEventArgs`, `DataGridRowEditEndingEventArgs`, `DataGridRowClipboardEventArgs` | Linked WPF source | `linked-upstream` | Enabled by the minimal local `DataGridRow` shell. |
| Row shell | `DataGridRow` | `System.Windows/Controls/DataGridRow.cs` | `local-shell` | Minimal `Control` shell (`Item`, `IsEditing`, internal owner) to unblock row/edit event args. Container generation, details, headers, and visual states remain deferred. |
| Notification target | `DataGridNotificationTarget` | Linked WPF source | `linked-upstream` | Session 11 replaced the session-9 local copy (byte-identical enum). |
| Clipboard/format helpers | `DataGridClipboardHelper`, `DataGridItemAttachedStorage`, `DataGridHeadersVisibilityToVisibilityConverter` | Linked WPF source | `linked-upstream` | Clipboard helper needed `DataFormats.CommaSeparatedValue` added to the clipboard shim; converter compiles against the WPF-shaped `IValueConverter` shim. |
| Item property metadata | `IItemProperties`, `ItemPropertyInfo` | Linked WPF source (WindowsBase) | `linked-upstream` | Self-contained; enables the internal auto-generation event args constructor. |
| New-item pipeline args | `AddingNewItemEventArgs`, `InitializingNewItemEventArgs`, `InitializingNewItemEventHandler`, `IProvideDataGridColumn` | Linked WPF source | `linked-upstream` | Leaf files from the session-12 probe; no new bridges needed. |
| Cell identity | `DataGridCellInfo` | Linked WPF source | `linked-upstream` | Compiles over the local `ItemsControl.ItemInfo` bridge, `DataGrid.NewItemInfo`, and `DataGridCell` row-owner internals. |
| Item info bridge | `ItemsControl.ItemInfo`, `ItemsControl.EqualsEx` | `System.Windows/Controls/ItemsControlItemInfo.cs` | `local-bridge` | Item/container/index equality subset; WPF sentinel containers and generator `Refresh` are omitted until virtualization paths are linked. |
| Thumb drag args | `DragStartedEventArgs`, `DragDeltaEventArgs`, `DragCompletedEventArgs` + handlers | Linked WPF source | `linked-upstream` | Linked over a minimal local `Thumb` shell that carries the three drag routed-event identities; Thumb input behavior is not implemented. |
| Resource keys | `ResourceKey`, `ComponentResourceKey` | Linked WPF source | `linked-upstream` | Needed a `MarkupExtension` shim, a `ComponentResourceKeyConverter` shim, and two SR strings. |
| Container tracking | `ContainerTracking<>` | Linked WPF source | `linked-upstream` | Self-contained linked-list node used by row/cell container tracking. |
| Uncommon field bridge | `UncommonField<>` | `MS.Internal/UncommonField.cs` | `local-bridge` | `ConditionalWeakTable`-backed; WPF's effective-value-table storage is not reachable on WinUI. |
| Focus direction | `FocusNavigationDirection` | `System.Windows/Input/FocusNavigationDirection.cs` | `local-bridge` | Enum shim mirroring WPF member order. |
| Known boxes | `BooleanBoxes` | Linked WPF source (WindowsBase) | `linked-upstream` | Replaced the local nested-class shim; now a real `MS.Internal.KnownBoxes` namespace as upstream files expect. |
| Spine virtuals | `ItemsControl` `OnItemsChanged`/`OnItemsSourceChanged`/`Prepare`/`ClearContainerForItemOverride`/`AdjustItemInfoOverride`/`OnInitialized`/`OnIsKeyboardFocusWithinChanged` | `System.Windows/Controls/ItemsControlSpine.cs` | `local-bridge` | No-op virtual hooks Selector/MultiSelector override; real behavior waits on item-container generation. |
| Binding expression bridge | `BindingExpressionBase`, `BindingExpression`, `BindingExpressionUncommonField`, `DynamicValueConverter`, `Binding.XPath` | `System.Windows/Data/BindingExpression.cs`, `MS.Internal/Data/DataBridges.cs` | `local-bridge` | Untargeted expression walks dotted CLR property paths via reflection for the selector `SelectedValue` paths; XML/indexer/property-engine evaluation not supported. Converter uses component-model converters with `UnsetValue` failure contract. |
| Spine attribute/info shims | `AttachedPropertyBrowsableForChildrenAttribute`, `MS.Internal.PresentationFramework.BuildInfo` | local shims | `local-bridge` | Designer metadata flattened to `Attribute`; BuildInfo constants mirror `RefAssemblyAttrs.cs` (not linked because it carries assembly-level attributes). |
| Selector spine | `Selector`, `MultiSelector`, `SelectedItemCollection`, `SelectionChangedEventArgs`/`Handler` | Linked WPF source (fork-guarded) | `linked-upstream` | Session 16: compiles over the grown shim `ItemsControl`/`ItemCollection` with seven `#if HAS_UNO` fork guards (attached-handler statics, two effective-value coercion blocks, automation event, `LayoutUpdated` signature adapters, deferred selected-index write). Behavior is selection-state only — no container generation, input, or automation yet. |
| Item collection currency | `ItemCollection` | `System.Windows/Controls/ItemCollection.cs` | `local-bridge` | Collection-change notifications plus `CurrentItem`/`CurrentPosition`/`MoveCurrentTo(Position)` currency; view features (filter/sort/defer, reliable hash codes) unsupported. |
| Generator stub | `ItemContainerGenerator`, `GeneratorStatus` | `System.Windows/Controls/ItemContainerGenerator.cs` | `local-bridge` | No containers are generated; lookups return null/-1 and status stays `NotStarted`. |
| Spine misc stubs | `SystemXmlHelper`, `FrameworkAppContextSwitches`, `IGeneratorHost`, `KeyboardNavigation.Current`/`GetVisualRoot`, `CollectionViewSource.IsDefaultView` | `MS.Internal/SpineStubs.cs`, input shims, `System.Windows/Data/CollectionViewSource.cs` | `local-bridge` | XML sources unsupported; focus-scope tracking not wired; every view is "the default view". |
| Validation layer | `ValidationRule`, `ValidationResult`, `ValidationStep`, `IEditableCollectionView`, `IEditableCollectionViewAddNewItem` | Linked WPF source | `linked-upstream` | Session 18; rule/result/step compile clean, the editable-view interfaces come from WindowsBase. |
| Row validation bridge | `BindingGroup` | `System.Windows/Data/BindingGroup.cs` | `local-bridge` | Stores rules/items and reports edits committable; transactional proposed-value semantics need the WPF property engine. Dispatcher-bound construction. |
| Grouping bridge | `GroupDescription`, `PropertyGroupDescription` | `System.Windows/Data/GroupDescriptions.cs` | `local-bridge` | Group-name extraction reuses the untargeted binding-expression path walker; upstream files drag `SortDescriptionCollection`/XML helpers. |
| Header shell/presenter | `DataGridColumnHeader`, `DataGridColumnHeadersPresenter`, `DataGridColumnHeaderCollection`, `DataGridColumnFloatingHeader`, `DataGridColumnDropSeparator` | Linked WPF sources plus current manual shim header row | `linked-upstream` / `local-bridge` | Sessions 87, 90, and 93-94 link the header container, presenter, header collection, and drag visuals. The linked presenter is not yet the live header host because `DataGrid.BuildHeaderRow` still manually creates headers for the shim template. |
| Command system | `CommandManager`, `InputBinding` over existing `RoutedCommand`/`CommandBinding`/`KeyGesture` shims | `System.Windows/Input/CommandManager.cs` | `local-bridge` | Class command bindings dispatch through the RoutedCommand registry with owner-type scoping; class input bindings recorded but not yet fired from input events; requery notifications direct rather than dispatcher-batched. |
| Sort descriptions | `SortDescription`, `SortDescriptionCollection` | Linked WPF source (WindowsBase) | `linked-upstream` | Needed one SR string (`CannotChangeAfterSealed`); `ItemCollection.SortDescriptions` stores them but sorting is not applied to the view. |
| Editable view | `ItemCollection : IEditableCollectionView` | `System.Windows/Controls/ItemCollection.cs` | `local-bridge` | Direct-list semantics: edit-item bookkeeping and removal work; `AddNew`/placeholders/cancel-edit unsupported (reported honestly via `CanAddNew`/`CanCancelEdit`). |
| View/grouping stubs | `CollectionView.NewItemPlaceholder`, `CollectionViewGroupInternal`, `GroupItem`, `IsGrouping` | `System.Windows/Data/CollectionViewShims.cs`, `GroupItem.cs`, spine | `local-bridge` | Placeholder is a stable sentinel never produced by the shim view; grouping paths are unreachable while `IsGrouping` is false. |
| Focus traversal | `TraversalRequest`/`FocusNavigationDirection` (linked WindowsBase), `KeyboardNavigationMode`, `KeyboardNavigation` traversal members, `Keyboard.Focus`, zero-argument `Focus()` | linked + input shims | `linked-upstream` / `local-bridge` | Prediction returns null and ancestry checks false (traversal falls back to `MoveFocus`, which reports no movement); cell/row `Focus()` routes to WinUI programmatic focus for real. |
| Automation stubs | `AutomationPeer`/`UIElementAutomationPeer`/`DataGrid*AutomationPeer`, `AutomationEvents`, `ValuePatternIdentifiers` | `System.Windows/Automation/Peers/` | `local-bridge` | `FromElement` null + `ListenerExists` false make every automation path unreachable; chosen over ~36 fork guards. |
| Control root | upstream `DataGrid.cs` | Linked WPF source (fork-guarded `partial` on `HAS_UNO`) + minimal local partial (`UpdateVisualState`) | `linked-upstream` | Session 22: third link attempt succeeded; 248 → 0 sites. WPF logic compiles and runs over bridge stubs; behavior (row generation, column widths, details, templates) comes from replacing stubs against a runtime sample. |
| Cell selection collections | `SelectedCellsCollection`, `VirtualizedCellInfoCollection`, `SelectedCellsChangedEventArgs`/`Handler` | Linked WPF source | `linked-upstream` | Compiles over guarded `DataGrid` internals (`Items` item list, `ItemInfoFromIndex`, subset `OnSelectedCellsChanged`), four new SR strings, and `CoreDispatcher.VerifyAccess`/`CheckAccess` extensions. `DataGrid.SelectedCells` and `SelectedCellsChanged` are exposed on the shell. |
| Column owner/collection | `DataGrid`, `DataGridColumnCollection` | upstream `DataGrid.cs` (linked) + `DataGridColumnCollection.cs` | `linked-upstream` / `local-bridge` | Session 22 superseded the local `Columns`/`SelectedCells` owner surface with the linked root. Session 64 ports the WPF display-index map/update subset and renders headers/cells in display order. Width computation/realization block lists remain local stubs (`InvalidateColumnWidthsComputation` et al.). |
| Combo box column | `DataGridComboBoxColumn` | Linked WPF source | `linked-upstream` | Session 60 linked the upstream body with narrow `HAS_UNO` guards for WinUI DP ownership/style-key gaps. Probe verifies selection write-back through WPF binding application. |
| Hyperlink column | `DataGridHyperlinkColumn` | `System.Windows/Controls/DataGridHyperlinkColumn.cs` | `local-shell` | Minimal placeholder added in session 63 so linked `DataGridColumn.CreateDefaultColumn` compiles for `Uri`. Real hyperlink rendering/navigation remains blocked on routed navigation/command plumbing. |
| Row/cell container behavior | upstream `DataGridRow.cs`, `DataGridCell.cs`, `DataGridCellsPresenter`, `DataGridRowsPresenter`, `DataGridCellsPanel` | Linked WPF sources plus local direct-host visual partials and guarded Uno cells-panel path | `linked-upstream` / `local-bridge` / `blocked` | Sessions 88-95 moved `DataGridRow`, presenters, and most `DataGridCell` behavior onto WPF source. Session 101 removed the `DataGridCellsPanel` fork guard so its full WPF measure/arrange/generation/virtualization body compiles on Uno (four narrow `#if HAS_UNO` patches for scroll/`Rect` gaps; `BringIndexIntoView` scroll plumbing still deferred). Replacing the manual rows/header host with the WPF item-hosted layout remains the major blocker. |

## Session Ladder

1. Establish independent API surface and compile green. Completed in session 1.
2. Add sizing tests and catalog first `DataGridColumn` dependency errors.
   Completed in session 2.
3. Add the WPF binding bridge and local `DataGridColumn` shell. Completed in
   session 3.
4. Add bound-column infrastructure: `BindingOperations`, `DataGridCell`, and a
   local `DataGridBoundColumn` shell. Completed in session 4.
5. Add a local `DataGridTextColumn` shell over Uno `TextBlock`/`TextBox`.
   Completed in session 5.
6. Add a local `DataGridCheckBoxColumn` shell over Uno `CheckBox`. Completed in
   session 6.
7. Add a local `DataGridTemplateColumn` shell over WinUI templates and
   `ContentPresenter`. Completed in session 7.
8. Add low-dependency column event args. Completed in session 8.
9. Add collection and notification types: `DataGridColumnCollection` and the
   minimal owner notification hooks it requires. Completed in session 9.
10. Enable remaining low-behavior concrete column types in this order:
   combo box/hyperlink columns as their dependencies become clear. Combo box
   column completed in session 10; hyperlink column remains queued behind
   command/navigation pieces.
11. Re-link pass: replace local event-args/notification shells with direct
   upstream links and pull in the leaf helper files
   (`DataGridClipboardHelper`, `DataGridItemAttachedStorage`,
   `ItemPropertyInfo`), unblocking row/edit event args with a minimal
   `DataGridRow` shell. Completed in session 11.
12. Control-shell milestone, linked path chosen: probe-link upstream
   `DataGrid.cs`, catalog its first-order contracts, and land the first rungs
   (new-item args, `ItemInfo` bridge, linked `DataGridCellInfo`). Completed in
   session 12.
13. Climb the remaining linked-`DataGrid.cs` ladder recorded in the session-12
   probe results: cell-selection collections (completed in session 13),
   rung-5/6 leaves and the spine probe (completed in session 14), spine
   layer-1 bridges and layer-2 catalog (completed in session 15), selector
   spine enablement via fork guards (completed in session 16), shell rebase
   onto `MultiSelector` (completed in session 17), control-root
   prerequisites and the 386-site member catalog (completed in session 18).
14. Control-root staged enablement, roughly one session per cluster from the
   session-18 catalog: command system and metadata friction (completed in
   session 19; 386 → 355 sites), sorting/view (completed in session 20;
   355 → 320), keyboard-focus traversal and automation stubs (completed in
   session 21; 320 → 248), helper/visual internals plus row/cell/presenter
   internals and the successful link attempt (completed in session 22;
   248 → 0 — the control root is now `linked-upstream`).
15. Bring row/cell container behavior and presenters online only when the
   control shell has tests proving the owner/column/item contracts; a
   runtime sample with static items and explicit columns gates behavior
   work. The sample head landed in session 23
   (`src/LeXtudio.Windows.Sample`, `--probe` headless gate, all steps
   green); the identified rungs are row container generation and a visual
   pipeline (template rebase or code-built visuals).

## Test Plan

- Always run `dotnet build WindowsShims/src/LeXtudio.Windows/LeXtudio.Windows.csproj`
  after each source-link batch.
- Run `dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop`
  after changes to the DataGrid sizing bridge.
- Once a control shell exists, add a small runtime sample/test with static items,
  explicit columns, and no virtualization before trying auto-generation,
  editing, or selection.

## Probe Results

### Session 2: `DataGridColumn.cs`

Temporary source-linking `DataGridColumn.cs` produced 15 compile errors before
the link was removed to keep the baseline green. The first blocker is a
`DataGridColumn` partial collision from generated/reference source rather than
a hand-written WindowsShims file. After that, the first-order missing contracts
are:

- DataGrid owner/control contract: `DataGrid`
- Row/cell containers: `DataGridRow`, `DataGridCell`
- Clipboard args: `DataGridCellClipboardEventArgs`
- Notification flags: `DataGridNotificationTarget`
- Data binding surface: `BindingBase`
- Input event base: `InputEventArgs`
- Item-property metadata: `ItemPropertyInfo`

This argues for a column-shell session before source-linking the full upstream
column base.

### Session 3: binding bridge and local column shell

Session 3 followed the column-shell path. `System.Windows.Data.BindingBase` is a
local WPF-shaped bridge rather than a WPF source port; it can convert to Uno's
`Microsoft.UI.Xaml.Data.Binding` in runtime contexts. `DataGridColumn` is a
local partial `DependencyObject` shell with core properties (`Header`, `Width`,
`MinWidth`, `MaxWidth`, `Visibility`, `IsReadOnly`, `DisplayIndex`,
`ClipboardContentBinding`) so source-linked clipboard structures and later
bound-column work have a stable type.

The full upstream `DataGridColumn.cs` remains blocked because Uno source
generation requires a partial class and the WPF file is not partial.

### Session 4: bound-column infrastructure

Session 4 added `System.Windows.Data.BindingOperations` as a WPF facade over
Uno binding operations, then added local partial shells for `DataGridCell` and
`DataGridBoundColumn`. `DataGridColumn` gained `SortMemberPath`, virtual
clipboard binding, and virtual refresh/generation hooks so bound columns can
compile against the expected WPF-shaped contract.

Direct upstream source-linking is still not the right path for
`DataGridBoundColumn` at this point: it owns dependency properties and depends
on WPF property metadata/coercion semantics. The local shell keeps the API
surface moving while the container/control spine remains absent.

Session 4 verification: `dotnet build WindowsShims/src/WindowsShims.slnx
--framework net10.0-desktop --no-restore` succeeded, and `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 17 tests.

### Session 5: text-column shell

Session 5 added a local partial `DataGridTextColumn` over
`DataGridBoundColumn`. The display path creates a Uno `TextBlock` and binds
`TextBlock.TextProperty` through the bound-column binding bridge. The editing
path creates a Uno `TextBox`, binds its `TextProperty`, and performs minimal
focus/select-all preparation.

This is intentionally below full WPF behavior: the upstream implementation's
font property synchronization, flow-direction cache/restore, typed-text
replacement, mouse caret placement, validation commit behavior, and default
style setters are deferred until there is a real DataGrid owner/control shell
and dispatcher-capable runtime coverage.

Session 5 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 18 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 6: checkbox-column shell

Session 6 added a local partial `DataGridCheckBoxColumn` over
`DataGridBoundColumn`. The display and editing paths create/reuse a Uno
`CheckBox`, synchronize `IsThreeState`, apply the selected style, and bind
`CheckBox.IsCheckedProperty` through the bound-column bridge.

The WPF source remains deferred because its user-input behavior depends on the
full DataGrid owner/edit pipeline: `OnInput`, `BeginEdit`, mouse hit testing,
space-key routing, and immediate toggle rules. The local shell keeps the public
column API and generated element contract available without pretending the
owner-control edit state exists yet.

Session 6 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 19 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 7: template-column shell

Session 7 added a local partial `DataGridTemplateColumn`. It exposes
`CellTemplate`, `CellTemplateSelector`, `CellEditingTemplate`, and
`CellEditingTemplateSelector` using WinUI template types, and generates a WinUI
`ContentPresenter` bound to the row data item through the local WPF
`BindingOperations` facade.

Direct upstream source-linking is still deferred. The upstream file includes
sort coercion through `CanUserSortProperty` and `OnCoerceCanUserSort`, which
belong to the wider `DataGridColumn`/owner sorting model that is not present in
the current shell.

Session 7 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 20 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 8: column event args

Session 8 added local shells for the low-dependency DataGrid column event args:
`DataGridColumnEventArgs`, `DataGridSortingEventArgs`,
`DataGridColumnReorderingEventArgs`, `DataGridAutoGeneratingColumnEventArgs`,
and `DataGridCellClipboardEventArgs`.

Row/edit event args are intentionally deferred because their WPF constructors
and properties require `DataGridRow` and the edit pipeline. The local event args
also avoid the upstream internal `ItemPropertyInfo` constructor until
auto-generation is wired to a real item-property discovery path.

Session 8 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 25 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 9: owner and column collection shell

Session 9 probed upstream `DataGridColumnCollection` and confirmed it is not a
good direct source-link candidate yet. The WPF file is coupled to the full owner
control: display-index maps, frozen-column invalidation, star-width
redistribution, realized-column virtualization blocks, cell-info selection
collections, and column/header presenter notifications.

Instead, session 9 added a local partial `DataGrid` shell with a WPF-shaped
`Columns` property backed by an internal `DataGridColumnCollection`. The
collection tracks `DataGridColumn.DataGridOwner`, rejects column reuse across
owners, normalizes default display indexes, and provides basic
`ColumnFromDisplayIndex` / `ColumnIndexFromDisplayIndex` lookups. The
notification target enum is also local now, but propagation remains a no-op
outside column forwarding until row/header/presenter shells exist.

Session 9 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 29 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 10: combo-box-column shell

Session 10 probed upstream `DataGridComboBoxColumn` and confirmed the binding
and item-source surface can be local-shelled without porting selector
item-container behavior: Uno `ComboBox` already provides `ItemsSource`,
`DisplayMemberPath`, `SelectedValuePath`, `SelectedItem`, `SelectedValue`, and
`Text` natively.

The local partial shell derives from `DataGridColumn` (matching WPF, not
`DataGridBoundColumn`) and exposes `SelectedItemBinding`,
`SelectedValueBinding`, and `TextBinding` as plain CLR properties with the WPF
effective-binding precedence (selected item, then selected value, then text)
used for clipboard fallback and one-way read-only coercion. Display and
editing generation both produce a Uno `ComboBox` with bindings and column
properties applied; `RefreshCellContent` rebinds or re-syncs the matching
combo property on column property changes.

Deferred upstream behavior: `TextBlockComboBox` styling via
`ComponentResourceKey`, `OnInput` drop-down opening (F4/Alt+Up/Alt+Down)
because there is no owner edit pipeline, flow-direction cache/restore, and
`SortMemberPath` coercion from the effective binding because the WPF-style
coercion shims are no-ops.

Session 10 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 34 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 11: re-link pass

Session 11 audited all 46 upstream `DataGrid*` files against the contracts the
shims now provide and converted everything that compiles cleanly to
`linked-upstream`:

- Replaced the session-8 local event-args shells and the session-9 local
  `DataGridNotificationTarget` copy with direct links
  (`DataGridColumnEventArgs`, `DataGridSortingEventArgs`,
  `DataGridSortingEventHandler`, `DataGridColumnReorderingEventArgs`,
  `DataGridAutoGeneratingColumnEventArgs`, `DataGridCellClipboardEventArgs`).
- Linked the leaf helpers `DataGridClipboardHelper` (needed one new
  `DataFormats.CommaSeparatedValue` constant), `DataGridItemAttachedStorage`,
  and `DataGridHeadersVisibilityToVisibilityConverter` (compiles against the
  WPF-shaped `IValueConverter` shim).
- Linked WindowsBase `IItemProperties.cs`, providing
  `System.ComponentModel.ItemPropertyInfo` for the internal auto-generation
  event-args constructor.
- Added a minimal local `DataGridRow` shell (`Item`, `IsEditing`, internal
  owner), which unblocked direct links for all seven row/edit event args:
  `DataGridRowEventArgs`, `DataGridBeginningEditEventArgs`,
  `DataGridCellEditEndingEventArgs`, `DataGridPreparingCellForEditEventArgs`,
  `DataGridRowDetailsEventArgs`, `DataGridRowEditEndingEventArgs`,
  `DataGridRowClipboardEventArgs`.

Net effect: 18 new linked upstream files, two local shell files deleted, and
the event-args backlog cleared. `DataGridRowClipboardEventArgs` round-trips
through the real WPF `DataGridClipboardHelper` CSV/text formatting in tests.

Still blocked after the audit: the behavioral core (`DataGrid.cs`,
`DataGridColumnCollection.cs`, `DataGridCellsPanel.cs`, upstream
`DataGridColumn.cs`/`DataGridRow.cs`/`DataGridCell.cs`, `DataGridHelper.cs`,
concrete column sources, `DataGridCellInfo`, `DataGridColumnHeaderCollection`,
drag/drop header visuals) on the WPF property engine
(`OverrideMetadata`/`AddOwner`/coercion), the Uno generator partial collision,
and the `ItemsControl`/`MultiSelector`/virtualization stack.

Session 11 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 44 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 12: linked `DataGrid.cs` probe and first rungs

The control-shell direction is decided: linked upstream `DataGrid.cs` with
guarded internals. Session 12 probe-linked the 8,628-line upstream file (with
the local `DataGrid` shell excluded) and captured the first-order catalog: 27
unique unresolved contracts. Notably, `CommandManager`, `KeyboardNavigation`,
`VirtualizingPanel`, `ItemsPanelTemplate`, and `FrameworkElementFactory`
already resolve from the RichTextBox-era shims. Error-type cascades suppress
member-level errors, so deeper layers will surface as these clear.

The catalog clusters into a bridge ladder:

1. New-item pipeline leaves (`AddingNewItemEventArgs`,
   `InitializingNewItemEventArgs`/`Handler`, `IProvideDataGridColumn`) —
   linked in session 12.
2. Cell identity (`DataGridCellInfo` over `ItemsControl.ItemInfo`,
   `DataGrid.NewItemInfo`, cell row-owner internals) — linked in session 12.
3. Cell-selection collections (`VirtualizedCellInfoCollection`,
   `SelectedCellsCollection`, `SelectedCellsChangedEventArgs`/`Handler`) —
   need DataGrid items/generator internals.
4. Selector spine (`MultiSelector` base, `SelectedItemCollection`,
   `ItemNavigateArgs`, `FocusNavigationDirection`) — the load-bearing rung;
   needs a WPF-shaped `ItemsControl`/`Selector`/`MultiSelector` bridge.
5. Headers/presenters (`DataGridColumnHeader`,
   `DataGridColumnHeadersPresenter`) and Thumb drag args
   (`DragStarted`/`DragDelta`/`DragCompletedEventArgs`, likely aliasable to
   WinUI primitives).
6. Validation/binding (`ValidationRule`, `BindingGroup`,
   `IEditableCollectionView`, `PropertyGroupDescription`, `MS.Internal.Data`)
   and infra (`UncommonField<>`, `ContainerTracking<>`,
   `ComponentResourceKey`).

The `ItemInfo` bridge intentionally omits WPF's sentinel containers (they
require constructing bare `DependencyObject` instances, dispatcher-bound on
Uno) and generator `Refresh`; equality keeps the item/container/index subset.
`DataGrid.NewItemInfo` returns caller-provided state until a real item
container generator exists.

Session 12 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 51 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 13: cell-selection collection stack

Session 13 compared the two candidate rungs. The selector spine
(`MultiSelector` is only 102 lines, but it sits on `Selector` at ~3k and
`ItemsControl` at ~4k lines) is the expensive rung; the cell-selection
collections turned out to be cheap because `VirtualizedCellInfoCollection`'s
owner contract is narrow: `Columns`, `ColumnFromDisplayIndex` (already
present), plus `Items`, `ItemInfoFromIndex`, and `Dispatcher.VerifyAccess`.

Guarded internals added to the local `DataGrid` shell: a plain `ItemCollection`
`Items` list (until the selector spine provides a real one),
`ItemInfoFromIndex` without generator container resolution, and a subset
`OnSelectedCellsChanged` that skips WPF's selection-unit validation and
pending-change coalescing and just raises the public `SelectedCellsChanged`
event. `DataGrid.SelectedCells` is now exposed as `IList<DataGridCellInfo>`
over the linked `SelectedCellsCollection`.

Supporting shim work: four SR strings
(`SelectedCellsCollection_InvalidItem`/`_DuplicateItem`,
`VirtualizedCellInfoCollection_DoesNotSupportIndexChanges`/`_IsReadOnly`) and
`VerifyAccess`/`CheckAccess` on both the `Dispatcher` shim and the
`CoreDispatcher` extensions (WinUI's native `Dispatcher` property shadows the
WPF-shaped extension, so the upstream call lands on `CoreDispatcher`).

Session 13 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 56 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 14: selector spine probe and rung-5/6 leaves

Session 14 probe-linked `Selector.cs` + `MultiSelector.cs` and got a
surprisingly small first-order catalog: 16 unique unresolved contracts. The
wall is not `ItemsControl` member surface as expected, but the WPF data-engine
internals: `BindingExpression`, `BindingExpressionUncommonField`,
`DynamicValueConverter`, `MS.Internal.Data`, `MS.Internal.KnownBoxes`, plus
seven missing virtuals on the shim `ItemsControl`
(`OnItemsChanged`/`OnItemsSourceChanged`/`Prepare`/`ClearContainerForItemOverride`/
`AdjustItemInfoOverride`/`OnInitialized`/`OnIsKeyboardFocusWithinChanged`) and
`AttachedPropertyBrowsableForChildren`. The probe was reverted; the usual
caveat applies that error-type cascades hide deeper member-level errors.

The session then landed the rung-5/6 leaves:

- Linked Thumb drag args (`DragStartedEventArgs`, `DragDeltaEventArgs`,
  `DragCompletedEventArgs` with their handler delegates) over a minimal local
  `Thumb` shell carrying the three drag routed-event identities.
- Linked `ResourceKey` + `ComponentResourceKey` over new `MarkupExtension`
  and `ComponentResourceKeyConverter` shims and two SR strings.
- Linked `ContainerTracking<>` (self-contained).
- Added a `ConditionalWeakTable`-backed `UncommonField<>` bridge (WPF's
  effective-value-table storage is unreachable on WinUI).
- Added a `FocusNavigationDirection` enum shim mirroring WPF member order.

Session 14 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 61 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 15: spine layer-1 bridges and layer-2 catalog

Session 15 cleared the session-14 spine catalog and re-probed. What landed:

- Seven no-op WPF-shaped virtuals on the shim `ItemsControl`
  (`ItemsControlSpine.cs`).
- Linked WindowsBase `KnownBoxes.cs`, deleting the local shim whose
  static-class shape broke `using MS.Internal.KnownBoxes;`.
- An untargeted `BindingExpression` bridge (`BindingExpressionBase` with the
  `DisconnectedItem` sentinel, reflection-based dotted-path evaluation with
  `Activate`/`Deactivate`/`Value`), `BindingExpressionUncommonField`, a
  `DynamicValueConverter` bridge over component-model converters, and
  `Binding.XPath` storage.
- `AttachedPropertyBrowsableForChildrenAttribute` (flat designer shim) and
  `MS.Internal.PresentationFramework.BuildInfo` constants.

Discovery: `ext/wpf` is a patched fork using `#if !HAS_UNO` guards inside
upstream files (`Window.cs`, `AdornerLayer`, `TextBoxBase`). Fork-patching is
therefore an established third mechanism alongside linking and local bridges.

The layer-2 re-probe of `Selector.cs`/`MultiSelector.cs` surfaced 79 unique /
302 total errors. Clusters: rich `ItemsControl` member surface (`NewItemInfo`,
`NewUnresolvedItemInfo`, `HasItems`, `ItemInfoFromIndex`,
`GetItemOrContainerFromContainer`, `IsItemItsOwnContainerOverride`),
`ItemCollection` behaving as a `CollectionView` (`CurrentItem`,
`MoveCurrentToPosition`, `IsEmpty`), WPF property-engine internals
(`SetCurrentValueInternal`, `GetValueEntry`/`LookupEntry`,
`EffectiveValueEntry`, `DependencyPropertyKey` set paths), `ItemInfo`
sentinels (intentionally omitted from the bridge), automation peers,
`KeyboardNavigation.Current`, `SystemXmlHelper`, `SelectedItemCollection`,
`DeferredSelectedIndexReference`, handler add/remove signature differences,
and several SR strings. Probe reverted; baseline stays green.

Conclusion: clean source-linking of the spine is not reachable by bridging
alone — the property-engine cluster has no Uno equivalent. The next decision
is fork-patching `Selector.cs`/`MultiSelector.cs` with `#if !HAS_UNO` guards
around those clusters versus writing a WPF-shaped local `Selector` spine that
exposes only what `DataGrid.cs` consumes.

Session 15 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 70 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 16: selector spine linked with fork guards

Session 16 decided the spine mechanism (fork-patch, per the `HAS_UNO`
precedent) and landed it the same session. The path that worked: grow the
cheap shim surface first, then guard only what is genuinely engine-bound.

Shim growth that eliminated most of the layer-2 catalog:

- `ItemCollection` rewritten as a currency-tracking collection
  (collection-change events, `CurrentItem`/`CurrentPosition`/`MoveCurrentTo`).
- `ItemInfo` completed: lazy sentinel containers (a generated
  `SentinelContainerObject` partial, because bare `DependencyObject` is an
  interface on Uno), full WPF equality, `Reset`, `Key`.
- Shim `ItemsControl` grew `HasItems`, `NewItemInfo`/`NewUnresolvedItemInfo`,
  `ItemInfoFromIndex`, `AdjustItemInfos(AfterGeneratorChange)`, generator
  property, `IGeneratorHost`, `ItemsSource`/`IsInitialized`/
  `IsKeyboardFocusWithin`, and bare-call members (`CheckAccess`,
  `CoerceValue`, `SetValue`/`ClearValue` with `DependencyPropertyKey`,
  `SetCurrentValueInternal`, `AddHandler`/`RemoveHandler`/`RaiseEvent`) —
  bare calls cannot bind to extension members, so these must be real members.
- `KeyboardNavigation` became instance-capable (`Current`,
  `FocusEnterMainFocusScope`, `UpdateActiveElement`, `GetVisualRoot`).
- Stubs: `SystemXmlHelper`, `CollectionViewSource.IsDefaultView`,
  `ItemContainerGenerator`/`GeneratorStatus`, `FrameworkAppContextSwitches`
  (consolidated into `MS.Internal` from the misplaced Documents copy).
- Eight more SR strings; `FrameworkPropertyMetadataOptions.Journal`.

Fork guards added to `Selector.cs` (seven sites): the four attached
Selected/Unselected handler helpers (route through the handler-bag
extensions), two effective-value/deferred-reference coercion blocks (coerce
unconditionally), the automation `IsSelected` event (no-op), `LayoutUpdated`
subscription adapters for WinUI's `EventHandler<object>` signature, and the
deferred selected-index write (computed eagerly via
`SetCurrentValueInternal`). One qualification edit resolves
`BindingExpressionBase.DisconnectedItem` against the System.Windows.Data
bridge (an older `System.Windows.BindingExpressionBase` shim shadows it via
enclosing-namespace lookup — consolidation candidate).

Caveat: the spine compiles and carries selection state, but container
generation, input-driven selection, currency synchronization, and automation
remain non-functional until those bridges exist. Dispatcher-bound
construction still limits plain NUnit coverage to type surface and the plain
event args.

Session 16 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 74 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 17: shell rebase and control-root catalog

Session 17 consolidated the duplicate `BindingExpressionBase` shims (the
`System.Windows` EarlyBatch copy shadowed the `System.Windows.Data` bridge
via enclosing-namespace lookup; the base now carries a virtual `Value` that
the bridge expression overrides, and the session-16 fork qualification was
reverted), then rebased the local `DataGrid` shell from `Control` onto the
linked `MultiSelector`. The shell dropped its duplicated `Items`,
`NewItemInfo`, and `ItemInfoFromIndex` members — those now come from the
spine — while keeping the column/cell surface local.

The control-root re-probe of upstream `DataGrid.cs` (8,628 lines) against
the rebased chain produced only 21 unique error sites, all concrete
member-level errors (no cascades, since the base chain fully resolves):

- Validation/binding cluster: `ValidationRule`, `BindingGroup`,
  `IEditableCollectionView`, `PropertyGroupDescription`.
- Header cluster: `DataGridColumnHeader`, `DataGridColumnHeadersPresenter`.
- `ItemNavigateArgs` (ItemsControl nested type).
- Ten missing virtuals on the shim chain (`OnTextInput`, `OnKeyDown`,
  `OnMouseMove`, `OnIsMouseCapturedChanged`, `OnIsGroupingChanged`,
  `OnContextMenuOpening`, `OnTemplateChanged`, `HandlesScrolling`,
  `GetContainerForItemOverride`, `ChangeVisualState`).
- Two override-signature clashes needing fork guards:
  `OnApplyTemplate` (WPF widens to public; WinUI is protected) and
  `OnCreateAutomationPeer` (different `AutomationPeer` types).

Probe reverted. This catalog is the session-18 enablement list for the
control root.

Session 17 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 75 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 18: control-root prerequisites and the real member catalog

Session 18 landed the session-17 enablement list:

- Linked `ValidationRule.cs`/`ValidationResult.cs`/`ValidationStep.cs` and
  WindowsBase `IEditableCollectionView`/`IEditableCollectionViewAddNewItem`.
- Local bridges: `BindingGroup` (rules/items storage, committable edits),
  `GroupDescription`/`PropertyGroupDescription` (group-name extraction over
  the untargeted binding-expression path walker), header shells
  (`DataGridColumnHeader`, `DataGridColumnHeadersPresenter`).
- Shim growth: eleven virtuals (`OnTextInput`, `OnKeyDown`, `OnMouseMove`,
  `OnIsMouseCapturedChanged`, `OnIsGroupingChanged`, `OnContextMenuOpening`,
  `OnTemplateChanged`, `HandlesScrolling`, `GetContainerForItemOverride`,
  `ChangeVisualState`, `FocusItem`), the `ItemNavigateArgs` bridge,
  `InputDevice` (with `KeyboardDevice` rebased onto it), and
  `Keyboard.PrimaryDevice`.
- Fork guards in `DataGrid.cs`: `OnCreateAutomationPeer` (excluded; peers not
  bridged) and `OnApplyTemplate` accessibility (protected under `HAS_UNO`).

Probe lesson: the session-17 catalog of 21 sites was declaration-level only —
those errors masked method-body binding. With them cleared, the honest
control-root catalog is 386 unique member-level sites, clustered:

1. Command system (largest single gap): `CommandManager` class command/input
   bindings (~44 sites).
2. Sorting/grouping/view: `ItemCollection.SortDescriptions`,
   `SortDescription`, `CollectionView`, `IsGrouping` (~60 sites).
3. Keyboard/focus traversal: `KeyboardNavigationMode`, `TraversalRequest`,
   `MoveFocus`, one-arg `Focus`, `PredictFocusedElement`, `IsAncestorOfEx`
   (~60 sites).
4. Automation peers: `DataGridAutomationPeer` and friends (~36 sites).
5. Helper/visual internals: `DataGridHelper.FindVisualParent`/
   `IsDefaultValue`, `VisualStates`, `Panel.Children`, `ContentElement`
   (~40 sites).
6. Row/cell/presenter internals: `DataGridRow.DetailsPresenter`/
   `CellsPresenter`/`DetailsVisibility`, `DataGridCellsPresenter`,
   `ItemInfoFromContainer`, `OnBringItemIntoView` (~50 sites).
7. Metadata/conversion friction: `FrameworkPropertyMetadata` ctor ambiguity,
   `CoerceValueCallback` conversions (~25 sites).

The control root therefore needs a staged plan — roughly one session per
cluster (command system, sort/view, focus traversal, automation guards,
row/cell internals) before the link attempt repeats. Probe reverted; the two
fork guards remain in the fork (valid for the eventual link).

Session 18 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 80 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 19: command-system bridge (control-root cluster 1)

The RichTextBox-era shims already carried most of the command system: a
`RoutedCommand` with a class-binding registry and target-scoped
`Execute`/`CanExecute` dispatch, `CommandBinding` with executed/can-execute
handlers and an `AppliesTo(target)` type filter, `RoutedUICommand`,
`KeyGesture`/`InputGesture(Collection)`, and `CommandBindingCollection`. The
missing pieces were `CommandManager` itself and `InputBinding`.

Session 19 added `System.Windows/Input/CommandManager.cs`:
`RegisterClassCommandBinding` scopes a binding (which self-registers with its
`RoutedCommand` at construction) to the owner type via a new
`CommandBinding.SetClassOwner`; `RegisterClassInputBinding` records
gesture/command pairs per type for the future key-routing bridge;
`InvalidateRequerySuggested` raises `RequerySuggested` directly (WPF batches
on the dispatcher). `InputBinding` is a flat gesture/command pair.

The session also fixed the cluster-7 `FrameworkPropertyMetadata` ambiguity:
Roslyn reports `(object?, System.Windows.PropertyChangedCallback?)` vs
`(object?, Microsoft.UI.Xaml.PropertyChangedCallback?)` as ambiguous for WPF
method-group arguments even though the method group only converts to the WPF
delegate (verified by direct-assignment repro). The WinUI two-argument
overload was removed — no caller in the solution needed it — and a comment
in the shim records why it must not return.

Re-probe: the control-root catalog dropped from 386 to 355 unique sites;
`CommandManager` (~44 sites) and the metadata ambiguity (~24 sites including
coerce-callback conversions at the same call sites) no longer appear.
Remaining clusters: automation peers, sorting/view, keyboard-focus traversal,
helper/visual internals, row/cell/presenter internals.

Session 19 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 85 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 20: sorting/view cluster (control-root cluster 2)

Session 20 cleared the sorting/view cluster:

- Linked WindowsBase `SortDescription.cs` and `SortDescriptionCollection.cs`
  (one new SR string). The collection implements `INotifyCollectionChanged`,
  which `DataGrid.cs` casts to for sort-change tracking.
- `ItemCollection` gained `SortDescriptions` (stored, not applied to the
  view) and an `IEditableCollectionView` implementation with direct-list
  semantics: `EditItem`/`CommitEdit` bookkeeping and `Remove`/`RemoveAt`
  work; `AddNew`, placeholders, and `CancelEdit` are unsupported and
  reported honestly through `CanAddNew`/`CanCancelEdit`/
  `NewItemPlaceholderPosition`. `DataGrid.EditableItems` is a plain cast of
  `Items`, so the editing pipeline binds against this implementation.
- Minimal `CollectionView` with the stable `NewItemPlaceholder` sentinel
  (never produced by the shim view, so placeholder checks are simply false),
  `CollectionViewGroupInternal` and `GroupItem` stubs for
  grouping-navigation code, and `IsGrouping => false` on the shim
  `ItemsControl` which makes those paths unreachable.

Re-probe: 355 → 320 unique sites; the sorting/view names no longer appear.

Session 20 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 90 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 21: focus traversal and automation stubs (clusters 3-4)

Keyboard-focus cluster:

- Linked WindowsBase `TraversalRequest.cs`, which carries the real
  `FocusNavigationDirection` enum — the session-14 local enum shim was
  deleted in its favor.
- Added `KeyboardNavigationMode` (WPF member order) and grew
  `KeyboardNavigation`: `DirectionalNavigation`/`ControlTabNavigation`
  attached properties, `ShowFocusVisual`, and instance `IsAncestorOfEx`
  (false) / `PredictFocusedElement` (null) so traversal prediction falls
  through to the default `MoveFocus` path.
- `Keyboard.Focus(IInputElement)` reports the element back;
  `MoveFocus`/`Focus()` on the shim `ItemsControl` report no focus movement;
  `DataGridCell`/`DataGridRow` gained zero-argument `Focus()` members that
  route to WinUI programmatic focus (those are real, not stubs).

Automation cluster (stubs, not guards — cheaper than ~36 fork edits and
honest at runtime): `AutomationPeer.FromElement` returns null and
`ListenerExists` returns false, so every automation path in the control root
is unreachable; `UIElementAutomationPeer`, `DataGridAutomationPeer` (raise
members + `FindOrCreateItemAutomationPeer`), `DataGridItemAutomationPeer`,
`DataGridCellItemAutomationPeer`, the full WPF-ordered `AutomationEvents`
enum, and `ValuePatternIdentifiers.ValueProperty` exist for compilation.

Re-probe: 320 → 248 unique sites. Remaining: helper/visual internals and the
row/cell/presenter/column-collection internals (the largest final cluster —
`DataGridRow.DetailsPresenter`/`CellsPresenter`/`DetailsVisibility`/
`TryGetCell`/`BindingGroup`, `DataGridCellsPresenter`,
`DataGridColumnCollection` width/realization internals,
`DataGridColumn.SortDirection`, `OnBringItemIntoView`,
`ItemInfoFromContainer`).

Session 21 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 95 tests; the solution build
also succeeds for `net10.0-desktop`.

## Open Questions

- Whether the first usable Uno DataGrid should wrap an existing Uno/WinUI grid
  control or preserve the WPF source as the behavioral owner from the start.
- How much WPF `ItemsControl`/selector surface should live in WindowsShims
  generally versus private DataGrid bridges.
- Whether full WPF virtualization semantics are required for the first consumer
  or can be deferred behind static row rendering.
