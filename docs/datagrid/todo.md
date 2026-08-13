# DataGrid Port — Remaining Work

Status as of session 126 (2026-08-12). All items below are **still open**
unless marked otherwise. Items closed in sessions 121–123 (grouping,
hyperlink column, frozen columns, row-details variable-height virtualization,
VSM sort/hover, TextSearch, redundant separator cleanup, Fluent theme, test
consolidation, grid-line rendering) and session 126 (accessibility bridge)
are **not** listed here.

---

## 1. Accessibility / UI Automation — DONE (session 126)

**Status:** bridged onto Uno 6.6's native Skia accessibility.
**Source:** session126.md, todo.md prior to session 126.

`AutomationPeer.FromElement` returned `null`, `ListenerExists` returned
`false`, raises were no-ops, so the linked WPF DataGrid's ~36
`ListenerExists`-gated automation call sites never fired. Session 126
rewired the bridge:

- WPF-shaped `AutomationPeer` now extends Uno's
  `Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer` — the
  one peer base `SkiaAccessibilityBase.TryGetPeerOwner` recognizes for native
  event routing. WPF's own peer files stay unlinked (their base lives in
  PresentationCore, 2600-line COM/UIA core); fresh peers were written against
  this project's DataGrid with WCT v7 as behavioral reference.
- `Control.OnCreateAutomationPeer` overrides Uno's virtual; C# 9 covariant
  overrides in the linked WPF files feed Uno's tree.
- Statics forward to Uno: `ListenerExists`/`FromElement`/
  `CreatePeerForElement`; instance `RaiseAutomationEvent`/
  `RaisePropertyChangedEvent` (AutomationEvents member values mirror UIA IDs
  0..17, so the cast is exact).
- Peers: DataGrid (Selection + Grid patterns; row/cell invoke/selection
  raises), Row (SelectionItem), Cell (Value + SelectionItem + GridItem),
  ColumnHeader (Invoke), ColumnHeadersPresenter/DetailsPresenter (Group),
  RowHeader (Header). Item peers route property changes through realized
  element peers.
- Verified by `Uia_DataGridExposesNativePeersAndSelectionEvents`: the probe
  wires Uno's internal `IAutomationPeerListener` via reflection +
  DispatchProxy, drives `SelectedIndex = 1`, asserts peer creation, control
  types, patterns, `GetSelection()`, cell Value/Name, column-header Invoke,
  and the raised `SelectionItemPatternOnElementAddedToSelection` event.

Remaining (future): Table pattern (needs `RowOrColumnMajor` — not in the
referenced Uno.UI build), IGridItem Row/ColumnSpan (fixed at 1), cell
SetValue (needs binding write-back), header drag-reorder automation.

---

## 2. Frozen columns — vertical scroll interaction

**Status:** resolved — the manual-mode row-sizing gap that could suppress the
vertical scroll extent is fixed (item 3, session 127); the tracked-row test
passes in the suite (66/66, session 127).  
**Source:** session121:1120-1124, 1367-1399; corrected 2026-07-27
(verification pass found `FrozenColumns_TrackedRowKeepsFrozenXAcrossVerticalScroll`
in `tests/DataGrid.IntegrationTests/DataGridIntegrationTests.cs:394` is now a
plain `[Fact]`, not `[Fact(Skip=...)]` as previously recorded here).

The root-cause comment above the test (manual/non-virtualized
`PART_ShimRowsScroll` StackPanel reporting `ExtentHeight == ViewportHeight`
because rows collapse to ~1-2px actual height) predates the session 127 row
height fix: rows no longer collapse (content-sized ~32.5px, and exactly
`RowHeight` when set), so the scroll extent reflects the real content height.
Re-read the stale comment and drop it next time this file is touched.

## 3. Manual-mode row-sizing gap — DONE (session 127)

**Status:** fixed and tested (3 new tests, `DataGridRowHeightTests.cs`).  
**Source:** session121:1378-1385, session126 summary

`DataGrid.RowHeight`/`MinRowHeight` now reach realized rows in manual
(non-virtualized) mode. WPF flows these through the CellsPresenter coercion
chain (`DataGrid.NotifyPropertyChanged` → row → `DataGridCellsPresenter`
Height/MinHeight `OnCoerceHeight`/`OnCoerceMinHeight`), which the manual path
lacks — so the shim re-applies heights directly:

- `DataGrid.ShimEnsureRowHeightHook` (lazy): `RegisterPropertyChangedCallback`
  on `RowHeightProperty`/`MinRowHeightProperty` → `ShimApplyRowHeightsToRealizedRows`
  (via `ItemContainerGenerator.Containers`).
- `DataGridRow.ShimApplyRowHeights(owner)` / `DataGridCell.ShimApplyRowHeight`:
  cell `Height = RowHeight` when non-NaN (else auto), `MinHeight = max(32,
  MinRowHeight)` when set.
- `DataGridRow.OnApplyTemplate` re-applies once cells actually exist — the
  manual path decorates the row (`ShimDecorateRow`) *before* its template is
  applied, so the decorate-time application would otherwise see zero cells.
  This was the debugging crux: `UpdateLayout` rebuilds rows with fresh cell
  instances that only got heights via the re-apply-on-template hook.

Verified by `DataGridRowHeightTests`: RowHeight=50 → all 21 rows actual 50;
MinRowHeight=100 over RowHeight=50 → rows 100; reset to NaN → content-sized
again. DataGrid suite 66/66, RichTextBox 238/238, model tests 234/234 green.

---

## 4. VSM template Slices 2-4 — Cell / Row / Root templates

**Status:** not started.  
**Source:** session121:1748-1753, 1677-1681

Slice 1 (DataGridColumnHeader VSM) is done (hover/pressed via
`CommonStates`). Slices 2-4 not started:

- **Slice 2 — DataGridCell:** selected/invalid visual treatment via VSM.
- **Slice 3 — DataGridRow:** hover/selected/alternating-row tint via VSM.
- **Slice 4 — Root DataGrid template:** gridline brush, filler column —
  highest risk (load-bearing virtualization infrastructure).

Key blocker for Row/Cell: C# already procedurally sets `Background` on
selection/alternation (`ApplyShimRowBackground`, `UpdateSelectionVisual`),
which would compete with any newly-added VSM-driven background animation on
the same property. Needs a design decision:
- Migrate fully to VSM.
- Keep VSM additive/non-conflicting.

---

## 5. Per-property coercion activation

**Status:** first+second slices done (session 130): `FrozenColumnCount`,
`AlternationCount`, and `IsSynchronizedWithCurrentItem` now coerce on
`DataGrid`; the ~25 other `CoerceValueCallback` registrations across linked
DataGrid/DataGridColumn/DataGridCell/DataGridRow files are still dormant
because the base `Control.cs`/`ContentControl.cs`/`ButtonBase.cs`/
`FrameworkElement.cs` all declare empty `CoerceValue(DependencyProperty dp) {}`.  
**Source:** session106:80-90, session121:1777-1788; session130.md

Session 130 added a narrow `internal new void CoerceValue(DependencyProperty)`
on `DataGrid` (hiding the base no-op), same pattern as the session 121
`DataGridColumnHeader` one, with a whitelist of exactly three properties:

- `FrozenColumnCountProperty` — clamps to `Columns.Count` via
  `OnCoerceFrozenColumnCount`, driven from the column-collection-changed path
  (upstream DataGrid.cs:263) and first measure (7639). The probe sets the
  count then adds a column to trigger the coercion path.
- `AlternationCountProperty` — promotes to `>= 2` when `AlternatingRowBackground`
  is set, via `OnCoerceAlternationCount` (upstream DataGrid.cs:619,
  `NotifyPropertyChanged` branch).
- `IsSynchronizedWithCurrentItemProperty` — coerced to `false` when
  `SelectionUnit` is `Cell` via `OnCoerceIsSynchronizedWithCurrentItem`
  (upstream DataGrid.cs:1061; the trigger is `OnSelectionUnitChanged` calling
  `CoerceValue(IsSynchronizedWithCurrentItemProperty)` at :4587).

Gotchas found along the way:

- The callbacks are invoked **directly** rather than looked up through
  `property.GetMetadata(...)`: `OverrideMetadata` is a project-wide no-op
  (WinUI has no per-type metadata), so the `AlternationCountProperty.OverrideMetadata(...)`
  in upstream DataGrid.cs:54 never registered anything.
- `ItemsControl.AlternationCount` in the shim was a plain auto-property with
  no backing store — `CoerceValue` wrote the spine-registered DP, but the
  getter returned 0. Rewired to read/write `AlternationCountProperty`.
- Coercion triggers on collection-change/measure/notification paths, **not** on
  plain `SetValue` — the test probe must exercise the upstream call site.

Verified by `Coercion_FrozenColumnCountClampsToColumnCount`,
`Coercion_AlternationCountPromotesToTwoWhenAlternatingBackgroundSet`, and
`Coercion_IsSynchronizedWithCurrentItemForcedOffInCellSelectionUnit`
(DataGrid suite 70/70, session 130).

Recommended approach for the remaining properties: smallest-blast-radius
activation, one property at a time. The width/frozen coerce callbacks should
stay dormant until the shim's parallel width logic is retired.

---

## 6. GroupStyle.Panel

**Status:** deliberate architectural scope cut (not a gap).  
**Source:** session121:1447-1457

Not shimmed. Supporting `GroupStyle.Panel` for real would require each
group's rows to live in a *separate* nested `ItemsControl`+panel subtree
instead of the current flattened single row-host list that frozen columns,
cell editing, column virtualization, and selection all depend on — a genuine
architectural rewrite of row hosting.

---

## 7. GroupStyle.AlternationCount

**Status:** not in scope (not a gap).  
**Source:** session121:1459-1460

Left alone.

---

## 8. Recycling mode for grouped grids

**Status:** deliberately unsupported.  
**Source:** session121:587-588

Grouped grids always use `VirtualizationMode.Standard` (discard-and-recreate).
Recycling reuses container *instances* across realized indices; since a
`GroupItem` and a `DataGridRow` are different types, the recycle pool can't
type-switch. Revisit only if profiling shows the allocation savings matter.

---

## 9. Manual-path collapse rebuilds entire visual tree

**Status:** correct but non-incremental.  
**Source:** session121:591-596

`BuildShimVisualTree()` rebuilds the entire tree on every expand/collapse
toggle, rather than surgically removing/re-adding just the affected group's
rows. Fine for interactive use; cost concern only for extremely large
flat-rendered (non-virtualized) grouped grids, which the
`ShimAutoVirtualizeThreshold` auto-switch already steers away from.

---

## 10. Style setter application

**Status:** deferred.  
**Source:** session114:66-68

`MetadataTableViews.Instance` returns `null` for all keys, so `CellStyle`
etc. are null in practice until `MetadataTableViews.xaml` is ported to WinUI
XAML. The linked upstream `PrepareContainerForItemOverride` / style-
application code compiles and runs, but has no style objects to apply.

---

## 11. B1 arc default-on decision

**Status:** `ShimSetHeaderPresenterHost` still opt-in (default off).  
**Source:** session120:720-724, 752-757

Session 120 proved the `DataGridColumnHeadersPresenter` path works (header
generation, Auto/Star widths, column resize, style/gridline notifications,
drag-reorder plumbing, floating drag header). Still opt-in because:

- Interactive drag *feel* not verified (blocked on Uno Platform synthetic-click
  issue — see `docs/uno-macos-synthetic-click-issue.md`).
- `ShimSetRowVirtualization` and `ShimSetHeaderPresenterHost` are independent
  gates; flipping both on by default needs a deliberate decision.

---

## 12. Cell editing under the header-presenter path — DONE (session 128)

**Status:** covered by `FrozenColumns_RealCellEditCommitsUnderHeaderPresenter`
(DataGridIntegrationTests.cs), which builds the grid with
`ShimSetRowVirtualization(true)` + `ShimSetHeaderPresenterHost(true)` +
`ShimSetCellsPresenterHost(true)` — the full presenter-hosted stack (Roma's
metadata-grid mode) — then runs real BeginEdit/CommitEdit against a tracked
row. Suite green (67/67 DataGrid, session 128).

Root-cause found while wiring the combination: the virtualized
`BuildShimVisualTree` branch (host is `DataGridRowsPresenter`) never called
`ItemContainerGenerator.ResetContainers()`, so containers registered by an
earlier manual pass (or a different virtualized window) lingered and
`ContainerFromItem` resolved stale row instances — cells empty, edit no-op.
Fixed by resetting the registry in that branch (DataGrid.cs).

Remaining from the earlier recording: nothing — the only untested combination
was header-presenter + editable source; that's now covered.

---

## 13. Column resize at frozen/non-frozen boundary — DONE (session 129)

**Status:** specifically verified and strengthened.  
**Source:** session121:1128-1132

`FrozenColumns_BoundaryResizeKeepsFrozenCellTracked` already resized the last
frozen + first non-frozen column with `FrozenColumnCount = 1`; session 129
added width-change assertions (frozen/nonFrozen Width must actually grow, not
just report accepted) plus width-before/after fields on the readback probe.
Resize at the boundary works: both columns grow 220 → 260 and the frozen
cell's screen X stays put (1px tolerance).

---

## 14. Binding-driven `DesiredSize` not remeasured

**Status:** Uno/Skia-desktop layout issue, not fixable from shim code.  
**Source:** session121:1637-1650

`DataGridBoundColumn.GenerateElement` creates an unparented element, binds
its property via `ApplyBinding`, then returns it for the caller to parent.
The binding target property never triggers measure invalidation when the
value resolves, because the element was unparented at binding-setup time.
Direct (non-binding) property assignment measures correctly on the first pass.

Would require Uno.UI-internals-level investigation — beyond what's reasonable
from this shim's own code.

---

## 15. DevFlow infrastructure fixes (not yet released)

**Status:** source fixes exist in `wpf-labs` working tree, not yet released.  
**Source:** session122:240-242, session120:746-750

- **`TryInvokeSelectionItemPattern` fallback** (session122) — new fallback in
  `UnoAgentService.cs` for tapping `NavigationViewItem` elements by setting
  `IsSelected` directly. Source-only; needs repack/version bump of
  `LeXtudio.DevFlow.Agent.Uno` NuGet package.
- **`DragRequest.Global` default fix** (session120:746-750) — `DragRequest.Global`
  changed from `false` to `true` in `DevFlowAgentServiceBase.cs`. Needs commit,
  review, and NuGet release.

---

## 16. `rowHeight: 1` observation on .NET 10.0.4

**Status:** uninvestigated.  
**Source:** session120:228-241

On this machine's installed .NET 10.0.4 runtime, `TypeDef` reports 5318 rows
(not 2400 as session 119's docs recorded) and `rowHeight: 1` (the
border-collapse symptom). Should be checked on a clean `master` checkout to
confirm it predates session 120's changes before anyone spends time on it.
Likely either a different installed CoreLib/table-schema version or a
`net10.0-desktop` SDK-version-specific regression.

---

## 17. Header interactive drag verification

**Status:** blocked on Uno Platform synthetic-click issue; the
`ReorderGrid_HeaderDragDevFlowUpdatesDisplayOrder` integration test is gated
behind `DATAGRID_DRAG_TESTS=1` (2026-08-11, Uno 6.6 verification pass) because
cliclick launched *from inside the host app* needs macOS Accessibility (TCC)
permission for the host process (granted only to the terminal so far), and the
drag-reorder itself remains blocked by the synthetic-click gap below. **Source:**
session120:840-841

Real interactive mouse-drag feel (smooth cursor tracking) not verified.
Blocked on `docs/uno-macos-synthetic-click-issue.md` — an Uno Platform macOS
input-bridge gap where `PointerPoint.IsLeftButtonPressed` is never `true` for
synthetic CGEvent clicks, preventing automated testing of any `ButtonBase`-
derived control interaction (sort-click, drag-reorder) on this platform.

## 18. Uno 6.6 verification (2026-08-11)

**Status:** done — DataGrid suite green (62/62), deterministic across runs.

- The DataGrid.IntegrationTestHost had a stale `obj/` locked to Uno.WinUI
  6.5.153; `dotnet restore` after `rm -rf bin obj` picked up 6.6.184 (matching
  the Uno.Sdk 6.6.42 bump in commit 292f209).
- `SelectedRow_UsesWpfFluentAccentWithReadableForeground` failed only in the
  suite: the shared collection's app instance runs tests alphabetically, and
  `datagrid.probe.dark-theme-contrast` permanently flipped the host *page*
  to Dark, so later tests measured dark-theme colors (WPF-faithful black
  foreground on the light accent). Fixed by scoping the dark switch to the
  grid subtree the probe creates (`grid.RequestedTheme = Dark` only), leaving
  the shared host Light.
- The frozen-column tracked-row test (`FrozenColumns_TrackedRowKeepsFrozenX-
  AcrossVerticalScroll`, the pre-6.6 documented failure) now passes.
