# DataGrid Port — Remaining Work

Status as of session 124 (2026-08-11). All items below are **still open**
unless marked otherwise. Items closed in sessions 121–123 (grouping,
hyperlink column, frozen columns, row-details variable-height virtualization,
VSM sort/hover, TextSearch, redundant separator cleanup, Fluent theme, test
consolidation, grid-line rendering) are **not** listed here.

---

## 1. Accessibility / UI Automation

**Status:** fully inert — largest remaining gap.  
**Source:** session121:699-700, session122:20-22, datagrid-compare.md:99-108

`AutomationPeer.FromElement` returns `null`, `ListenerExists` returns `false`.
Real WPF peers (`DataGridAutomationPeer`, `DataGridCellAutomationPeer`,
`DataGridRowAutomationPeer`, etc.) exist in the linked upstream source but
are COM/UIA-based (~36 call sites using `DataGrid` internals not reachable
through the shim bridge). No screen-reader output from the grid.

**Options (not yet decided):**
- (a) Bridge WPF's own `System.Windows.Automation.Peers.DataGridAutomationPeer`
  family onto Uno's native automation peer model.
- (b) Write fresh peer classes against this project's
  `System.Windows.Controls.DataGrid`, using WCT v7's peers as a behavioral
  reference.
- (c) Defer further (current status).

WCT v7's peers cannot be linked directly (60+ compile errors, different
namespaces, different base types).

---

## 2. Frozen columns — vertical scroll interaction

**Status:** test unskipped since session 123; underlying row-sizing cause
possibly still present — needs confirmation, not re-diagnosis.  
**Source:** session121:1120-1124, 1367-1389, 1391-1398; corrected 2026-07-27
(verification pass found `FrozenColumns_TrackedRowKeepsFrozenXAcrossVerticalScroll`
in `tests/DataGrid.IntegrationTests/DataGridIntegrationTests.cs:354` is now a
plain `[Fact]`, not `[Fact(Skip=...)]` as previously recorded here).

The root-cause comment above the test (manual/non-virtualized
`PART_ShimRowsScroll` StackPanel reporting `ExtentHeight == ViewportHeight`
because rows collapse to ~1-2px actual height) is still present verbatim in
the test file, unchanged. It is unclear whether the test now passes because
the underlying gap was actually fixed, or because it passes incidentally
(e.g. changed assertions, different repro path). **Needs a run + read of the
current test body before trusting either "fixed" or "still broken."**

**Unblocked by:** fixing the manual-mode row-sizing gap (item 3 below), if it
turns out to still apply.

---

## 3. Manual-mode row-sizing gap

**Status:** diagnosed, not fixed.  
**Source:** session121:1378-1385

`DataGridRow.Height` has no effect in manual (non-virtualized) mode. WPF
applies `RowHeight` via a default `Style` setter this shim's runtime-built
templates don't have. Nothing currently wires `RowHeight` to
`DataGridRow.Height`.

Rows collapse to ~1-2px actual height, so `PART_ShimRowsScroll`'s StackPanel
reports `ExtentHeight == ViewportHeight` → no vertical scroll bar appears.
This blocks frozen-columns vertical-scroll verification (item 2) and is a
real visual gap for non-virtualized grids with many rows.

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

**Status:** `CoerceValue` is a universal no-op project-wide, except on
`DataGridColumnHeader`.  
**Source:** session106:80-90, session121:1777-1788

`DataGridColumnHeader` received a narrow `CoerceValue` implementation
(session121). The ~25 other `CoerceValueCallback` registrations across linked
DataGrid/DataGridColumn/DataGridCell/DataGridRow files are still dormant
because the base `Control.cs`/`ContentControl.cs`/`ButtonBase.cs`/
`FrameworkElement.cs` all declare empty `CoerceValue(DependencyProperty dp) {}`.

Recommended approach: smallest-blast-radius activation, one property at a
time. The width/frozen coerce callbacks should stay dormant until the shim's
parallel width logic is retired.

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

## 12. Cell editing under the header-presenter path (untested)

**Status:** narrower than previously recorded — cells-presenter editing is
now tested; header-presenter combination is not.  
**Source:** session121:1125-1127; corrected 2026-07-27 (verification pass
found `FrozenColumns_RealCellEditCommits` in
`tests/DataGrid.IntegrationTests/DataGridIntegrationTests.cs:369-383` already
exercises real `BeginEdit`/`CommitEdit` against a grid built with
`ShimSetCellsPresenterHost(true)` — see `MainPage.cs:1811`).

What remains actually untested is editing with `ShimSetHeaderPresenterHost`
(item 11) also enabled — that combination has no coverage. Roma's read-only
metadata grids are still the only real consumer of the presenter path in an
app, so an editable data source under the *header*-presenter combination
specifically is what's needed next.

---

## 13. Column resize at frozen/non-frozen boundary

**Status:** not specifically tested.  
**Source:** session121:1128-1132

Slice 2 verified resize works under the cells presenter in general, but not
specifically for a column at or near the frozen/non-frozen boundary while
`FrozenColumnCount > 0` — a plausible edge case given the arrange math's
boundary-cell clip logic.

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
