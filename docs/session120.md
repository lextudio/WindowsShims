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
- **Still not wired to the presenter path**: drag-reorder. This is the last
  remaining item from session 119's original B1 scope.

## Recommended next step

Drag-reorder (`ComputeDropSlot`/`UpdateReorderIndicator`/the floating-header
drag visuals) is the last remaining B1 item, and unlike slices 2-4 it is
**not** a simple list-source swap: it reads/writes `_headerHostPanel`, a
manual-path-only reorderable panel that **doesn't exist at all** in the
virtualized template (the pinned header host there is a plain `Border`
holding either `BuildHeaderRow()`'s panel or the presenter — there's no
reorder-capable panel wrapper either way once the presenter is hosted).
Wiring this needs a design decision first — e.g. whether the presenter's own
`DataGridCellsPanel` (which upstream WPF already drives reorder visuals
through, per session 119's B1 investigation) can host the drop-indicator
directly, or whether `_headerHostPanel`-equivalent scaffolding needs to be
added to the virtualized template — before any code changes, not a
continuation of the slice 2-4 pattern. Separately, the `rowHeight: 1`
observation flagged earlier in this document should be checked on a clean
`master` checkout to confirm it predates this session's changes before anyone
spends time on it — it is unrelated to the B1 arc.
