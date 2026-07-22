# Session 120

Date: 2026-07-19

Continuation of the header-presenter swap arc (B1) started at the end of
session 119. Picked up uncommitted working-tree state from that session:
the row-separator fix (session 119 Slice 20), the `ShimSetHeaderPresenterHost`
opt-in scaffolding, `DataGridColumnHeadersPresenter.uno.cs`'s minimal
template, and an already-attempted fix in the `ext/wpf` submodule
(`DataGridCellsPanel.ParentPresenter`, `#elif HAS_UNO` branch) switching the
two-hop `TemplatedParent` walk to `ItemsControlSpine.GetItemsOwner`.

None of this had been runtime-verified yet — session 119 ended with "run
`roma.probe.metadata-header-presenter` against a live Roma instance" as the
explicit next step. Did that.

## What was verified this session

Built `LeXtudio.Windows` (0 errors) and launched Roma.Host directly
(`dotnet run -c Debug -f net10.0-desktop`), confirmed the DevFlow agent came
up on `:9223`, and invoked the probe against a real assembly:

```bash
curl -s -X POST http://localhost:9223/api/v1/invoke/actions/roma.probe.metadata-header-presenter \
  -H "Content-Type: application/json" \
  -d '{"args":["/usr/local/share/dotnet/shared/Microsoft.NETCore.App/10.0.4/System.Private.CoreLib.dll","TypeDef"]}'
```

Result:

```json
{
  "columnCount": 9,
  "realizedHeaders": 1,
  "firstHeaderText": "Microsoft.UI.Xaml.Controls.Grid",
  "presenterHosted": true,
  "panelHosted": true,
  "panelTemplatedParentType": null,
  "standTemplatedParentType": null,
  "generated": false
}
```

**Conclusion: still not working, even with the `GetItemsOwner`-based fix
already in the submodule.** `presenterHosted`/`panelHosted` confirm the new
`DataGridColumnHeadersPresenter`/`DataGridCellsPanel` instances do get placed
in the live tree (the opt-in wiring in `DataGrid.ShimBuildVirtualizedHeader`
works), but only 1 of 9 columns produced a `DataGridColumnHeader` — and that
one's content (`"Microsoft.UI.Xaml.Controls.Grid"`) looks like a stray/default
item rendered via `ToString()` on some unrelated visual, not a real generated
header, so real generation count is effectively **zero**. The
`ParentPresenter` fix (`ItemsControlSpine.GetItemsOwner` instead of the
TemplatedParent walk) did not fix it either: this is new information beyond
what session 119 left off with, since that fix was written but never run.

## Root-cause candidates for next session (not yet investigated)

`GetItemsOwner(panel)` requires `VisualTreeHelper.GetParent(panel)` to reach
an `ItemsControl`-typed ancestor by walking up from the panel. For
`DataGridRowsPresenter` (the already-proven case) the panel *is* the
`ItemsControl`'s items host directly assigned via `PART_ShimRowsHost`, one
level below `DataGrid` itself. For the header presenter, the chain is one
level deeper and self-referential in a way not previously exercised: the
`DataGridCellsPanel` sits inside a `ControlTemplate` applied to
`DataGridColumnHeadersPresenter` (built via `XamlReader.Load`, then
`Template` assigned in the presenter's constructor, then
`presenter.ApplyTemplate()` called explicitly by
`ShimBuildVirtualizedHeader` after parenting). Plausible reasons
`GetItemsOwner` still returns null / generation still doesn't fire:

1. **`VisualTreeHelper.GetParent` may not resolve into `XamlReader.Load`-built
   `ControlTemplate` children the same way it does for
   `DataGridRowsPresenter`'s template** (that template is also built via
   `XamlReader.Load` in `DataGrid.cs`'s `ShimVirtualizedTemplateXaml`, so this
   needs a direct comparison, not an assumption — check whether
   `VisualTreeHelper.GetParent(cellsPanel)` returns the presenter or `null`
   immediately after `ApplyTemplate()`).
2. **Timing**: `ApplyTemplate()` is called once, synchronously, right after
   `headerHost.Child = presenter`. If WinUI defers actual template
   materialization to the next layout pass, the panel may not exist as a
   live visual yet when `GetItemsOwner`/`MeasureOverride` first run, and
   nothing re-triggers generation afterward (no `InvalidateMeasure`/
   `EffectiveViewportChanged` hook exists for the header path the way there is
   for `VirtualizingStackPanel`).
3. **The stray realized header** (`realizedHeaders: 1`, bogus content) suggests
   something elsewhere in the tree independently produces a
   `DataGridColumnHeader` — worth checking whether `FindDescendants` is
   picking up a header from the *manual* `BuildHeaderRow()` path that wasn't
   actually replaced (e.g. a leftover header from before
   `ShimSetHeaderPresenterHost` was toggled, if enabling it doesn't fully tear
   down the prior manual header tree).

## State

Uncommitted working-tree changes from session 119 (row separator fix, header
presenter opt-in scaffolding) are unaffected code-wise — build still 0 errors,
tests unaffected. This is exploratory/opt-in (`ShimSetHeaderPresenterHost`
defaults off), so there is no regression to the shipping manual header path.

**This session did not change any code** — it only ran the verification step
session 119 asked for and recorded the (negative) result plus root-cause
candidates. The header-presenter swap (B1) remains **not working** and
**not** ready to move to "slice 2" (wiring Auto/Star width to the presenter).

## Investigation continued — root-cause candidate 1 confirmed, narrowed further

Chased candidate 1 to ground with live probes (rebuilding + relaunching Roma
between each change):

1. **Confirmed the timing bug is real.** `IsItemsHost="True"` in the header
   template's XAML sets the shim's `Panel.IsItemsHost` DP at template-expansion
   time, before the panel is attached under the presenter — so
   `OnIsItemsHostChanged`'s one-shot `ParentPresenter`/`GetItemsOwner` call saw
   null and never wired `InternalItemsHost`. Added
   `DataGrid.ShimRetryHeaderItemsHost()` (toggles the panel's `IsItemsHost`
   off/on, then `InvalidateMeasure()`, once the panel is confirmed parented
   after a layout pass) and had the probe call it. This is exploratory
   diagnostic code, gated behind the same `ShimSetHeaderPresenterHost`
   opt-in — no effect on the default render path.
2. **After the retry, wiring is fully correct** — confirmed via reflection in
   the probe: `presenterItems=9` (the presenter's `ItemsSource` — the
   `DataGridColumnHeaderCollection` — has all 9 columns), `internalItemsHostSet
   =True`, `internalItemsHostIsPanel=True` (the exact `DataGridCellsPanel`
   instance is correctly registered as the presenter's items host). So
   candidate 1 (the `OnIsItemsHostChanged` timing gap) is confirmed and now
   worked around.
3. **Yet `realizedHeaders` is still 0, `generated: false`.** With ItemsSource,
   items host, and the generator-identity check all correct, `DataGridCellsPanel`'s
   own (fully upstream, unmodified-for-Uno) `MeasureOverride`/`GenerateChildren`
   — which drives realization directly through
   `IItemContainerGenerator.StartAt`/`GenerateNext`, WPF's classic
   generator-driven panel model — still produces zero realized
   `DataGridColumnHeader` children.

This rules out every wiring-level explanation and points at the same place
session 119's Slice 12 hit and abandoned: **WPF's raw
`ItemContainerGenerator`-driven realization path (`GenerateNext`/`StartAt`
directly driving a panel's children), as opposed to the row path's
purpose-built `VirtualizingRowsRealizer`, does not actually produce working
containers under Uno.** Slice 12 saw an analogous failure from the *row* side
(rows collapsed to border-only height when driven through the generator
instead of the realizer) and was reverted as not worth the effort for a
behavior-neutral refactor; this is the *same* underlying mechanism now blocking
header generation, except here there is no alternative already-built realizer
to fall back to — `DataGridCellsPanel`'s generation code is fully linked
upstream WPF source, not shim code we've replaced.

## Updated conclusion

The header-presenter swap (B1) is blocked on a **real, non-trivial gap**: the
generator-driven `GenerateChildren`/`StartAt`/`GenerateNext` panel-realization
pattern that upstream `DataGridCellsPanel` uses does not work on this shim's
Uno target. Confirming why (does `GenerateNext` return containers that never
get measured/added? does `StartAt` position resolve wrong for a
non-virtualizing items host? is this specific to `IsVirtualizing=false`, the
header's default per the static constructor's
`VirtualizingPanel.IsVirtualizingProperty.OverrideMetadata(false, ...)`?) is
its own investigation, comparable in scope to a full row-virtualization slice,
not a quick follow-up. Recommend either:

- (a) instrument `DataGridCellsPanel.GenerateChildren`/`MeasureOverride`
  directly (temporary logging or a debugger-attached run) to see whether
  `GenerateNext` is even called and what it returns, since the probe can only
  observe before/after state, not what happens inside upstream WPF's own
  measure pass; or
- (b) treat this as confirmation that the generator-driven WPF panel model is
  categorically unsupported on this shim (consistent with the Slice 12
  finding) and stop investing further in the header-presenter swap — revert to
  back-burner status, same as items 3-8 in the session 119 gap survey, since
  `BuildHeaderRow()` already works correctly and nothing user-facing depends on
  this swap.

No regressions: `LeXtudio.Windows` 0 errors; WindowsShims 207 passed / 3 failed
(same pre-existing, confirmed-unrelated failures as session 119's Slice 20 —
`AmbiguousMatchException` in `BindingExpressionBridgeTests`/
`DataGridSelectedCellsTests`/`SelectorSpineTests`); `Roma.Host` 0 errors. All
new code (`ShimRetryHeaderItemsHost`, the probe's retry + reflection
diagnostics) is exploratory/opt-in, reachable only when
`ShimSetHeaderPresenterHost(true)` is called — the shipping manual header path
is untouched.

## Root cause found and fixed — headers now generate correctly

Pursued option (a): temporarily instrumented `DataGridCellsPanel.MeasureOverride`
and the shim's `ItemContainerGenerator.GeneratorSession.GenerateNext` with
`Console.WriteLine` diagnostics (removed again once the fix was confirmed) and
re-ran the probe live. The trace showed:

```text
[CellsPanel] MeasureOverride isHeader=True ... rebuild=True isVirtualizing=True
[GenSession] realized index=0 item=RID container=DataGridColumnHeader
[CellsPanel] DetermineRealizedColumnsBlockList -> 0,19.5      (pass 1: 1 header realized)
[CellsPanel] MeasureOverride isHeader=True ... rebuild=True isVirtualizing=True
[CellsPanel] DetermineRealizedColumnsBlockList -> 241,0       (pass 2: 0 GenerateNext calls)
```

**Root cause:** `VirtualizingPanel.IsVirtualizing` is a WPF property-value-
*inherited* attached DP. `ShimSetRowVirtualization(true)` sets it (true) up the
tree so the row cells panels virtualize columns correctly — but that inherited
value flows down into the pinned header's `DataGridCellsPanel` too, overriding
the type-default-metadata `false` that `DataGridColumnHeadersPresenter`'s
static constructor registers specifically for headers (type-default metadata
only applies where nothing else — inherited or local — provides a value; it
does not shadow an inherited true). With `IsVirtualizing=true`, the header
panel runs `DataGridCellsPanel`'s column-*virtualization* math — keyed off
`ParentDataGrid.HorizontalScrollOffset`/`GetViewportWidth()`, i.e. the rows'
scroll/viewport state — which is meaningless for a non-scrolling pinned
header. The realized set churns between measure passes (1 column, then 0) and
never converges, ending at 0 realized headers.

**Fix:** [DataGrid.cs](../src/LeXtudio.Windows/System.Windows/Controls/DataGrid.cs)
— in `ShimBuildVirtualizedHeader`, right after parenting the presenter, call
`VirtualizingPanel.SetIsVirtualizing(presenter, false)` to set a *local* value
that wins over the inherited one, restoring the header path to the
non-virtualizing (generate-everything) branch it was always meant to use.

**Verified live** (same probe, same assembly/table): `generated: true`,
`realizedHeaders: 9` of `columnCount: 9`, and the trace now shows
`isVirtualizing=False` with all 9 columns generating cleanly on the first
pass and the block list stabilizing (`rebuild=False`) on the second — no more
churn. Re-checked on `MethodDef` (a large, auto-virtualized table) with the
same result. Re-checked `roma.probe.metadata-virtualization` afterward — row
windowing/scrolling still works (`virtualized: true`, window shifts on
scroll) — the header fix doesn't touch the rows' own virtualization state
(`VirtualizingStackPanel`/`DataGridRowsPresenter`), since it only sets the
attached DP locally on the *header* presenter, not the DataGrid or rows host.

**Unrelated observation, not caused by this fix, not chased further this
session:** on this machine's installed .NET 10.0.4 runtime,
`roma.probe.metadata-virtualization` against `TypeDef` reports `total: 5318`
(not the 2400 rows session 119's docs recorded) and `rowHeight: 1` (the
border-collapse symptom session 119 believed it had fixed in Slice 20).
Reproduced this on a **freshly launched app with the header-presenter path
never touched** — confirmed independent of today's change. Given the row
count is also different from session 119's baseline, this is most likely
either a different installed CoreLib/table-schema version on this machine
changing `TypeDef`'s row count and layout characteristics, or a
`net10.0-desktop` SDK-version-specific regression unrelated to the header
work. Flagging for whoever picks up the row-separator/row-height area next —
not re-investigated here since it's outside this session's scope (header
generation, now fixed and confirmed).

Verification commands run this session:

```bash
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop --no-restore   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore  # 207/210 (3 pre-existing, unrelated)
dotnet build ../Roma/src/Roma.Host/Roma.Host.csproj -f net10.0-desktop                       # 0 errors
```

## Slice 2 — Auto/Star column-width computation wired to the presenter path

`OnAutoWidthLayoutUpdated` (the post-layout pass that measures Auto/SizeTo*/
Star columns and applies a uniform per-column width) read `_headerCells`
directly — populated only by `BuildHeaderRow()`, so it silently no-op'd
whenever the header-presenter path was active (`_headerCells.Count == 0`).
Two fixes, both in [DataGrid.cs](../src/LeXtudio.Windows/System.Windows/Controls/DataGrid.cs):

1. **`EffectiveHeaderCells()`** — a new helper that returns `_headerCells`
   for the manual path, or (when `ShimSetHeaderPresenterHost(true)` is
   active) resolves the presenter's realized `DataGridColumnHeader`
   containers via `ColumnHeadersPresenter.ItemContainerGenerator.Containers`,
   matched to `_visibleColumns` **by `Column` reference**, not index — the
   presenter's container-registry order need not match display order once
   column reorder is in play. `OnAutoWidthLayoutUpdated` now reads/writes
   through this helper instead of `_headerCells` directly.
2. **Actually scheduling the pass.** Tracing showed fix #1 alone wasn't
   enough: `_autoWidthPending`/the `LayoutUpdated` hook were only ever
   armed at the end of the *manual* rebuild path in `BuildShimVisualTree` —
   the virtualized branch (`host is DataGridRowsPresenter`) returns before
   reaching that code, so the pass was never scheduled at all when
   virtualization is on (independent of the header-presenter swap — this
   gap exists for row-virtualized grids generally). Extracted the
   scheduling logic into `ScheduleAutoWidthPassIfNeeded()` and call it from
   both the manual path (unchanged behavior) and the virtualized branch.

**Verified live** by temporarily tracing `_visibleColumns` state at each
schedule check: with both fixes, the pass runs and correctly computes
per-column measured widths matching the manual path's content-based values
(e.g. `56,74,74,73,134,67,98` — in the same ballpark as the manual path's
`56,74,74,101,73,112,97,88,109` for the same table, exact figures drift
slightly run-to-run since metadata content/column order isn't perfectly
deterministic across process runs, but the *shape* — varied, content-driven,
not uniform — matches). Confirmed no zero-width columns
(`zeroWidthColumns: 0`) in the final probe snapshot.

**A separate, later effect then dominates the final value under
virtualization**, and is **not** something this slice introduced or can
fix: once row virtualization is active, WPF's own internal column-width
estimation (`DataGridCellsPanel`'s `GetColumnEstimatedMeasureWidthSum`/
`InternalColumns.AverageColumnWidth` machinery, upstream, unmodified code)
re-resolves Auto columns to a **uniform averaged width** — because it
cannot exactly measure off-screen, unrealized rows. This is standard,
expected WPF row-virtualization behavior (Auto-width columns lose exact
per-content sizing under virtualization in real WPF too), confirmed by
tracing the schedule-check log across a probe run: the first 7 calls (before
`ShimSetRowVirtualization(true)`) show correctly varied, content-based
widths; the last 2 calls (after row virtualization takes over) show all
columns collapsed to one shared estimate (`241` for all 9, matching
`averageColumnWidth`-style math seen earlier in this session's header
investigation). This is orthogonal to the header-presenter swap — the same
thing happens for row-virtualized grids using the *manual* header path too,
since it's driven entirely by the rows' own cells panel, not the header's.

Verification:

```bash
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop --no-restore   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore  # 207/210 (3 pre-existing, unrelated)
dotnet build ../Roma/src/Roma.Host/Roma.Host.csproj -f net10.0-desktop                       # 0 errors
```

Live probe (fresh app instance, `roma.probe.metadata-header-presenter` on
`TypeDef`): `generated: true`, `realizedHeaders: 9/9`, `zeroWidthColumns: 0`.
All temporary diagnostic `Console.WriteLine` tracing added during this
investigation (in `DataGrid.ScheduleAutoWidthPassIfNeeded`,
`DataGridCellsPanel.MeasureOverride`, `ItemContainerGenerator.GenerateNext`)
has been removed — only the actual fixes remain.

## Slice 3 — column resize wired to the presenter path

Three more `_headerCells` read/write sites, all resize-related, switched to
`EffectiveHeaderCells()` in [DataGrid.cs](../src/LeXtudio.Windows/System.Windows/Controls/DataGrid.cs):

- `ShimApplyColumnWidth` — applies a resized/auto-width column's new width to
  the header cell, data cells, and filter cell (called from the gripper-drag
  handlers and the Auto/Star pass alike).
- `ShimBestFitColumnWidth` — double-click-to-fit measurement, reads the
  header cell's best-fit width.
- `ShimResizeBaseWidth` — the starting width a gripper drag computes its delta
  from.

Left untouched (confirmed out of scope for resize, each is its own later
slice): `ComputeDropSlot`/`UpdateReorderIndicator` (drag-reorder, still
`_headerCells`-only) and `ShimNotifyColumnHeaders` (the live-style/gridline
notification batch, still `_headerCells`-only). `BuildHeaderRow()`'s own
population of `_headerCells` is correctly left alone — that's the manual
path's construction site, not a consumer.

**Verified live**: added a temporary probe,
`roma.probe.metadata-header-presenter-resize` (enables row virtualization +
header presenter, same sequence as `roma.probe.metadata-header-presenter`,
then drives `ShimTryResizeColumn` on a realized column). Result on `TypeDef`
column 4 ("Name"), delta +60: `resized: true`, `before: 241, after: 301` —
gripper-drag-equivalent resize correctly reaches and mutates the presenter's
realized header. Cross-checked the existing manual-path probe
(`roma.probe.metadata-resize-column`) on a fresh table afterward to confirm
no regression there: `resized: true, before: 74, after: 94`.

Verification:

```bash
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop --no-restore   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore  # 207/210 (3 pre-existing, unrelated)
dotnet build ../Roma/src/Roma.Host/Roma.Host.csproj -f net10.0-desktop                       # 0 errors
```

## Slice 4 — style/gridline notification batch wired to the presenter path

`ShimNotifyColumnHeaders` (session 67's `NotifyPropertyChanged` dispatch —
pushes `GridLinesVisibility`/`CellStyle`/`ColumnHeaderStyle`/
`RowStyleSelector`-style changes, and column `Header`/`Width`/etc. property
changes, onto realized headers) iterated `_headerCells` directly. Switched it
to `EffectiveHeaderCells()` in
[DataGrid.cs](../src/LeXtudio.Windows/System.Windows/Controls/DataGrid.cs) —
same one-line pattern as the slice 2/3 sites. This is the dispatch upstream
`DataGrid.NotifyPropertyChanged`'s `#if HAS_UNO` branch already calls (see
[DataGrid.cs](../ext/wpf/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/DataGrid.cs)
line ~662); real WPF's `#else` branch calls `ColumnHeadersPresenter.NotifyPropertyChanged`
directly instead, which our header-presenter path *could* eventually reuse
since `ColumnHeadersPresenter` is genuinely set now — left as-is for this
slice since the shim dispatch already reaches the right headers.

**Verified live** with a new temporary probe,
`roma.probe.metadata-header-presenter-notify`: enables row virtualization +
header presenter (same sequence as the other B1 probes), then sets
`column.Header = "RenamedHeader"` and checks the realized header's rendered
text. First attempt asserted on `header.Content?.ToString()` directly and
falsely looked like a failure (`"Microsoft.UI.Xaml.Controls.Grid"` both
before and after) — traced this to `DataGridHelper.TransferProperty` (in
[DataGridHelper.cs](../src/LeXtudio.Windows/System.Windows/Controls/DataGridHelper.cs))
rebuilding `Content` as a fresh `Grid` (via `DataGrid.HeaderContent`, a
Grid+TextBlock layout with sort-arrow/text) on *every* `Header`-notify, for
both the manual and presenter paths alike — so `Content` always looks like a
`Grid` and the actual rendered text is a nested `TextBlock.Text`, not the
`Content` reference itself. Fixed the probe to walk into the Grid and read
the nested `TextBlock.Text` instead. Corrected result:
`beforeText: "Name", afterText: "RenamedHeader", propagated: true`.

Verification:

```bash
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop --no-restore   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore  # 207/210 (3 pre-existing, unrelated)
dotnet build ../Roma/src/Roma.Host/Roma.Host.csproj -f net10.0-desktop                       # 0 errors
```

## State after this session

- **B1 arc, header generation (slice 1): done and proven.** Headers render
  via the live `DataGridColumnHeadersPresenter`/`DataGridCellsPanel`, gated
  behind `ShimSetHeaderPresenterHost(true)` (default off).
- **B1 arc, Auto/Star column widths (slice 2): done and proven**, modulo the
  pre-existing, unrelated row-virtualization width-averaging behavior noted
  above.
- **B1 arc, column resize (slice 3): done and proven.**
- **B1 arc, style/gridline notification batch (slice 4): done and proven.**
- **B1 arc, column reorder (slice 5): done and proven** — logical commit
  worked with no changes; interactive drag-gesture plumbing (handler
  attachment + overlay-based drop indicator) implemented and verified this
  session (see below). Only the raw mouse-drag *feel* remains unverified
  (needs a running app, not reachable via DevFlow probes).

## Drag-reorder scoping — narrower than first assessed

Before writing any reorder code, re-examined what "drag-reorder" under the
header-presenter path actually requires, and found it splits into two
genuinely independent pieces — one already works, one doesn't exist yet:

1. **The logical reorder commit** — `ShimTryReorderColumn(column,
   targetDisplayIndex)`: gated on `CanUserReorderColumns`/`column.CanUserReorder`,
   raises `ColumnReordering`, sets `column.DisplayIndex`, calls
   `BuildShimVisualTree()` (a full rebuild), raises `ColumnReordered`. This
   method touches **no** `_headerCells`/panel-hosting state directly — it
   delegates entirely to the existing rebuild path, which (per slices 1-4) is
   already presenter-aware.
2. **The interactive pointer-drag gesture** — `OnHeaderPointerPressed/Moved/
   Released/CaptureLost/Exited` and the `_headerHostPanel`-based drop
   indicator. These handlers are attached to header cells **only inside
   `BuildHeaderRow()`** (`headerCell.PointerPressed += ...`, etc.) — the
   presenter's own upstream `PrepareContainerForItemOverride` has no
   knowledge of this DataGrid-specific wiring, so **presenter-generated
   headers never get these handlers attached at all**, independent of any
   `_headerHostPanel`/indicator-hosting concern.

**Verified #1 live**, since Roma's metadata grids set `CanUserReorderColumns
= false` by design (`ext/ilspy/ILSpy/Metadata/Helpers.cs` — fixed-schema
columns aren't meant to be user-reorderable), a temporary probe
(`roma.probe.metadata-header-presenter-reorder`) force-enabled
`CanUserReorderColumns`/`column.CanUserReorder` (diagnostic-only override, not
a real default change) purely to exercise the mechanism, then called
`ShimTryReorderColumn` directly — bypassing pointer/mouse input entirely,
mirroring how slice 3's resize probe called `ShimTryResizeColumn` directly.
Result moving "Name" (`DisplayIndex 4`) to the front (`DisplayIndex 0`):
`reordered: true`, `beforeOrder: "RID,Token,Offset,Attributes,Name,
Namespace,BaseType,FieldList,MethodList"` →
`afterOrder: "Name,RID,Token,Offset,Attributes,Namespace,BaseType,FieldList,
MethodList"`, `realizedHeadersAfter: 9` (all headers still correctly
generated post-rebuild, in the new order). **No code changes were needed for
this** — the existing rebuild-based commit path already works correctly
under the header-presenter path.

**#2 remains genuinely unimplemented** and is real, scoped work for a future
session: attaching the pointer handlers to presenter-generated headers needs
a hook analogous to `ItemsControlSpine.ShimOnContainerRealized`/
`ShimOnContainerRecycled` (session 119 slice 7's mechanism for rows) but for
`DataGridColumnHeadersPresenter`'s header generation — which currently has no
such hook, since its generation goes through fully-upstream
`PrepareContainerForItemOverride`, not the shim's row-realizer path. Once
handlers can be attached, the drop-indicator hosting question from the
original assessment still applies: inserting `_reorderIndicator` directly
into the presenter's `DataGridCellsPanel.Children` risks the panel's
generator-driven `VirtualizeChildren`/index bookkeeping treating it as a
stray untracked child on the next measure pass (the same fragility class as
session 119's Slice 12 finding) — an overlay positioned independently of the
panel's own children (e.g. a sibling in a restructured header host, rather
than a `Children.Insert` into the generator-managed collection) would be the
safer design, at the cost of restructuring how the header host wraps its
content for both paths.

## Drag-reorder wiring — implemented and verified (item #2 closed)

Implemented both pieces item #2 needed:

1. **Pointer-handler attachment.** Added `#if HAS_UNO` hooks in
   [DataGridColumnHeadersPresenter.cs](../ext/wpf/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/Primitives/DataGridColumnHeadersPresenter.cs)'s
   `PrepareContainerForItemOverride`/`ClearContainerForItemOverride` calling new
   `DataGrid.ShimHookHeaderReorderHandlers`/`ShimUnhookHeaderReorderHandlers`
   ([DataGrid.cs](../src/LeXtudio.Windows/System.Windows/Controls/DataGrid.cs)) —
   attaches/detaches the same `PointerPressed`/`Moved`/`Released`/
   `CaptureLost`/`Exited` handlers `BuildHeaderRow()` already wires manually,
   now also firing for presenter-generated headers as they realize/clear.
2. **Drop-indicator overlay, not a `Children.Insert`.** Confirmed the
   fragility concern was real and avoided it: rather than inserting
   `_reorderIndicator` into the presenter's generator-managed
   `DataGridCellsPanel.Children`, `ShimBuildVirtualizedHeader` now wraps the
   presenter in a small `Grid` (`_headerPresenterOverlay`) that the indicator
   is added to as a plain sibling, positioned by `Margin.Left` (computed from
   `ComputeDropSlot`'s cumulative column-width offset, now returned via an
   `out` parameter) rather than by list order — the trick the manual
   `StackPanel` path relied on implicitly, which doesn't apply to a
   single-cell `Grid` overlay. `EffectiveHeaderReferenceElement()` picks the
   right coordinate-reference `UIElement` (the overlay `Grid` vs. the manual
   `_headerHostPanel`) for `PointerRoutedEventArgs.GetCurrentPoint`, so the
   same `OnHeaderPointer*`/`ComputeDropSlot`/`UpdateReorderIndicator`/
   `EndReorder` code now serves both paths without ever touching the
   presenter's own `Children`.

**Verified live**: `roma.probe.metadata-header-presenter-reorder-indicator`
drives `ComputeDropSlot`/`UpdateReorderIndicator`/`EndReorder` directly via
reflection (DevFlow can't dispatch real pointer input, so this exercises
every piece except the raw `PointerPressed`/`Moved` event delivery itself) —
simulating four drag-move steps then ending the drag:
`realizedBefore: 9, realizedAfter: 9` (headers unaffected by the new overlay
wrapper or by the simulated drag), `overlayChildCountsDuringDrag: [2,2,2,2]`
(presenter + indicator, repositioned via `Margin` each step — never growing,
confirming the indicator isn't re-added on every move), `overlayChildCountAfterEnd: 1`
(indicator cleanly removed, only the presenter remains). This is exactly the
proof the original scoping asked for: the overlay approach does not disturb
the presenter's generator-managed children across a full drag cycle.

## Real-mouse verification attempt — inconclusive, but a useful negative result

DevFlow can dispatch real native pointer input via `cliclick` (`/api/v1/ui/actions/click`
and `/api/v1/ui/actions/drag`, both report `mode: "native"`/`"native-global"`), so this
was attempted rather than left as "needs manual testing." Calibrated screen coordinates
against the running Roma window (via `osascript`'s System Events for window position,
cross-checked against `/api/v1/ui/tree`'s element bounds — confirmed by a data-row click
correctly selecting the exact targeted row), then:

- **Drag test** (`Offset` header → `RID` position, header-presenter path, `CanUserReorderColumns`
  forced on via a new `roma.probe.enable-reorder-on-current-grid` probe): column order
  unchanged after the drag.
- **Control test #1**: a plain click on a column header (sort-click, no drag) — same
  result on **both** the header-presenter path *and* the plain manual `BuildHeaderRow()`
  path — no sort arrow appeared on either.
- **Control test #2**: a plain click on the app's own "File" menu button (nothing to do
  with `DataGrid` at all) — no dropdown flyout appeared either.

Since manual-path sort-click and an unrelated menu button show the **same** non-response
as the presenter-path drag, this rules out a presenter-specific or reorder-specific
regression — real synthetic pointer clicks in this environment do not appear to reliably
trigger WinUI `ButtonBase.Click`/`OnClick`-style interactions here at all, even though the
same click mechanism *does* correctly drive simpler pointer-based interactions (a data-row
click correctly produced row selection/highlight, confirmed repeatedly and reliably).

### Follow-up after a DevFlow update (cliclick-sharp)

Re-ran the same test after the DevFlow agent's native-input path was upgraded to a
proper `cliclick`-backed implementation (`wpf-labs/external/cliclick-sharp` — real
`CGEventCreateMouseEvent`-based down/move/up sequences with easing, not a single
synthetic click). Two things stood out:

- **`/api/v1/ui/actions/click` and `/api/v1/ui/actions/drag` use different coordinate
  scales.** `click` echoes back whatever x/y it's given unchanged and reliably lands
  (confirmed via row selection); `drag` silently **halves** the coordinates it's given
  in its response (`{"fromX":928,"fromY":537}` → echoed back as `{"x":464,"y":268.5}`).
  Doubling the input to `drag` (`1856,1074` → `892,1074`) made it echo back the
  *intended* physical coordinates, confirming `drag` expects raw Retina/2x pixels while
  `click` expects logical points. This is a real, reproducible discrepancy between the
  two endpoints on this machine — worth fixing or documenting on the DevFlow side.
- **Even after correcting for that scale mismatch, the drag still did not commit a
  reorder**, and a header sort-click retried on the same freshly-launched instance
  still produced no visible sort arrow. Row-level interactions (click-to-select)
  continued to work reliably throughout, at both coordinate scales tested.

**Refined conclusion**: this narrows the mystery from "clicks don't work" to
specifically "pointer input aimed at `DataGridColumnHeader`-level elements doesn't
appear to register" — both the upstream `ButtonBase.OnClick`-driven sort and this
session's new `OnHeaderPointerPressed`-driven reorder are equally unreachable via
synthetic input, while `DataGridRow`/`Selector`-level pointer handling works fine at
the same coordinates. This could be a z-order/hit-test issue specific to the header
row (e.g. an overlapping element intercepting pointer events before they reach the
header, such as the horizontal-scroll-sync `ScrollViewer` or the pinned header
`Border` in the virtualized template) rather than purely an input-injection artifact,
but that hypothesis is **not yet investigated** — it would need inspecting the actual
element receiving `PointerPressed` at those coordinates (e.g. temporary logging in
`OnPointerPressed` at the `DataGridColumnHeader`/`Border` level), which wasn't done
this session. The reflection-driven `ComputeDropSlot`/`UpdateReorderIndicator`/
`EndReorder` proof remains the authoritative verification of this session's actual
code changes; real interactive-drag (and, it turns out, real interactive sort-click)
verification is still an open item — and now scoped more precisely than before.

Verification:

```bash
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop --no-restore   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore  # 207/210 (3 pre-existing, unrelated)
dotnet build ../Roma/src/Roma.Host/Roma.Host.csproj -f net10.0-desktop                       # 0 errors
```

### DevFlow coordinate-scale bug — root-caused and fixed (in `wpf-labs`, not this repo)

Traced the `/actions/drag` coordinate-halving to its source in
`wpf-labs/src/DevFlow/LeXtudio.DevFlow.Agent.Core/DevFlowAgentServiceBase.cs`:
`DragRequest.Global` defaulted to `false`, while the sibling `ClickRequest.Global`
and `MoveRequest.Global` both default to `true`. Omitting `"global"` from a request
body (as every test in this session did) therefore sent `drag` down a completely
different code path than `click`/`move`: `UnoAgentService.TryResolveScreenPoint`
treats non-global coordinates as **screenshot pixels** and divides by the display's
rasterization scale (2.0 on this Retina display) to get the screen points CGEvent
needs — exactly reproducing the observed halving. `click`/`move` skip that
conversion entirely when `Global` defaults `true`, taking the input as already being
in the right units. Same omitted field, two different unit systems, purely from an
inconsistent default — not a scaling *algorithm* bug.

**Fix**: changed `DragRequest.Global`'s default from `false` to `true`, matching
`ClickRequest`/`MoveRequest`, with a doc comment explaining why (this file:
`wpf-labs/src/DevFlow/LeXtudio.DevFlow.Agent.Core/DevFlowAgentServiceBase.cs`).

**Verified two ways**:

1. `wpf-labs`'s own `LeXtudio.DevFlow.Agent.Uno.Tests` suite: 22/22 passed after
   the change (no existing test asserted the old default, so this is a
   behavior-widening fix, not a breaking one for anything under test).
2. Live: to test the fix against Roma without touching Roma's own
   `nuget.config`/pinned `0.1.14` package version, checked out the exact
   `v0.1.14` tag into a scratch worktree (`git worktree add`), applied *only*
   this one fix there (so the rebuilt package is otherwise byte-for-byte the
   published API surface — building current `wpf-labs` HEAD directly against
   the older pinned version would have pulled in unrelated source-linking
   refactors and broken the build), built `LeXtudio.DevFlow.Agent.Core`/
   `.Agent.Uno` in Release, and copied the resulting DLLs over the local NuGet
   package cache (`~/.nuget/packages/lextudio.devflow.agent.{core,uno}/0.1.14/lib/...`).
   Rebuilt Roma.Host — 0 errors. Relaunched, and:
   `curl .../ui/actions/drag -d '{"fromX":928,"fromY":537,"toX":446,"toY":537,...}'`
   now echoes back `from: {928,537}, to: {446,537}` — **exactly the input,
   no more halving** — and `mode` reads `"native-global"` (previously plain
   `"native"`), confirming it now takes the same code path as `click`.

**The coordinate bug is fixed and confirmed.** The header-hit-test mystery from
the section above is **independent of it**: with coordinates now provably
correct, the same drag (and a plain header click) still produced no reorder
and no sort arrow, while row-selection continued to work at the same
coordinates. So the next investigation (temporary logging in
`DataGridColumnHeader`/its template `Border`'s pointer handling to see whether
`PointerPressed` reaches the header element at all) is now unblocked by any
coordinate-calibration doubt — whatever's stopping header interaction is a
UI/hit-testing question, not an input-injection one.

Note: the DevFlow fix lives in the `wpf-labs` repo (uncommitted at time of
writing — `src/DevFlow/LeXtudio.DevFlow.Agent.Core/DevFlowAgentServiceBase.cs`),
not in `WindowsShims`/`Roma`, so it isn't part of the B1 arc's diff. The
NuGet-cache binary patch used to validate it live is a local, session-scoped
workaround — production consumption still requires a real
`LeXtudio.DevFlow.Agent.Core`/`.Agent.Uno` NuGet release once the fix is
committed and reviewed there.

## Header hit-testing mystery — root-caused (not a DataGrid bug)

Went back to the open question with temporary diagnostics: added `Console.WriteLine`
to `ButtonBase.OnPointerPressed`/`OnPointerReleased` (this shim) and
`DataGridColumnHeader.OnClick` (upstream, `#if`-free — removed again after) to see
directly whether pointer input reaches the header element at all.

**Fixed one real bug along the way.** Traced why `Template` might never reach
presenter-generated headers: `ApplyShimGridLines()` — the only place that assigned
`Template` on `DataGridColumnHeader` — was called exclusively from the manual
`BuildHeaderRow()` path. Presenter-generated headers (`new DataGridColumnHeader()`
from `DataGridColumnHeadersPresenter.GetContainerForItemOverride`) never got a
`Template` at all: no `Border`, no gripper `Thumb`s, nothing but whatever WinUI's
bare `Control` chrome provides by default. Fixed by calling `ApplyShimGridLines()`
from the shared upstream `PrepareColumnHeader` (`#if HAS_UNO`, called by both
paths) instead, and hardened it to always give the header an explicit `Background`
(WinUI does not hit-test a `Border`/`Control` with a null `Background` — the
template's two gripper `Thumb`s already worked around this locally with
`Background='Transparent'`; the header's own outer `Border` didn't). This is a
real, independent fix — worth keeping regardless of what caused the click mystery.

**That fix did not explain the mystery, though.** With diagnostics in place:

- On a **fresh app launch**, the *first* click of any kind on the header produced
  **no** `OnPointerPressed` log at all — not a DataGrid issue, not specific to
  `DataGridColumnHeader`: a real native `Microsoft.UI.Xaml.Controls.Button` (the
  header's own filter-icon button) at the same coordinates showed the identical
  silent non-response.
- Doing **any other click first** (e.g. selecting a data row) — then retrying the
  header click — made `OnPointerPressed` fire correctly on the header. This is
  consistent with the well-known macOS behavior where the first click on an
  inactive/background window only activates it and isn't delivered as a "real"
  click to the content beneath; every earlier test in this investigation happened
  to be the first interaction after a fresh launch, which is why it looked like
  headers specifically were unreachable.
- With that ruled out, `OnPointerPressed` **and** `OnPointerReleased` both fired
  correctly on the header (confirmed via log), yet `OnClick` still **never** fired.
  Inspecting `_isPressed` at the top of `OnPointerReleased` showed it was `false`
  — meaning `pt.Properties.IsLeftButtonPressed` read `false` inside
  `OnPointerPressed` too, so the shim's own press-tracking (and, almost certainly,
  WinUI's internal native `ButtonBase` click state machine, which likely gates on
  the same `PointerPoint` property) never armed in the first place.

**Conclusion**: the synthetic `cliclick`/CGEvent-injected pointer input reaches
Uno's Skia/macOS pointer pipeline and is delivered to the correct element
(`OnPointerPressed`/`OnPointerReleased` both fire on the right control at the
right coordinates), but the delivered `PointerPoint` does not report
`IsLeftButtonPressed = true` the way a real mouse/trackpad event does. This is
an Uno-platform/CGEvent-flag-interpretation gap (or a flag `cliclick-sharp` isn't
setting on its synthesized `CGEventCreateMouseEvent`), not a `DataGrid`,
`DataGridColumnHeader`, or presenter-path bug — WPF's real `ButtonBase.Click`
semantics depend on that property being true during the press, and no header-side
code change can fix a false pointer-button-state flag arriving from outside the
app. This is well outside the B1 arc's scope (and outside `WindowsShims`/`Roma`
entirely) — it would need to be chased in Uno's own pointer/Skia-macOS backend or
in how `cliclick`/CGEvent constructs the synthetic mouse-down event.

All temporary diagnostic logging has been removed; only the `ApplyShimGridLines`/
`PrepareColumnHeader` template-application fix remains.

Verification:

```bash
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop --no-restore   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore  # 207/210 (3 pre-existing, unrelated)
dotnet build ../Roma/src/Roma.Host/Roma.Host.csproj -f net10.0-desktop                       # 0 errors
```

## B1 arc — conclusion

All items from session 119's original B1 scope are now implemented and
proven under `ShimSetHeaderPresenterHost(true)` (default off, zero
regression to the shipping manual header):

1. Header generation (slice 1)
2. Auto/Star column widths (slice 2)
3. Column resize (slice 3)
4. Style/gridline notification batch (slice 4)
5. Column reorder — logical commit (already worked) + interactive drag
   plumbing (this slice)

Remaining before this could become the shipping default: manual/screenshot
verification of the interactive drag *feel* (item 5's caveat above), and a
decision on whether to actually flip `ShimSetHeaderPresenterHost`/
`ShimSetRowVirtualization` on by default now that the header side is at
parity with the manual path.

## Recommended next steps (superseded by later findings in this document — see below)

*(Historical note: this section originally proposed investigating a suspected
z-order/hit-test issue for the header click mystery. That was chased further
this session — see "Header hit-testing mystery — root-caused" above — and
ruled out; the real cause is an Uno Platform macOS input-bridge issue, written
up separately in [uno-macos-synthetic-click-issue.md](uno-macos-synthetic-click-issue.md)
for upstream reporting. The DevFlow `drag` vs `click` coordinate-scale
mismatch mentioned here was also root-caused and fixed — see "DevFlow
coordinate-scale bug" above. Kept this section for the session's chronological
record rather than deleting it.)*

Remaining open items, consolidated:

1. **Uno Platform bug** (macOS 15+, synthetic clicks don't set
   `IsLeftButtonPressed`) — written up in
   [uno-macos-synthetic-click-issue.md](uno-macos-synthetic-click-issue.md),
   ready to file against `unoplatform/uno`. Blocks real interactive-drag/click
   verification for any `ButtonBase`-derived control via automated testing on
   this platform, independent of DataGrid/B1.
2. **DevFlow fix not yet committed** — the `DragRequest.Global` default fix
   lives uncommitted in the `wpf-labs` working tree
   (`src/DevFlow/LeXtudio.DevFlow.Agent.Core/DevFlowAgentServiceBase.cs`);
   needs a real commit/review/release there before it's available outside this
   session's local NuGet-cache patch.
3. **`rowHeight: 1` observation** (flagged earlier in this document) should be
   checked on a clean `master` checkout to confirm it predates this session's
   changes before anyone spends time on it — unrelated to the B1 arc.
4. **B1 arc default-on decision** — per the "B1 arc — conclusion" section
   above: whether to flip `ShimSetHeaderPresenterHost`/`ShimSetRowVirtualization`
   on by default now that the header side is at parity with the manual path,
   pending real interactive-drag verification (blocked on item 1).

## Floating drag-header visual (gap-survey item 6, closed)

Implemented the "column drag-reorder floating header" cosmetic gap flagged in
session 119's gap survey (item 6: linked upstream
`DataGridColumnFloatingHeader`/`DataGridColumnDropSeparator` compile but the
live header used a plain opacity-dim instead of an actual floating ghost that
follows the pointer).

**Did not reuse the linked upstream `DataGridColumnFloatingHeader` class** —
it paints a live `VisualBrush` snapshot of the source header
(`new VisualBrush(_referenceHeader)`), and WinUI/Uno has no equivalent brush
type. Reimplemented the visual effect instead:

- Added `PART_ShimDragOverlay`, a hit-test-invisible `Canvas` sibling in both
  root templates (`ShimTemplateXaml` and `ShimVirtualizedTemplateXaml`,
  `Grid.RowSpan="2"` in the latter so it can host the floating header over
  either the pinned header row or the scrolling rows area) — a stable,
  absolutely-positioned overlay independent of whichever panel
  (`_headerHostPanel`/`_headerPresenterOverlay`) currently hosts the realized
  header cells.
- `StartFloatingHeader`/`UpdateFloatingHeader`/`EndFloatingHeader`: on drag
  start, builds a lightweight clone (a `Border` + centered `TextBlock` showing
  the column's header text) positioned via `TransformToVisual`/`Canvas.SetLeft`
  at the real header's location, then repositioned by pointer delta-X on each
  move (`Canvas.SetLeft(startLeft + deltaX)`), removed on drag end. Wired into
  the existing `OnHeaderPointerMoved`/`EndReorder` drag-reorder handlers from
  earlier this session.

**Hit — and fixed — a third distinct Uno-Skia measurement quirk this
session.** The first two implementation attempts rendered nothing visible:

1. First attempt (`Border` with explicit `BorderThickness` + `Width`/`Height`
   set from `header.ActualWidth`/`ActualHeight`, `Child` = a fresh
   `HeaderContent(column)` clone): collapsed to `73x2` (exactly the sum of the
   top+bottom border thickness) — the same "explicit `BorderThickness` on an
   element measured with an unconstrained parent collapses to border-only
   size" quirk session 119 root-caused for the row separator (there: a
   `VirtualizingStackPanel`'s infinite-width constraint; here: a `Canvas`,
   which always measures children with infinite available size in both
   dimensions).
2. Second attempt (dropped `BorderThickness`, kept explicit `Height`): still
   collapsed, this time to `73x0`. Traced further via a temporary probe
   reading `header.ActualWidth/ActualHeight/DesiredSize/RenderSize` directly:
   **all of the header's own height-related properties read `(0, 0)`** at the
   moment `StartFloatingHeader` runs — `DesiredSize`/`RenderSize` `(0,0)` and
   even `ActualHeight` `0` — despite the header visibly rendering at its real
   ~20px height on screen (confirmed via `/api/v1/ui/tree`'s independently-
   sourced bounds and the screenshot). Only `ActualWidth` read a sane value
   (`73`), because `BuildHeaderRow()` only ever sets `Width` explicitly on
   headers, never `Height` — so this reads as a live/rendered-vs-CLR-property
   desync specific to `Height`-related layout properties on this Uno/Skia
   target, not something fixable from `DataGrid`-level code (a fourth
   platform-level oddity noted here for awareness, not chased further — this
   session already has one written up in
   [uno-macos-synthetic-click-issue.md](uno-macos-synthetic-click-issue.md);
   didn't want to double the scope chasing a second one for a cosmetic
   feature).
3. **Fix**: stopped depending on the header's own (unreliable) height read
   entirely. Left the floating `Border`'s `Height` unset (`Auto`) and let it
   size vertically from its own `TextBlock` content + `Padding` — a
   `TextBlock` reliably self-sizes from rendered text regardless of the
   incoming measure constraint, sidestepping the whole class of quirk. Kept
   `Width = header.ActualWidth` (that one reads correctly, per above).

**Verified live**, three ways:

1. Structural, reflection-driven (`roma.probe.metadata-reorder-floating-header`,
   bypassing pointer input): `overlayFoundBefore: true`, `childCountAfterStart: 1`
   (clone added), `leftAtStart: 314.0` → `leftAfterMove: 364.0` after an
   `UpdateFloatingHeader(50.0)` call (`movedBy: 50.0`, exact), `childCountAfterEnd: 0`
   (cleanly removed), `headerStillRealized: 8` (real headers unaffected). Confirmed
   working identically on the header-presenter path too (`TypeDef`, 9 headers,
   `movedBy: 50.0`).
2. Visual: a probe variant (`roma.probe.metadata-reorder-floating-header-show`)
   that starts the float and leaves it visible produced `floatingBounds: "73x23.5"`
   (real, nonzero size) and a screenshot showing a light-gray "Name" ghost header
   rendered over the target drop position, offset left by the requested delta —
   the actual intended visual effect, confirmed by eye.
3. Regression: build 0 errors, WindowsShims 207/210 (same 3 pre-existing,
   unrelated failures), Roma.Host 0 errors.

**Not verified**: the real interactive mouse-drag feel (does it track the
cursor smoothly during an actual drag gesture) — blocked on the same Uno
Platform macOS input-bridge bug (item 1 above) that blocks all real-pointer
verification this session, not on anything specific to this feature.

Verification:

```bash
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop --no-restore   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore  # 207/210 (3 pre-existing, unrelated)
dotnet build ../Roma/src/Roma.Host/Roma.Host.csproj -f net10.0-desktop                       # 0 errors
```
