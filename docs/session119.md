# Session 119

Date: 2026-06-28

## Goal

Implement **DataGrid UI virtualization** by porting WPF's architecture as faithfully as the
Uno/Skia layout system allows ("照搬 WPF 那一套并全力实现共享代码"). Decision (confirmed with
the project owner): the **Full WPF port** strategy — make the DataGrid a real `ItemsControl`
on Uno whose items host is the upstream `DataGridRowsPresenter`, and give the (currently no-op)
`VirtualizingStackPanel` a functional, Uno-backed measure/arrange/realize core that drives the
existing WPF-style `IItemContainerGenerator`. Upstream `DataGridRowsPresenter`,
`DataGridCellsPresenter`, `DataGridRow`, `DataGridCell`, and all column code stay linked and
shared; only the unavoidable layout/scroll core is reimplemented for Uno.

## Why this is feasible (Starting Baseline)

A survey of the shim found the foundation is far more complete than the inert
`VirtualizingPanelStubs` suggested:

- `ItemsControl : IGeneratorHost`
  ([ItemsControlSpine.cs](../src/LeXtudio.Windows/System.Windows/Controls/ItemsControlSpine.cs))
  already exposes `ItemContainerGenerator`, `CreateContainerForItem`,
  `PrepareContainerForItem`/`PrepareContainerForItemOverride`,
  `ClearContainerForItem`/`ClearContainerForItemOverride`.
- [ItemContainerGenerator.cs](../src/LeXtudio.Windows/System.Windows/Controls/ItemContainerGenerator.cs)
  is a real **lazy, recycling** generator: positional `StartAt`, `GenerateNext(out isNewlyRealized)`,
  a `RecyclableContainers` queue, `Recycle`/`Remove` calling back into the owner's clear hook.
- [VirtualizingPanelStubs.cs](../src/LeXtudio.Windows/System.Windows/Controls/VirtualizingPanelStubs.cs)
  already wires `VirtualizingPanel.ItemContainerGenerator` through
  `ItemsControl.GetItemsOwner(this).ItemContainerGenerator.GetItemContainerGeneratorForPanel(this)`.
- Upstream `DataGridRowsPresenter : VirtualizingStackPanel` and `DataGridCellsPresenter` are
  **already linked** (LeXtudio.Windows.csproj) — they compile against the stubs and do nothing yet.

What is **missing**:

1. `VirtualizingStackPanel` has no real `MeasureOverride`/`ArrangeOverride` — it never realizes a
   visible slice or recycles.
2. The DataGrid still renders via the manual `BuildShimVisualTree()` into a plain `StackPanel`
   (`PART_ShimRowsHost`) inside a native `ScrollViewer`; it does **not** use `DataGridRowsPresenter`
   as its items host, and `GetContainerForItemOverride` is not on the Uno render path.
3. No viewport/scroll feedback reaches the panel (no `ScrollViewer.ViewChanged` hook, no IScrollInfo).

WPF reference sizes (cannot be linked literally — too WPF-internal-coupled, must be reimplemented
Uno-backed against the same generator contract): `VirtualizingStackPanel.cs` 13,052 lines,
`ItemContainerGenerator.cs` 3,111 lines.

Last verified baseline (session 118): WindowsShims 189 passed; Roma.Host build 0 errors;
Roma focused DataGrid metadata tests 9 passed.

## Architecture (target)

```text
DataGrid : MultiSelector (ItemsControl)            [shared upstream + HAS_UNO deltas]
  Template: ScrollViewer > ItemsPresenter > DataGridRowsPresenter (PART_RowsPresenter)
  GetContainerForItemOverride() -> DataGridRow     [shared upstream]
        │  feeds items through ItemContainerGenerator (shim, lazy+recycling)
        ▼
DataGridRowsPresenter : VirtualizingStackPanel     [shared upstream, UNCHANGED]
        │  inherits the real core from ↓
        ▼
VirtualizingStackPanel : VirtualizingPanel         [shim — NEW functional core]
  MeasureOverride:  compute visible slice -> StartAt/GenerateNext -> measure realized rows
  ArrangeOverride:  stack realized rows at index*rowHeight, report extent
  scroll:           viewport from ScrollViewer (pixel ScrollUnit first; IScrollInfo later)
  recycle:          Recycle() rows outside the realized slice + cache
        ▼
DataGridRow / DataGridCellsPresenter / DataGridCell  [shared upstream]
```

Pixel-based scrolling (uniform/estimated row height) is the first target because the native Uno
`ScrollViewer` scrolls pixels; WPF item-based `ScrollUnit.Item` + `IScrollInfo` and variable row
heights are later slices.

## Slice plan

- **Slice 1 — viewport math (this session).** Dispatcher-free `VirtualizingRowsLayout` helper:
  `(itemCount, rowHeight, viewportTop, viewportHeight, cacheRows) -> (firstIndex, count,
  extentHeight, firstItemTop)`. Pure, unit-tested, zero change to the live render path. Mirrors the
  established `DataGridColumnResizeShim.ComputeWidth` helper pattern.
- **Slice 2 — VSP realize/arrange core.** Implement `VirtualizingStackPanel.MeasureOverride`/
  `ArrangeOverride` using Slice 1 + the generator (`StartAt`/`GenerateNext`/`Recycle`), reporting
  extent so a parent `ScrollViewer` scrolls. Driven by an injected viewport for headless tests.
- **Slice 3 — items-host swap.** Make the DataGrid template host `DataGridRowsPresenter` and route
  generation through `GetContainerForItemOverride`/`Prepare`/`Clear`, retiring `BuildShimVisualTree`
  for the data rows (header row handled separately). Keep filter/sort/selection working.
- **Slice 4 — scroll wiring + recycling churn.** Hook the real viewport (ScrollViewer offset),
  enable `VirtualizationMode.Recycling`, verify only ~viewport+cache rows are realized while scrolling.
- **Slice 5 — parity.** Selection across unrealized rows, `BringIndexIntoView`/scroll-into-view,
  row details expansion height, filter re-application, alternating-row striping.
- **Slice 6 (optional) — column virtualization** via `DataGridCellsPanel` (WPF default off).

## Verification Plan

After each slice:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Before closing the session / after Roma probe changes:

```bash
dotnet build ../Roma/src/Roma.Host/Roma.Host.csproj -f net10.0-desktop --no-restore
dotnet test ../Roma/tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter "MetadataTable_RendersDataGrid|MetadataTable_ColumnResizeChangesWidth|MetadataTable_CopySelectedRowProducesClipboardText|MetadataTable_KeyboardSelectionSelectsCellsAndMovesCurrentCell"
```

## Notes

Keep the live `BuildShimVisualTree` path intact until Slice 3 flips the host, so every slice up to
that point is non-regressing. Prefer extending the existing generator over forking it.

## Slice 1 Result

Landed the dispatcher-free viewport math, the pure core the Slice 2 panel will measure against:

- [VirtualizingRowsLayout.cs](../src/LeXtudio.Windows/System.Windows/Controls/VirtualizingRowsLayout.cs)
  — `VirtualizingRowsLayout.Compute(itemCount, rowHeight, viewportTop, viewportHeight, cacheRows)`
  returns `(FirstIndex, Count, ExtentHeight, FirstItemTop)`. Pixel-based, uniform/estimated row
  height, symmetric cache band, fully clamped (empty list, non-positive/non-finite row height →
  realize-all, negative/NaN offsets, zero-height viewport anchor row, end-of-list clamp).
- 10 NUnit tests in
  [VirtualizingRowsLayoutTests.cs](../src/LeXtudio.Windows.Tests/VirtualizingRowsLayoutTests.cs)
  covering each edge case. No change to the live `BuildShimVisualTree` render path — zero regression.

Verification:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Results:

- WindowsShims: 202 passed (10 new), 0 failed.

Next: Slice 2 — wire `VirtualizingStackPanel.MeasureOverride`/`ArrangeOverride` to drive
`StartAt`/`GenerateNext`/`Recycle` over the slice this helper computes, with an injectable viewport
so the realize/recycle behavior is headless-testable before the items-host swap.

## Slice 2 Result

A structural finding reshaped this slice. The shim `Panel` (and therefore `VirtualizingStackPanel`,
`DataGridRowsPresenter`) aliases to `Microsoft.UI.Xaml.FrameworkElement` but is **not** a
`Microsoft.UI.Xaml.Controls.Panel`; its `Children` is a plain `Collection<UIElement>` disconnected
from the live visual tree. Uno never calls `MeasureOverride` on it to host visuals — the live render
still flows through `BuildShimVisualTree` into a real WinUI `StackPanel`. Also `ItemsControl.GetItemsOwner`
/`GetContainerForItemOverride` are stubbed to `null`, and the existing generator realizes
contiguously-from-zero (`_containers[i]` ⇒ item `i`), so it cannot represent a moving window.

So the realize/recycle **state machine** — the genuine heart of virtualization, independent of where
containers are visually positioned — did not yet exist. Built it as a pure, injected-dependency unit,
matching how Slice 1 delivered the math:

- [VirtualizingRowsRealizer.cs](../src/LeXtudio.Windows/System.Windows/Controls/VirtualizingRowsRealizer.cs)
  — `VirtualizingRowsRealizer<TContainer>` keeps exactly the in-window slice (from
  `VirtualizingRowsLayout`) realized: out-of-window containers are cleared and, in Recycling mode,
  pooled for scrolled-in items; already-realized in-window indices are kept without redundant prepare.
  Generic over `TContainer` so the algorithm is decoupled from the WinUI visual tree (UIElements can't
  be constructed off the Uno UI thread — the whole existing test suite avoids instantiating them).
  Honors `VirtualizationMode.Recycling` vs `Standard`.
- 8 NUnit tests in
  [VirtualizingRowsRealizerTests.cs](../src/LeXtudio.Windows.Tests/VirtualizingRowsRealizerTests.cs):
  initial window sizing, no-op re-realize, shared-index instance identity across overlapping scroll,
  recycled-instance reuse across disjoint windows (zero new creates after warmup), Standard-mode
  re-creation, realized-count ≤ window invariant while scrolling, `Clear()` pooling, correct index on
  recycled reuse. No change to the live render path.

Verification:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Results:

- WindowsShims: 210 passed (8 new), 0 failed.

Revised plan for the host-side slices: because the shim panel can't host live visuals, Slice 3 must
either (a) make the live host panel (`PART_ShimRowsHost`, a real WinUI panel) the surface the realizer
positions rows into, or (b) base the shim `DataGridRowsPresenter` on a real WinUI `Panel`. (a) is the
lower-risk continuation of the existing bridge.

## Slice 3 Result — option (b): WinUI Panel as a WPF Panel

Chosen direction (project owner): option (b) — make the WPF panel chain a *live* WinUI panel so the
upstream `DataGridRowsPresenter`/`DataGridCellsPanel` participate in Uno layout directly, maximizing
reuse of the upstream virtualizing-panel code. This slice builds that compatibility layer.

Blast-radius survey first: all live-render `.Children` usage targets real
`Microsoft.UI.Xaml.Controls.StackPanel` (PART_ShimRowsHost, header host, XAML translator), and there is
no shim `StackPanel`/`Grid`/`Canvas`. The *only* compiled consumers of the shim `Panel`'s WPF surface
are the virtualizing chain (`VirtualizingPanel`/`VirtualizingStackPanel` + linked `DataGridCellsPanel`/
`DataGridRowsPresenter`). An API inventory of the two linked panels confirmed `DataGridRowsPresenter`
needs almost nothing beyond what exists; `DataGridCellsPanel` (column virtualization, Slice 6,
WPF-default-off) is the heavy consumer but only needs to keep compiling.

Changes:

- [PanelShims.cs](../src/LeXtudio.Windows/System.Windows/Controls/PanelShims.cs) — `Panel` now derives
  from `Microsoft.UI.Xaml.Controls.Panel` (was `FrameworkElement`). Dropped the detached
  `Collection<UIElement>` children and the `BackgroundProperty` stub (WinUI provides both); kept the
  WPF surface: `IsItemsHost`/`OnIsItemsHostChanged`, and `InternalChildren` mapped onto the live WinUI
  `Children`. Net effect: the WPF virtualizing panel chain is now a real layout element Uno measures/
  arranges, with real visual children.
- [GlobalUsings.cs](../src/LeXtudio.Windows/GlobalUsings.cs) — aliased `UIElementCollection` to
  `Microsoft.UI.Xaml.Controls.UIElementCollection` so linked WPF panel code binds to the live type.
- [DataGridCellsPanel.cs](../ext/wpf/.../System/Windows/Controls/DataGridCellsPanel.cs) — one isolated
  `#if HAS_UNO` cast (`(IList)InternalChildren`): WinUI's `UIElementCollection` implements non-generic
  `IList` explicitly, so the WPF implicit conversion needs an explicit cast. Only compile-affecting;
  column-virtualization path, not the row path.

Verification:

```bash
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop --no-restore   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Results:

- LeXtudio.Windows build: 0 errors.
- WindowsShims tests: 210 passed, 0 failed (no behavior regression from the panel re-base).

### Side repair — Roma.Host probe scaffolding

Building Roma.Host in Debug with project references (the owner flipped `UseNuGetPackages` to `false`)
surfaced 24 pre-existing `CS0103` errors in `RomaIntegrationProbes.cs`: 8 hex-filter probe helpers
were *called* but never defined (committed broken at HEAD from the prior session, unrelated to the
panel work). Reconstructed all 8 from their call sites in
[RomaIntegrationProbes.cs](../../Roma/src/Roma.Host/Diagnostics/RomaIntegrationProbes.cs):

- `FirstHexFilterColumnIndex` — first column whose DataGridExtensions editor is `FilterKind.Hex` (public API).
- `RealizedRowItems` — items of realized `DataGridRow` containers (visual-tree walk).
- `ActiveColumnFilterCount` — reflects into the shim's internal `DataGridFilter.State.ColumnFilters`
  (Roma.Host isn't in `InternalsVisibleTo`; matches the resize probes' reflection style).
- `FindDescendant<T>` / `FindDescendants<T>` — depth-first `VisualTreeHelper` walkers.
- `IsAncestorVisible` — no collapsed ancestor.
- `InvokeButtonClick` / `SetTextBoxTextThroughAutomation` — drive Button.Invoke / TextBox.SetValue via
  automation peers (so real `Click`/`TextChanged` fire).
- `MetadataHexFilterSnapshot` — 12-field JSON snapshot mirroring `MetadataHexFilterTypingSnapshot`.

Verification: `dotnet build Roma/src/Roma.Host` and `Roma/tests/Roma.IntegrationTests` both succeed,
0 errors. (Live probe execution needs the running app + DevFlow agent on :9223; not run here.)

## Slice 4 Result — functional VirtualizingStackPanel engine

Now that the panel chain is a live WinUI panel (Slice 3), gave `VirtualizingStackPanel` a real
row-virtualizing measure/arrange engine driven by the Slice 1/2 brain:

- [VirtualizingPanelStubs.cs](../src/LeXtudio.Windows/System.Windows/Controls/VirtualizingPanelStubs.cs)
  — `VirtualizingStackPanel` now:
  - tracks the effective viewport via `EffectiveViewportChanged` and invalidates measure on change;
  - in `MeasureOverride`, resolves its owning `ItemsControl` (`GetItemsOwner`), computes the visible
    slice through `VirtualizingRowsRealizer<UIElement>` (which uses the tested `VirtualizingRowsLayout`),
    realizes only those rows via the owner's `CreateContainerForItem`/`PrepareContainerForItem`,
    measures them, refines a uniform row-height estimate, and reports the full extent
    (`itemCount * rowHeight`) so the parent `ScrollViewer` scrolls naturally;
  - in `ArrangeOverride`, stacks each realized row at `index * rowHeight`;
  - recycles off-screen rows (Recycling mode keeps them collapsed for reuse; Standard removes them);
  - raises `OnViewportSizeChanged` so `DataGridRowsPresenter` can redistribute column widths.
- [ItemsControlSpine.cs](../src/LeXtudio.Windows/System.Windows/Controls/ItemsControlSpine.cs) —
  `GetItemsOwner` is now functional: the `IsItemsHost` panel walks up the live visual tree to its
  owning `ItemsControl`.

Verification & scope honesty: the engine is **inert until a panel is installed as an items host**
(`GetItemsOwner` returns null on the current manual `BuildShimVisualTree` path), so it cannot regress
today's render. Correctness of the windowing/recycling decisions is inherited from the Slice 1/2 unit
tests; the live measure/arrange itself can't be headless-tested (a `VirtualizingStackPanel` is now a
`UIElement`, not constructible off the Uno UI thread — the same reason the brain was built as plain
classes). Runtime behavior is verified in Slice 5 when the presenter is swapped into the DataGrid
template and exercised by a Roma probe.

```bash
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop --no-restore   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
dotnet build ../Roma/src/Roma.Host/Roma.Host.csproj -f net10.0-desktop                       # 0 errors
```

Results: LeXtudio.Windows 0 errors; WindowsShims 210 passed / 0 failed; Roma.Host 0 errors.

## Slice 5 Result — live items-host swap, proven on a 2400-row grid

Wired the Slice 4 engine into the real DataGrid behind an **opt-in gate** (default off → the manual
`BuildShimVisualTree` path is unchanged, zero regression):

- [DataGrid.cs](../src/LeXtudio.Windows/System.Windows/Controls/DataGrid.cs) — added a virtualized
  `ControlTemplate` (`ShimVirtualizedTemplateXaml`) whose `PART_ShimRowsHost` is a live
  `DataGridRowsPresenter` with `IsItemsHost="True"` (instead of a `StackPanel`), and
  `ShimSetRowVirtualization(bool)` to switch templates at runtime. Both `BuildShimVisualTree` and
  `RefreshFilteredRows` early-return when the host is a `DataGridRowsPresenter` (rows are generated by
  the presenter via `GetContainerForItemOverride`/`PrepareContainerForItemOverride`, which already
  call `row.PrepareRow` + tracker on the linked path).
- [RomaIntegrationProbes.cs](../../Roma/src/Roma.Host/Diagnostics/RomaIntegrationProbes.cs) — new
  `roma.probe.metadata-virtualization`: opens a metadata table, enables virtualization, and reports
  `total` items vs `realized` (visible) `DataGridRow` containers, before and after a scroll.

Runtime verification (launched Roma, opened `System.Private.CoreLib.dll` 10.0.1, DevFlow on :9223):

```text
roma.probe.metadata-virtualization(CoreLib, "TypeDef")
  → { total: 2400, realizedInitial: 25, virtualized: true }
roma.probe.metadata-open-table(CoreLib, "Module")   (default manual path)
  → { hasGrid: true, rows: 1, columns: 8 }           (no regression)
```

**Only 25 of 2400 rows are realized** — row virtualization works end-to-end on a real metadata grid.

Honest gaps (deferred):

- **Scroll does not yet shift the window** (`firstAfterScroll` stayed 0): `ScrollViewer.ChangeView`
  updates the effective viewport asynchronously, but the probe re-measures synchronously, so the
  realized window hadn't moved yet. Needs either an async settle in the probe or verifying the engine
  reacts to a programmatic offset change — Slice 6.
- **`MethodDef` probe timed out**: ILSpy still materializes the *entire* `Items` collection
  (tens of thousands of rows) synchronously on open — a non-UI cost independent of UI virtualization.
- **Parity not done under virtualization**: pinned header, alternating-row background, selection
  visuals, filter re-application, and generator registration for selection/scroll-into-view are not
  yet carried on the generated-row path. This is why virtualization stays opt-in.

Verification: WindowsShims 210 passed / 0 failed (manual path untouched); LeXtudio.Windows + Roma.Host
build 0 errors; runtime probe confirms windowing.

## Slice 6 Result — scrolling shifts the realized window

The Slice 5 gap (window stuck at the top) was an async-viewport timing issue, now resolved:

- [VirtualizingPanelStubs.cs](../src/LeXtudio.Windows/System.Windows/Controls/VirtualizingPanelStubs.cs)
  — `VirtualizingStackPanel` now hooks the **ancestor `ScrollViewer`'s `ViewChanged`** on `Loaded`
  (the presenter is the scroll content, so `VerticalOffset` is the window top, `ViewportHeight` the
  band) in addition to `EffectiveViewportChanged`; both funnel through `SetViewport(top, height)` →
  `InvalidateMeasure`. Added an internal `ShimForceViewport(top, height)` test seam so the
  offset→window logic can be driven deterministically without async scroll callbacks.
- [RomaIntegrationProbes.cs](../../Roma/src/Roma.Host/Diagnostics/RomaIntegrationProbes.cs) — the
  virtualization probe now drives `ShimForceViewport(extent/2, 500)` and re-checks the window.

Runtime verification (CoreLib `TypeDef`, 2400 rows):

```text
{ total: 2400, realizedInitial: 25, firstInitial: 0,
  realizedAfterScroll: 27, firstAfterScroll: 1198, virtualized: true }
```

The window **moved from rows [0,25) to [1198,1225)** and stayed ~25 rows — scrolling shifts a fixed
viewport-sized window and recycles the rest. Row virtualization is functionally complete.

Note discovered during testing: in this probe's layout, `ScrollViewer.ViewportHeight` read as
~unbounded (an earlier run realized 1198→end for a viewport-height of the full extent — the engine
behaving correctly for a bad input). Real interactive scrolling needs the virtualized template's
`ScrollViewer` to be height-constrained so `ViewportHeight` is sane; tracked for the parity slice.

Verification: WindowsShims 210 passed / 0 failed; LeXtudio.Windows + Roma.Host build 0 errors;
runtime probe confirms window-shift + recycling.

## State after Session 119

Row virtualization (the Full WPF port direction) is implemented and proven end-to-end on a real
2400-row metadata grid, behind an opt-in gate (`ShimSetRowVirtualization`) so the default render is
unchanged. Brain (layout + realizer) is unit-tested; the live panel + host-swap + scroll are verified
by Roma probe.

Remaining for a future session to make virtualization the default:

1. Height-constrain the virtualized `ScrollViewer` so real interactive scrolling realizes a sane window.
2. Parity on the generated-row path: pinned header, alternating-row background, selection visuals +
   generator registration (scroll-into-view), filter/sort view feeding the presenter
   (`OrderedItems()` instead of raw `Items`).
3. Address the synchronous full `Items` materialization for very large tables (e.g. `MethodDef`).

## Session 119 (cont.) — Parity slices

### Slice 7 — generated-row decoration + generator registration

Generated rows now match the manual path's appearance/behavior, via realize/recycle hooks:

- [ItemsControlSpine.cs](../src/LeXtudio.Windows/System.Windows/Controls/ItemsControlSpine.cs) — new
  virtual `ShimOnContainerRealized(container, item, index)` / `ShimOnContainerRecycled(container, item)`
  hooks (carry the display index the WPF `PrepareContainerForItemOverride` doesn't get).
- [VirtualizingPanelStubs.cs](../src/LeXtudio.Windows/System.Windows/Controls/VirtualizingPanelStubs.cs)
  — the realizer's prepare/clear callbacks invoke those hooks.
- [DataGrid.cs](../src/LeXtudio.Windows/System.Windows/Controls/DataGrid.cs) — extracted
  `ShimDecorateRow` (shared by the manual builder and the generated path: alternating background,
  selection highlight, separator) and overrode the hooks to decorate + register/unregister in the
  `ItemContainerGenerator` (so `ContainerFromItem` — selection / scroll-into-view — works).
- [ItemContainerGenerator.cs](../src/LeXtudio.Windows/System.Windows/Controls/ItemContainerGenerator.cs)
  — added `UnregisterContainer`.

### Slice 8 — filtered/sorted view feeds the presenter

- `ItemsControl` exposes `ShimRealizationCount` / `ShimRealizationItemAt(index)` (default: raw `Items`);
  `DataGrid` overrides them to expose a cached `OrderedItems()` (filter + sort) view, so virtualization
  honors column filters and sorting. `ShimInvalidateRealizationView` rebuilds the cache and calls the
  presenter's `ShimResetRealization` (recycle all + re-measure) wherever the manual path would rebuild.

### Uno border-measure quirk (root-caused by bisection)

Enabling the per-row decoration initially collapsed every row to ~1px (extent 2400 / rowHeight 1 on a
2400-row table). Bisected through the Roma probe (added `extent`/`rowHeight` diagnostics): the cause is
**setting `row.BorderThickness` on a row measured by the `VirtualizingStackPanel`** (infinite-width
constraint) — Uno then measures the row to border-only height, content contributing 0. The StackPanel
manual path tolerates the same border. Alternating background and selection are unaffected. Fix: the
virtualized path applies `ShimDecorateRow(..., includeSeparator: false)`; the inter-row separator line
is deferred. Tracked as a follow-up (find a separator mechanism that survives the virtualizing measure,
e.g. a cell-level bottom border or a finite-width row measure).

### Verification (runtime, CoreLib `TypeDef`, 2400 rows)

```text
{ total: 2400, realizedInitial: 25, firstInitial: 0,
  realizedAfterScroll: 27, firstAfterScroll: 1198,
  extent: 52800, rowHeight: 22, virtualized: true }
```

Windowed realization (25 of 2400), correct ~22px row height, window shifts to the middle on scroll, and
rows carry alternating background + selection + generator registration. WindowsShims 210 passed / 0
failed; LeXtudio.Windows + Roma.Host build 0 errors.

Parity still remaining: pinned (non-scrolling) header on the virtualized template; the deferred row
separator; height-constraining the virtualized `ScrollViewer` so live interactive scrolling (not just
the probe's deterministic viewport) realizes a sane window; filter/sort runtime probe.

### Slice 9 — pinned column header on the virtualized template

WPF pins the column headers; the virtualized template now does too:

- [DataGrid.cs](../src/LeXtudio.Windows/System.Windows/Controls/DataGrid.cs) — `ShimVirtualizedTemplateXaml`
  is now a `Grid` with a pinned `PART_ShimHeaderHost` (row 0, `Auto`) above a vertical-only
  `ScrollViewer` (row 1) hosting the presenter; horizontal scroll is disabled so rows measure at a
  finite width and align with the header. `ShimBuildVirtualizedHeader` builds the existing
  `BuildHeaderRow()` (column headers, resize grippers, filter buttons, sort) into the pinned host.
  `ShimSetRowVirtualization` / the virtualized `BuildShimVisualTree` branch now set `_visibleColumns`
  and build the header before invalidating the realization view.

Retested whether the finite-width measure would let the separator border survive — it does **not**
(rowHeight still collapsed to 1 with the border on), so the separator stays deferred; the quirk is
specific to `BorderThickness` on a row measured by the panel, not the width constraint.

Runtime verification (CoreLib `TypeDef`, 2400 rows):

```text
{ total: 2400, realizedInitial: 25, firstAfterScroll: 1198,
  extent: 52800, rowHeight: 22, headerCells: 9, virtualized: true }
```

Pinned header (9 columns) present, rows correctly ~22px and windowed (25 of 2400), window shifts on
scroll. WindowsShims 210 passed / 0 failed; LeXtudio.Windows + Roma.Host build 0 errors.

Parity still remaining: the deferred row separator (find a separator that survives the virtualizing
measure — e.g. a cell-level bottom border); height-constraining the live `ScrollViewer` so interactive
scrolling realizes a sane window (the probe drives a deterministic viewport); horizontal-scroll header
sync if wide tables need it; a filter/sort runtime probe.

### Slice 10 — viewport authority, horizontal-scroll header sync, filter probe

Closed three of the four remaining parity items:

- **(2) Live-scroll viewport authority.**
  [VirtualizingPanelStubs.cs](../src/LeXtudio.Windows/System.Windows/Controls/VirtualizingPanelStubs.cs)
  — `EffectiveViewportChanged` is now the **authoritative** viewport source. It reports the true
  visible band in the panel's own coordinates accounting for *all* ancestor clipping, so the window
  stays sane even when the immediate `ScrollViewer` is measured unbounded (`ViewportHeight ==` content
  height — exactly the earlier "realized 498/1198→end" anomaly). The ancestor `ScrollViewer.ViewChanged`
  is now only a re-measure nudge, not a value source.
- **(3) Horizontal-scroll header sync.**
  [DataGrid.cs](../src/LeXtudio.Windows/System.Windows/Controls/DataGrid.cs) — the virtualized
  template's rows `ScrollViewer` (`PART_ShimRowsScroll`) re-enables horizontal scrolling;
  `ShimHookHeaderScrollSync` translates the pinned `PART_ShimHeaderHost` by `-HorizontalOffset` on
  `ViewChanged` so columns stay aligned with the rows when scrolled horizontally.
- **(4) Filter runtime probe.** `roma.probe.metadata-virtualization-filter` applies a hex column
  filter via the shim's filter state and asserts the presenter's `ShimRealizationCount` tracks the
  filter-matching count.

Runtime verification (CoreLib `TypeDef`, 2400 rows):

```text
metadata-virtualization        → { realizedInitial: 25, firstAfterScroll: 1198,
                                   rowHeight: 22, headerCells: 9, virtualized: true }
metadata-virtualization-filter → { total: 2400, viewBefore: 2400, viewAfter: 1,
                                   expected: 1, honorsFilter: true }
```

Windowing intact after the viewport/horizontal-scroll changes; the virtualized view honors the column
filter (realized exactly the 1 matching row). WindowsShims 210 passed / 0 failed; LeXtudio.Windows +
Roma.Host build 0 errors.

Only remaining parity item: the deferred per-row separator line (the `BorderThickness`-on-row collapse
under the panel measure; needs a separator drawn some other way — e.g. a cell-level bottom border or a
1px child element rather than the row `Control`'s border). Live mouse-wheel scrolling could not be
exercised by the synchronous probe, but the authoritative `EffectiveViewport` is the WinUI-correct
mechanism and reports sane values.

## Session 119 (cont.) — Shim-reduction audit + Slice 11

Audited the local virtualization shims (~900 lines: `VirtualizingRowsLayout` 129, `VirtualizingRowsRealizer`
135, `VirtualizingPanelStubs` 335, `ItemContainerGenerator` 301) against upstream WPF. Verdict:

- **Keep (good shims):** our ~900-line engine replaces `VirtualizingStackPanel.cs` (13k) +
  `ItemContainerGenerator.cs` (3k), neither linkable on Uno (bound to WPF measure/IScrollInfo/CollectionView
  internals). Porting them is infeasible; the compact reimplementation is the right call.
- **Remove (avoidable duplication):** (A) a parallel filtered view at the DataGrid level
  (`ShimRealizationView`/`Count`/`ItemAt`) instead of WPF's `ICollectionView.Filter`; (B) two realization
  systems (the unused generator `GenerateNext`/session path vs the realizer) + two recycle pools.

### Slice 11 — filtering via `ICollectionView.Filter` (audit item A)

Moved filtering onto the collection view, matching WPF, and removed the parallel shim:

- [ItemCollection.cs](../src/LeXtudio.Windows/System.Windows/Controls/ItemCollection.cs) — added a WPF
  `Filter` predicate with source/view separation: `Refresh()` rebuilds the visible backing to the
  filtered+sorted subset of a retained unfiltered source (kept in sync on insert/remove); a null filter
  restores the full set (unchanged behavior, so non-filtered grids are unaffected).
- [DataGrid.cs](../src/LeXtudio.Windows/System.Windows/Controls/DataGrid.cs) — `ShimApplyFilterView`
  pushes the active DataGridExtensions filters onto `Items.Filter` and `Items.Refresh()` (guarded so the
  collection-reset doesn't trigger a full rebuild / header-focus loss), then refreshes rows. `OrderedItems()`
  is now just `Items` (no re-applied filter); the six filter callbacks call `ShimApplyFilterView`. Removed
  `_shimRealizationView` + `ShimRealizationView` + the `ShimRealizationCount`/`ItemAt` overrides; the VSP
  realizes directly over `owner.Items`.
- [ItemsControlSpine.cs](../src/LeXtudio.Windows/System.Windows/Controls/ItemsControlSpine.cs) — removed the
  `ShimRealizationCount`/`ShimRealizationItemAt` virtuals (the indirection layer is gone).

**Behavior change (intended, WPF-faithful):** `Items.Count` / enumeration now reflect the active filter
(previously Items was the full set and the filter applied only at render). Selection, copy, etc. now see
the filtered set — matching WPF.

Runtime verification (CoreLib `TypeDef`, 2400 rows):

```text
metadata-virtualization-filter → { total: 2400, viewBefore: 2400, viewAfter: 1, expected: 1, honorsFilter: true }
metadata-virtualization        → { realizedInitial: 25, firstAfterScroll: 1198, rowHeight: 22, headerCells: 9, virtualized: true }
```

`Items.Count` drops to the filtered count (1) and the virtualized view honors it; windowing unaffected.
WindowsShims 210 passed / 0 failed; LeXtudio.Windows + Roma.Host build 0 errors.

### Slice 12 — audit item B (attempted, reverted)

Attempted to fold `VirtualizingRowsRealizer` into the `ItemContainerGenerator` (single windowed store +
single recycle pool) and have the VSP drive realization through the generator (`ShimRealizeAt` /
`ShimRecycleOutside`), mirroring the WPF panel↔generator relationship.

Outcome: **reverted.** With the generator-driven realization, rows measured at border-only height
(`extent 2400 / rowHeight 1`) instead of 22px — i.e. cells didn't contribute to the row measure — even
after matching the realizer's exact add→prepare→decorate ordering and two-pass (realize-all-then-measure)
structure. The realizer path produces correct 22px rows from a structurally identical sequence; the
subtle Uno templating/measure-timing difference that breaks the generator-driven path wasn't worth more
probe cycles to root-cause for an optional, behavior-neutral refactor. Reverted the seven B files to the
committed Slice-11 state (`git checkout HEAD -- …`); the realizer remains as the (headless-unit-tested)
windowing brain with the generator as the WPF-contract registry.

Verification after revert: WindowsShims 210 passed / 0 failed; runtime probes unchanged
(`metadata-virtualization` → realized 25/2400, rowHeight 22, header 9, shift 1198;
`metadata-virtualization-filter` → viewAfter 1, honorsFilter true).

Conclusion of the shim-reduction audit: item A (filtering via `ICollectionView.Filter`) landed and
removed the parallel realization-view shim. Item B (unifying the realizer and generator) is **not worth
pursuing** — it trades tested code for coupling and a measure regression, with only aesthetic WPF-fidelity
gain, since our generator can't be the real (un-portable) WPF generator anyway. The realizer + generator
split is the right structure to keep.

### Slice 13 — scroll-into-view / devirtualization (parity A1)

Implemented `VirtualizingPanel.BringIndexIntoView` for the live panel so an off-screen item can be
scrolled to and realized (the basis for selection / keyboard navigation reaching unrealized rows):

- [VirtualizingPanelStubs.cs](../src/LeXtudio.Windows/System.Windows/Controls/VirtualizingPanelStubs.cs)
  — `BringIndexIntoView(index)` scrolls the owning `ScrollViewer` to `index * rowHeight` AND sets the
  viewport directly (so the next measure realizes the target window synchronously, not waiting on the
  async `ViewChanged`).
- [DataGrid.cs](../src/LeXtudio.Windows/System.Windows/Controls/DataGrid.cs) — `ShimScrollItemIntoView(item)`
  finds the item's index and drives the presenter's `BringIndexIntoView`, returning whether the item is
  realized afterwards.
- `roma.probe.metadata-scroll-into-view` verifies it.

Runtime verification (CoreLib `TypeDef`, 2400 rows):

```text
{ targetIndex: 1500, realizedAfter: true, firstAfter: 1498, broughtToTarget: true }
```

`BringIndexIntoView(1500)` moved the realized window to start at 1498 (target + cache) and realized the
target row. Caveat: the probe layout's effective viewport is large enough that the target was already
realized before scrolling (`realizedBefore: true`), so the off-screen→on-screen transition itself
couldn't be staged headlessly; the proof is that the window is repositioned *at* the target.
WindowsShims 210 passed / 0 failed; LeXtudio.Windows + Roma.Host build 0 errors.

### Slice 14 — huge tables open (parity C1)

`MethodDef` (≈9–50k rows) previously **timed out on open**. Two contained fixes:

- [ItemCollection.cs](../src/LeXtudio.Windows/System.Windows/Controls/ItemCollection.cs) +
  [ItemsControlSpine.cs](../src/LeXtudio.Windows/System.Windows/Controls/ItemsControlSpine.cs) —
  `ItemCollection.ReplaceAll(IEnumerable)` bulk-populates with a **single Reset** instead of N per-item
  `Add` notifications; `SyncItemsFromSource` uses it. Previously each `Add` fired `CollectionChanged` →
  `BuildShimVisualTree` on a hooked grid → **O(n²)**.
- [DataGrid.cs](../src/LeXtudio.Windows/System.Windows/Controls/DataGrid.cs) — `BuildShimVisualTree`
  auto-switches to the virtualized presenter when `Items.Count > 1000` (`ShimAutoVirtualizeThreshold`),
  so a huge table realizes only its visible window instead of eagerly building tens of thousands of
  rows. Metadata grids are read-only, so editing-under-virtualization parity is moot.

Runtime verification (CoreLib):

```text
metadata-virtualization(MethodDef) → { total: 9278, realizedInitial: 25, rowHeight: 22, virtualized: true }
metadata-open-table(MethodDef)     → opens fast: hasGrid true, rows 9278, columns 9, headerCells 9  (auto-virtualized, no explicit enable)
metadata-open-table(Module, 1 row) → unchanged: manual path, rows 1, columns 8
```

`MethodDef` now opens (was a hard timeout); small tables keep the manual path. WindowsShims 210 passed /
0 failed; builds clean. Behavior change (intended): metadata tables with >1000 rows now render virtualized
by default.

### High-value parity round — summary

- **A1 scroll-into-view** — done (`BringIndexIntoView` devirtualizes/positions the window at an item).
- **C1 huge tables** — done (`MethodDef` opens via bulk-populate + auto-virtualize).
- **B1 reuse `DataGridColumnHeadersPresenter`** — investigated, **not pursued**: like audit-item B, it
  requires making the linked header presenter + `DataGridColumnHeadersPanel` functional AND migrating all
  the manual header interaction (resize / reorder / filter / sort / auto-width wired on `_headerCells`),
  a large high-risk surgery for structural-only benefit while the manual header works. Deferred.

### Slice 15 — off-screen selection survives virtualization

Roma's metadata navigation (GoToToken etc.) calls `DataGrid.SelectItem(item)` to select+reveal a row,
which under virtualization may be off-screen / unrealized. Two fixes:

- [DataGrid.cs](../src/LeXtudio.Windows/System.Windows/Controls/DataGrid.cs) — implemented the WPF
  `OnBringItemIntoView(ItemInfo)` hook (driven by `ScrollIntoView`): a realized container is brought
  into view via `StartBringIntoView`; an unrealized one resolves the items host
  (`GetTemplateChild("PART_ShimRowsHost")`) and calls `BringIndexIntoView(index)` + a synchronous
  `host.UpdateLayout()` so the target window realizes before the async `ScrollViewer.ViewChanged` can
  reset the forced viewport.
- [ExtensionMethods.cs](../../Roma/ext/ilspy/ILSpy/ExtensionMethods.cs) (ILSpy, ROMA_UNO) — `SelectItem`
  now sets the **engine selection** (`view.SelectedItem = item`) before scrolling, so the selection
  survives container recycling (a re-realized row reads selected state from the DataGrid's selection via
  `IsRowItemSelected`, not a transient row flag), then `ScrollIntoView` + `UpdateLayout`.

Runtime verification (CoreLib `TypeDef`, 2400 rows; target index 1500 pinned off-screen):

```text
metadata-select-offscreen → { realizedBefore: false, engineSelected: true, visuallySelected: true, firstAfter: 1498 }
```

Selecting an off-screen item sets engine selection, scrolls the window to it (first realized row 1498 ≈
target), and the realized row shows selected. WindowsShims 210 passed / 0 failed; builds clean.

### Slice 16 — keyboard navigation reaches off-screen rows

[DataGrid.cs](../src/LeXtudio.Windows/System.Windows/Controls/DataGrid.cs) — `MoveSelectionToIndex` /
`MoveSelectionByOffset` (the targets of Up/Down/Home/End/PageUp/PageDown in row-selection mode) were
operating only over **realized** containers, so under virtualization they couldn't move past the visible
window (End stopped at the last realized row). Rewrote them in **item-index space**: the current item
comes from the engine selection, the target is clamped against `Items.Count`, then `OnBringItemIntoView`
scrolls/realizes it and `HandleShimRowClicked` applies selection on the now-realized row (engine-selection
fallback otherwise). Works for both the manual and virtualized paths.

Runtime verification (CoreLib `TypeDef`, target 1500 off-screen):

```text
metadata-keyboard-offscreen → { realizedBefore: false, engineSelected: true, visuallySelected: true, firstAfter: 1498 }
```

### Option 1 (virtualization parity) — conclusion

- **Off-screen selection** (Roma's GoToToken/navigation `SelectItem` path) — done (Slice 15).
- **Keyboard navigation to off-screen rows** — done (Slice 16).
- **Row-details variable height under virtualization** — **moot for Roma**: the tables that use
  `RowDetails` are the PE-header tables (DOS/COFF/Optional, <50 rows → manual path), while the tables
  large enough to auto-virtualize (TypeDef/MethodDef, thousands of rows) don't use row details. No Roma
  scenario hits expanded details inside a virtualized grid, so the uniform-row-height assumption holds.

Virtualization parity for Roma's real usage is complete: large tables auto-virtualize, scroll/recycle,
filter/sort, fixed header, and selection + keyboard navigation correctly reach (and reveal) off-screen
rows. WindowsShims 210 passed / 0 failed; LeXtudio.Windows + Roma.Host build 0 errors.

### Slice 17 — cross-platform shell commands (Open Folder / Command Line)

Roma's assembly-tree context-menu "Open containing folder" and "Open command line here" were Windows-only
and dead on macOS. Ported ProjectRover's cross-platform approach (dropping its settings-section dependency
for sensible per-OS defaults):

- [GlobalUtils.wpf.cs](../../Roma/ext/ilspy/ILSpy/Util/GlobalUtils.wpf.cs) — `OpenTerminalAt` was
  hard-coded `cmd.exe`; now branches by `RuntimeInformation`: Windows `cmd.exe`, macOS Terminal.app via
  `osascript`, Linux common emulators (gnome-terminal/konsole/xfce4/x-terminal-emulator/xterm).
- [ShellHelper.cs](../../Roma/ext/ilspy/ILSpy/Util/ShellHelper.cs) — `OpenFolderAndSelectItem(s)` used
  `shell32` P/Invoke, which threw `DllNotFoundException` (not caught by the COM/Win32 filter) on macOS.
  Now branches: Windows keeps the shell32 multi-select; macOS reveals via `open -R`; Linux opens the
  containing folder via `xdg-open`. `OpenFolder` likewise uses `open`/`xdg-open`/shell-execute.

Verification: Roma.Host builds 0 errors; `open` and `osascript` resolve on macOS, `osascript` runs, and
the Terminal AppleScript compiles. Actual Finder-reveal / Terminal-launch are GUI side effects (not
asserted headlessly), but the logic mirrors ProjectRover's shipping macOS implementation.

### Slice 18 — update check against project-roma GitHub releases

Roma's update check pointed at ILSpy's upstream `updates.xml` feed and compared against the ILSpy *engine*
version, so it never reflected Roma's own releases. Repointed it at GitHub (adapted from ProjectRover,
which uses Octokit — Roma uses plain HttpClient + System.Text.Json, no new dependency):

- [UpdateService.cs](../../Roma/ext/ilspy/ILSpy/Updates/UpdateService.cs) — `GetLatestVersionAsync` now
  queries `https://api.github.com/repos/lextudio/project-roma/releases/latest` (the API already excludes
  drafts/prereleases), parses `tag_name` (e.g. `v0.2.0` → `0.2.0.0`, stripping `v`/pre-release suffix) and
  uses `html_url` as the download link. Dropped the XML/redirect machinery.
- [AppUpdateService.cs](../../Roma/ext/ilspy/ILSpy/Updates/AppUpdateService.cs) — `CurrentVersion` now
  resolves Roma's *own* app version (this code is linked into Roma.Host, so `typeof(...).Assembly` is
  Roma's, from GitVersion), normalized to `Major.Minor.Build.0`. This fixes both comparison sites (the
  About-page panel and `CheckForUpdateInternal`) at once.
- `roma.probe.check-update` exercises the live query.

Runtime verification (against the real repo):

```text
check-update → { current: 0.2.1.0, latest: 0.2.0.0,
                 downloadUrl: https://github.com/lextudio/project-roma/releases/tag/v0.2.0,
                 updateAvailable: false }
```

Queries the correct repo, parses the tag, compares against Roma's own version (0.2.1 > 0.2.0 → no update).
Roma.Host builds 0 errors; WindowsShims 210 passed / 0 failed (unaffected — changes are in Roma).

### Slice 19 — terminal preference + recent-fonts (ported from ProjectRover)

Two settings-backed features adapted from Rover:

- **Terminal preference** for "Open command line here":
  [RomaHostSettings.cs](../../Roma/src/Roma.Host/RomaHostSettings.cs) gained `PreferredTerminalApp` +
  `CustomTerminalPath` (single value, interpreted per current OS — leaner than Rover's per-OS triplet,
  appropriate for Roma). [GlobalUtils.wpf.cs](../../Roma/ext/ilspy/ILSpy/Util/GlobalUtils.wpf.cs)
  `OpenTerminalAt` reads it via `App.ExportProvider → SettingsService → RomaHostSettings` and switches
  per platform (macOS: Terminal.app / iTerm2 / System Default / Custom; Windows: cmd / PowerShell /
  pwsh / Windows Terminal / Custom; Linux: gnome-terminal / konsole / xfce4 / xterm / Custom), falling
  back to the per-OS default when unset. [MiscSettingsPanel](../../Roma/src/Roma.Host/Options/MiscSettingsPanel.xaml)
  exposes a current-OS dropdown + custom-path box.
- **Recent fonts**: ported [RecentFontsCache.cs](../../Roma/src/Roma.Host/RecentFontsCache.cs)
  (most-recent-first, capped at 8, persisted under LocalApplicationData) + a `RecentFontsEnabled` setting
  with a Misc-panel checkbox, AND wired it into Roma's existing font picker
  ([DisplaySettingsPanel](../../Roma/src/Roma.Host/Options/DisplaySettingsPanel.xaml.cs)): on load the
  combo floats recently-used fonts to the top (when enabled), and picking a font records it via
  `RecentFontsCache.Update`. Mirrors ProjectRover's display panel.

Verification (`roma.probe.host-settings`): `terminalRoundTrip: true` (settings section persists),
`recentFontsEnabled: true`, `recentFontTop: RomaProbeFont`, `recentFontCachesIt: true` (cache round-trips).
Roma.Host builds 0 errors. The OptionsDialog dropdown's visual interaction and the actual per-preference
terminal launch are GUI side effects (not asserted headlessly); the settings they read/write are verified.

## DataGrid port — remaining-gap survey and plan for the next session

Cross-checked `docs/DATAGRID.md`'s feature table against the current source tree
(`src/LeXtudio.Windows/System.Windows/...`) to separate what is genuinely still
missing from what the older doc's table entries describe stale. Row/cell
container behavior, selection, editing, sorting, filtering, and row
virtualization (this session) are done and proven by probes — the remaining
gaps are narrower than the DATAGRID.md table (last substantively updated
around session 110) suggests. Grouped by cost/value, cheapest and
highest-value first:

### Confirmed still-missing (checked against source, not just the doc)

1. **Row separator line under virtualization.** Deferred three times (Slices
   8-10): `BorderThickness` on a row measured by `VirtualizingStackPanel`
   collapses the row to border-only height on Uno. Non-virtualized (manual)
   path still draws it fine. Needs a separator mechanism that survives panel
   measure — likely a 1px child `Border`/`Rectangle` docked to the row's
   bottom edge instead of `Control.BorderThickness`, or a cell-level bottom
   border (`DataGridCell` already draws grid-line borders per session 74-75 —
   extend that path to also draw the row separator instead of relying on the
   row's own border).
2. **Accessibility / UI Automation is fully inert.** Confirmed in source:
   `AutomationPeer.ListenerExists` always `false`, `AutomationPeer.FromElement`
   always `null` (`System.Windows/Automation/Peers/AutomationPeer.cs`), so
   every `DataGrid*AutomationPeer` in the linked upstream code is unreachable
   dead weight. Screen readers get nothing from the grid today. This is a
   real accessibility gap for a shipped app, not just a WPF-fidelity nicety.
3. **Grouping is fully inert.** Confirmed in source: `ItemsControlSpine.
   IsGrouping` is hardcoded `false`; `CollectionViewShims`/`GroupItem` are
   stub types gated on that flag. `DataGrid.GroupStyle` / row-group headers
   cannot work. Only relevant if a future Roma table wants grouped rows —
   currently no consumer needs it.
4. **Frozen columns don't actually freeze.** Confirmed: no
   `FrozenColumnCount`-driven clip/scroll offset logic in the current
   `DataGridCellsPanel`/`DataGridRowsPresenter` Uno path — session 71 only
   pinned live *notification* of `IsFrozen`/`DisplayIndex < FrozenColumnCount`
   onto realized visuals, not the actual "stay put while the rest scrolls
   horizontally" layout. Low priority unless a wide metadata table needs
   pinned key columns.
5. **Column-header reuse (`DataGridColumnHeadersPresenter`) not live.**
   `DataGrid.BuildHeaderRow` still hand-builds headers; the linked upstream
   presenter/collection compile but aren't the render path. Investigated and
   explicitly deferred as "high-risk surgery for structural-only benefit"
   (Session 119, "B1" note) — no behavior gap today, just source-purity debt.
6. **Column drag-reorder via floating header / drop separator.** Linked
   upstream `DataGridColumnFloatingHeader`/`DataGridColumnDropSeparator`
   compile (session 93) but the live header still uses the shim-native
   reorder path, not these visuals — cosmetic parity gap only if reorder drag
   currently looks different from WPF.
7. **Hyperlink column is a placeholder.** `DataGridHyperlinkColumn` is a
   `local-shell` stub added only so `CreateDefaultColumn` compiles for `Uri`
   properties; no real hyperlink rendering/navigation. Only matters if a Roma
   table auto-generates a `Uri`-typed column.
8. **Row-details (expander) variable-height rows under virtualization.**
   Noted moot for Roma today (Session 119 "Option 1 conclusion" — the only
   tables using `RowDetails` are small, non-virtualized PE-header tables) but
   would need work if a future large virtualized table adopts row details.

### Already done (do not re-plan these — DATAGRID.md's table entries predate them)

Row/cell selection (single+multi, keyboard, cell unit), sort, column
filter, cell/row editing + validation (`IDataErrorInfo`, `ValidationRule`,
`IEditableObject`), Auto/Star column widths, clipboard copy, row
virtualization + recycling + scroll-shift + auto-virtualize for huge tables,
off-screen selection/keyboard navigation, pinned header, horizontal-scroll
header sync, cross-platform shell commands, update check, terminal/font
preferences.

### Recommended order for a future session

1. **Row separator under virtualization** (item 1) — cheapest, closes the
   last cosmetic gap in the now-default-eligible virtualized render path.
2. **Accessibility (item 2)** — highest real-world value (screen-reader
   users get literally nothing today) but the largest lift: needs an actual
   `AutomationPeer.FromElement`/`ListenerExists` bridge onto Uno's automation
   provider (Uno does have its own automation-peer system under
   `Microsoft.UI.Xaml.Automation.Peers`), then wiring the ~8 linked
   `DataGrid*AutomationPeer` classes to raise through it. Worth scoping as its
   own multi-slice arc rather than folding into a DataGrid session.
3. Items 3-8 are back-burner: no current Roma scenario needs grouping, frozen
   columns, the header-presenter swap, drag-visual parity, real hyperlink
   columns, or virtualized row-details. Revisit only if a concrete consumer
   need appears.

### Slice 20 — row separator under virtualization (item 1, closed)

Root cause was mutating `DataGridRow`'s own `BorderThickness`/`BorderBrush` (the
row `Control`'s DPs, bound into the row template's outer `Border` via
`TemplateBinding`) to draw the separator. Under `VirtualizingStackPanel`'s
infinite-width measure pass this collapsed the row to border-only height on
Uno — confirmed independent of horizontal-scroll/width settings across three
prior sessions (8-10), so it was deferred each time rather than re-investigated.

Fix: stopped drawing the separator via the row's own border entirely and made
it a plain **fixed-height child element** instead — unaffected by the parent
panel's constraint because its size doesn't depend on measurement:

- [DataGridRow.cs](../src/LeXtudio.Windows/System.Windows/Controls/DataGridRow.cs)
  — `RowTemplateXaml` gained a `PART_RowSeparator` (`Border`, `Height=1`,
  `HorizontalAlignment=Stretch`, gray background) as a sibling after
  `PART_DetailsHost`. `OnApplyTemplate` resolves it; new
  `ApplyRowSeparatorVisibility()` toggles it Visible/Collapsed based on
  `HasRowValidationError`, called from `OnApplyTemplate`, `SetRowError`, and
  `ClearRowError`. `ClearRowError` now resets the row-wide error border to
  none (`BorderThickness=0`) instead of restoring it to a 1px separator value,
  since the separator is no longer that border. Removed the now-dead
  `SeparatorBrush`/`SeparatorThickness` static fields.
- [DataGrid.cs](../src/LeXtudio.Windows/System.Windows/Controls/DataGrid.cs) —
  `ShimDecorateRow` dropped the `includeSeparator` parameter (both the manual
  and virtualized paths now call `row.ApplyRowSeparatorVisibility()`
  unconditionally, since the fix applies equally to both); the virtualized
  realize hook (`ShimOnContainerRealized`) call site simplified to match.

Verification:

```bash
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop --no-restore   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop
dotnet build ../Roma/src/Roma.Host/Roma.Host.csproj -f net10.0-desktop                       # 0 errors
```

Results: LeXtudio.Windows 0 errors; WindowsShims 207 passed / 3 failed — the 3
failures are pre-existing on `master` (confirmed via `git stash`: identical
`AmbiguousMatchException` reflection failures in
`BindingExpressionBridgeTests`/`DataGridSelectedCellsTests`/`SelectorSpineTests`
before this change too, unrelated to the separator fix). Roma.Host builds 0
errors. Runtime rendering of the separator under an active virtualized grid
was not re-probed live in this session (no running app instance); the fix
removes the exact mutation identified as the root cause and preserves it as a
static template child, so the border-collapse mechanism no longer applies.

Row separator is the last item from the "Confirmed still-missing" list that's
now closed. Remaining open items: accessibility (2, largest, deserves its own
session), grouping/frozen columns/header-presenter swap/drag visuals/hyperlink
columns/virtualized row-details (3-8, back-burner, no current consumer need).

## Session 119 (cont.) — B1 header-presenter swap: re-investigated, still deferred

User picked item 5 (column-header reuse — making `DataGridColumnHeadersPresenter`
the live header host instead of `DataGrid.BuildHeaderRow`) from the back-burner
list to work on next. Re-investigated given how much has landed since the
original "high-risk surgery" assessment (row virtualization is now a proven
generator-driven live host) to see if the same pattern now makes this
tractable. It does not, for reasons specific to headers, not virtualization:

**What's promising (confirmed in source):**

- `DataGridColumnHeadersPresenter` (linked, session 94) is a plain
  (non-virtualizing) `ItemsControl` over `DataGridCellsPanel` — the same panel
  type already proven live for row cells since session 91/101. Its
  `GetContainerForItemOverride`/`PrepareContainerForItemOverride` just call
  `new DataGridColumnHeader()` + `header.PrepareColumnHeader(item, column)` —
  a shim method our own `BuildHeaderRow` already calls today, so generation
  itself is not the blocker.
- `DataGridColumnHeader`'s resize-gripper drag and sort-click
  (`OnClick` → `PerformSort`) are **already real upstream behavior** wired
  through `PART_LeftHeaderGripper`/`PART_RightHeaderGripper` `Thumb`s in our
  own template (session 77) — not something the swap would need to add.

**Why it's still a large, risky refactor (the actual blocker, more precise
than the earlier assessment):** `DataGrid._headerCells` — the
manually-populated `List<DataGridColumnHeader>` from `BuildHeaderRow` — is
read at **21 call sites**, not just at generation time:

- Auto/Star column-width computation (`_headerCells[i].DesiredSize.Width` /
  `.ActualWidth`) — the already-fragile width pass from sessions 41-42.
- Column resize (`_headerCells[i].Width = w`).
- Drag-reorder drop-slot math (`OnHeaderPointerMoved`/`Released`, keyed by
  index into `_headerCells`/`_visibleColumns` and positions relative to the
  manually-built `_headerHostPanel`).
- The live-notification dispatch loop (session 70-75 batch) that pushes
  `GridLinesVisibility`/`CellStyle`/`ColumnHeaderStyle`/`RowStyleSelector`-style
  changes onto realized headers by iterating `_headerCells` directly.

`PrepareContainerForItemOverride` is upstream, unguarded (no local `HAS_UNO`
override point), and sets `Column` via a private field with no property-changed
notification to hook into — so none of the above could be quietly redirected
to read from the presenter's generated children without rewriting all four
systems (width, resize, reorder, live-notification) against the
`ItemContainerGenerator`/presenter's realized-child collection instead of
`_headerCells`. Auto/Star width in particular is the same "proxy, not exact"
mechanism flagged as fragile in sessions 41-42 — touching its measurement
source now, for a change with **zero behavioral upside** (BuildHeaderRow
already renders resize, reorder, sort, filter, frozen, styles, and gridlines
correctly and is fully tested), is a bad risk/reward trade at any effort level,
not just under the current budget.

**Conclusion:** left unimplemented, matching the Session 119 "B1" note.
Recommend either (a) scoping this as its own dedicated multi-slice arc — one
slice per system (width, resize, reorder, notifications) each swapped and
re-verified against the existing Roma header probes before touching the next —
analogous to how row virtualization took ~10 slices, or (b) leaving it
permanently back-burner, since (unlike accessibility) no user-facing behavior
is blocked on it today. No code changed in this investigation.

User elected to commit to (a) as a real multi-session arc and start slice 1
now, accepting the exploratory risk below.

### Slice 1 (exploratory) — can a non-DataGrid ItemsControl generate its own items here?

Before writing code, re-checked the precedent I'd cited for optimism and found
it didn't hold: `DataGridCellsPresenter` — the other `ItemsControl`-derived
WPF presenter already linked in this codebase (session 91) — has **never
actually been instantiated** (`grep -rn "new DataGridCellsPresenter"` across
`src/LeXtudio.Windows/` returns nothing outside build artifacts). Cell
rendering still goes through `DataGridRow.BuildCells()` manually building a
plain `StackPanel`. Likewise `DataGridRowsPresenter` is never `new`'d directly —
it's placed via XAML in the DataGrid's own template and works because it's a
`Panel` (not an `ItemsControl`) piggybacking on **DataGrid's own**, already-
proven generation via `ItemsControlSpine.GetItemsOwner` walking up to find
DataGrid as the owner. `DataGridColumnHeadersPresenter` is different in kind:
it IS the `ItemsControl` and must generate over its own `ItemsSource`
(`DataGridColumnHeaderCollection`) — a capability this shim has never
exercised for any class other than `DataGrid` itself. Flagged this to the user
before proceeding; they chose to accept the risk and continue.

Investigation turned up one more relevant fact: this shim's
`System.Windows.Controls.ItemsControl` derives from the **real**
`Microsoft.UI.Xaml.Controls.ItemsControl`, not from the shim's own `Control`
(superseding the session 108 note, which predates this). WinUI's real
`ItemsControl` has the same four virtuals as WPF
(`GetContainerForItemOverride`/`IsItemItsOwnContainerOverride`/
`PrepareContainerForItemOverride`/`ClearContainerForItemOverride`), so the
linked upstream `DataGridColumnHeadersPresenter`'s overrides bind to WinUI's
real virtual slots, not a shim reimplementation. However, `Panel.IsItemsHost`
— the flag WinUI's own `ItemsControl`/`ItemsPresenter` machinery uses
internally to find its host panel — is **shadowed** by a same-named local
`DependencyProperty` on the shim `Panel` (`PanelShims.cs`, added for the row-
virtualization work), so setting it only satisfies `ItemsControlSpine.
GetItemsOwner`'s own manual visual-tree walk, not WinUI's native item-host
discovery. That local walk is genuinely generic, though (it just looks for the
nearest `ItemsControl`-typed ancestor), so a `DataGridColumnHeadersPresenter`
with a `DataGridCellsPanel` marked `IsItemsHost` in its template should resolve
itself as the owner the same way DataGrid does for rows — *if* the linked
`DataGridCellsPanel.MeasureOverride` (fully linked WPF source, session 101)
actually drives generation through that same `GetItemsOwner` call, which
wasn't independently re-verified here (it's inherited, not new code).

**Change made** (compiles, builds and tests green, default OFF — zero
regression risk to the working manual header):

- [DataGridColumnHeadersPresenter.uno.cs](../src/LeXtudio.Windows/System.Windows/Controls/Primitives/DataGridColumnHeadersPresenter.uno.cs)
  (new file) — gives the previously template-less linked presenter a minimal
  `ControlTemplate`: a `DataGridCellsPanel` marked `IsItemsHost='True'` as its
  sole child, assigned in an explicit constructor (the shim `Control`'s
  `InitializeDefaultStyleKey()` hook doesn't apply here since this class's
  base really is WinUI's `ItemsControl`, not the shim `Control`).
- [DataGrid.cs](../src/LeXtudio.Windows/System.Windows/Controls/DataGrid.cs) —
  new opt-in `ShimSetHeaderPresenterHost(bool)` (mirrors
  `ShimSetRowVirtualization`): when enabled, `ShimBuildVirtualizedHeader()`
  puts a `DataGridColumnHeadersPresenter` into `PART_ShimHeaderHost` instead of
  `BuildHeaderRow()`'s manual `StackPanel`, then calls `presenter.
  ApplyTemplate()` explicitly — the presenter's own linked `OnApplyTemplate`
  resolves `ParentDataGrid` via a visual-tree walk and sets
  `ItemsSource`/`grid.ColumnHeadersPresenter` from it, so it must run only
  *after* the presenter is parented, not at construction time. Only reachable
  when row virtualization is also enabled (the manual template has no
  standalone header host to swap). Column resize, drag-reorder, and the
  session 70-75 live-style/gridline notification batch are **not** wired to
  this path — they still target the old `_headerCells` list — so enabling
  this only proves/renders column header generation and content (via the
  existing `DataGridHelper.TransferProperty` glue, unchanged), nothing else.
- [RomaIntegrationProbes.cs](../../Roma/src/Roma.Host/Diagnostics/RomaIntegrationProbes.cs)
  — new `roma.probe.metadata-header-presenter`: enables virtualization + the
  header presenter, then reports `realizedHeaders` (count of real
  `DataGridColumnHeader` descendants found) vs `columnCount`, plus the first
  header's text. This is the actual proof point — the reasoning above is not a
  substitute for running it.

**Not verified at runtime in this session** — no running app instance was
available. The mechanism is plausible from source reading (detailed above) but
**unproven**: whether `DataGridCellsPanel.MeasureOverride` actually resolves
`GetItemsOwner` to this presenter and drives real generation, versus silently
rendering zero children, is exactly what `roma.probe.metadata-header-presenter`
needs to answer next time the app can run. Do not assume this works before that
probe reports `generated: true`.

Verification performed:

```bash
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop --no-restore   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop
dotnet build ../Roma/src/Roma.Host/Roma.Host.csproj -f net10.0-desktop                       # 0 errors
```

Results: LeXtudio.Windows 0 errors; WindowsShims 207 passed / 3 failed (the
same pre-existing failures confirmed unrelated in the Slice 20 entry above,
unaffected by this change since it's opt-in/default-off); Roma.Host 0 errors
including the new probe.

**Next step for whoever picks this up:** run `roma.probe.metadata-header-
presenter` against a live Roma instance. If `generated: true` with a sane
`firstHeaderText`, slice 2 is wiring Auto/Star width computation to read from
the presenter's realized headers (via its `ItemContainerGenerator`) instead of
`_headerCells`, verified against the existing width probes before touching
resize/reorder/notifications. If `generated: false`, the next investigation
is why `DataGridCellsPanel.MeasureOverride` isn't resolving/driving generation
for this owner (likely something `GetItemsOwner`'s visual-tree walk finds
before reaching the presenter's template root, or a WinUI template-application
timing gap around the explicit `ApplyTemplate()` call).
