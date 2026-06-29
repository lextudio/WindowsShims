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
