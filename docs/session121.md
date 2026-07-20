# Session 121

Date: 2026-07-20

Re-investigation and closure of session 119's Slice 12 (row realization via
WPF's raw `ItemContainerGenerator.GenerateNext`/`StartAt`, reverted at the
time without a confirmed root cause). Triggered by session 120's B1 arc,
which found and fixed a superficially similar generator-driven realization
failure on the header side (`IsVirtualizing` property-value-inheritance
shadowing) and raised the question of whether the same fix, or the same
underlying mechanism, applied to rows.

## Setup

Created `exp/slice12-generator-direct`, branched from `master` (not from a
pre-virtualization commit — an earlier attempt at branching before Slice 2's
`VirtualizingRowsRealizer` landed was abandoned once it became clear that
commit also predates `GetItemsOwner`'s real implementation, `DataGrid`'s
`ShimOnContainerRealized`/`ShimOnContainerRecycled` hooks, and the rest of
the row-hosting infrastructure Slice 12 actually depends on — branching there
would have conflated "missing infrastructure" with "generator vs realizer,"
which is the one variable this investigation needed to isolate).

Implemented `VirtualizingStackPanel.ShimUseGeneratorDirect` (default off): an
alternate `MeasureOverride`/`ArrangeOverride` path that drives realization
through `owner.ItemContainerGenerator.StartAt`/`GenerateNext` directly,
mirroring real WPF's panel↔generator contract (the panel, not the generator,
adds newly-realized containers to `InternalChildren` and calls
`Remove()`/`GeneratorPositionFromIndex` for containers scrolling out of the
window), instead of `VirtualizingRowsRealizer`. Added temporary
`Console.WriteLine` diagnostics in this path and a temporary DevFlow probe in
Roma (`roma.probe.metadata-virtualization-generator-direct`, not committed —
lived only in the Roma working tree for the duration of this session) that
flips the flag via reflection and drives the same before/after-scroll check
`roma.probe.metadata-virtualization` already does.

## Root cause #1 — confirmed: double container-list registration

Live run against `TypeDef` (5318 rows) reproduced session 119's exact
symptom: `rowHeight: 1`. The instrumented log made the mechanism visible
directly:

```text
[GenDirect] Realized index=0 container=DataGridRow
[GenDirect] Measured index=0 desired=784x1
[GenDirect] Measured index=1 desired=784x1        <- no "Realized" line
[GenDirect] Realized index=2 container=DataGridRow
[GenDirect] Measured index=2 desired=784x1
[GenDirect] Measured index=3 desired=784x1        <- no "Realized" line
```

`Realized` (i.e. `isNewlyRealized == true`) fires only on even indices;
`Measured` fires on every index. Tracing why: `DataGrid.ShimOnContainerRealized`
— the hook the new realize step calls after every fresh container, mirroring
what the working realizer path already calls — invokes
`ItemContainerGenerator.RegisterContainer(item, row)`, which unconditionally
appends to the generator's own `_containers`/`_items` lists. But
`GeneratorSession.GenerateNext`'s own container-creation branch **also**
appends to those same lists directly when it creates a container. Every
freshly-generated container is therefore registered twice, and each
double-append shifts the physical list one slot further ahead of the logical
item index it's supposed to represent. The next `GenerateNext` call sees
`_index < _containers.Count` (now advanced by 2, not 1) and treats the
duplicate leftover entry as an already-realized container for the *next*
index — so every other "row" is actually a stale relabeled duplicate of the
previous item's container, never receiving its own real content/measurement,
collapsing the whole window to a degenerate uniform ~1px height. This isn't
an Uno/Skia measurement quirk of any kind — it's a plain shim-side
double-bookkeeping bug in how two independent, both-necessary code paths
(`GenerateNext`'s WPF-faithful insert logic and `ShimOnContainerRealized`'s
registration, which the realizer path also depends on for
`ContainerFromItem`/`IndexFromContainer` to work) write to the same list.

## Root cause #2 — confirmed: the generator has no sparse/arbitrary-start model

Independently, forcing the viewport to scroll (window start jumping to index
2657, a range the physical container list had nowhere near accumulated)
threw live:

```text
[GenDirect] GenerateNext threw at index=2657: ArgumentOutOfRangeException: Index must be within the bounds of the List. (Parameter 'index')
```

`GeneratorSession.GenerateNext`'s creation branch does
`_containers.Insert(_index, container)` when `_index != _containers.Count` —
a model that assumes the list is always filled contiguously from index 0
(true for the full/manual render path this generator was originally built
for, session 27). A virtualized window that starts mid-list with nothing
realized below it has no way to satisfy that precondition; the shim's
`ItemContainerGenerator` would need reworking to a sparse, arbitrary-position
model (e.g. `Dictionary<int, container>` instead of a dense `List<T>`) before
it could support this at all — independent of, and in addition to, fixing
root cause #1.

## Conclusion — Slice 12 closed

The raw `ItemContainerGenerator` cannot be reused for row virtualization as
implemented, for two independent, now-confirmed reasons, neither of which is
an Uno-platform limitation:

1. `GenerateNext` and `ShimOnContainerRealized`/`RegisterContainer` both
   write to the same container list, silently double-registering every
   fresh realization.
2. The container list has no model for a sparse/arbitrary-start window —
   only for contiguous fill from index 0.

Both are fixable in principle, but doing so is not a quick unlock: it's
comparable in scope to writing a new realizer (rework the generator to be
window-aware, then remove the redundant registration path without breaking
the full/manual render path that still depends on `RegisterContainer`'s
current append-only semantics). `VirtualizingRowsRealizer`'s design —
a separate `Dictionary<int, TContainer>` as the sole index authority,
deliberately decoupled from the generator's own bookkeeping — sidesteps both
problems by construction. Session 119's original call to keep the realizer
and not chase generator unification is confirmed correct; no further work is
planned on this path.

Experiment code is committed on `exp/slice12-generator-direct` (not intended
to merge) for reference. The temporary Roma probe was removed from Roma's
working tree — only WindowsShims' own uncommitted state (unrelated to this
session) remains there.

## Aside: how much upstream WPF code the realizer path already reuses

A natural follow-up question: given the realizer (not the generator) is the
permanent design, how much of the *rest* of WPF's DataGrid row machinery is
still linked/reused as-is, versus reimplemented? Per
`LeXtudio.Windows.csproj`, the row-rendering chain is almost entirely
upstream, unmodified-for-Uno WPF source:

- `DataGridRow.cs` — linked (`ext/wpf/.../Controls/DataGridRow.cs`)
- `DataGridCell.cs` — linked (`ext/wpf/.../Controls/DataGridCell.cs`)
- `DataGridCellsPanel.cs` — linked (`ext/wpf/.../Controls/DataGridCellsPanel.cs`)
- `Primitives/DataGridRowsPresenter.cs` — linked (`ext/wpf/.../Controls/Primitives/DataGridRowsPresenter.cs`)

`VirtualizingRowsRealizer`/`VirtualizingRowsLayout` (shim-only) sit entirely
*underneath* this chain: they decide *which* items get a container and *when*
containers are created/recycled, but the containers themselves
(`DataGridRow`), their per-row cell layout (`DataGridCellsPanel`), and the
presenter wrapping them (`DataGridRowsPresenter`) are the same upstream WPF
classes cell/row editing, styling, and selection already rely on elsewhere
in the shim — none of that had to be reimplemented to add virtualization.
The realizer's `create`/`prepare`/`clear` callbacks are the *only* new
surface: they call straight into `owner.CreateContainerForItem`/
`PrepareContainerForItem`/`ClearContainerForItem` (the same entry points the
non-virtualized manual path uses), so row-level behavior (templates, styles,
`DataGridCellsPanel`'s own per-cell measure/arrange) is identical between
virtualized and non-virtualized rendering by construction, not by
parallel reimplementation. The generator-direct experiment did not change
any of this — its failure was confined entirely to the
realize/recycle-*bookkeeping* layer (which item↔container mapping is active
right now), not to anything in the linked row/cell rendering classes
themselves.

## Grouping, Slice 1 — real data-level grouping (CollectionViewGroup, ItemCollection)

Starts closing the grouping gap flagged earlier this session (grouping was
previously all inert stubs: `IsGrouping` hardcoded `false`,
`MS.Internal.Data.CollectionViewGroupInternal` a bare stub with no real
tree). This slice is the data model only — building a correct group tree and
reordering `Items` into group-contiguous order — not the visual
`GroupItem`/row-group-header rendering (a later slice; see below for why it
doesn't reuse a WPF `DataGridRowGroupHeader` class, because none exists
upstream).

**`System.Windows.Data.CollectionViewGroup`** (`CollectionViewShims.cs`) —
new, real abstract base matching upstream's shape: `Name`, `Items` (a
`ReadOnlyObservableCollection<object?>` backed by a protected
`ObservableCollection<object?>`), `ItemCount`, `abstract bool IsBottomLevel`.

**`MS.Internal.Data.CollectionViewGroupInternal`** — real (not stub)
implementation. Bottom-level groups hold leaf items directly in `Items`;
higher levels hold nested `CollectionViewGroupInternal` instances in that
same `Items` property (matching upstream, so recursive-`ItemCount`-style code
elsewhere doesn't need a different shape per level). Added
`LeafIndexFromItem` (recursive, mirrors upstream's flat-index lookup) for
future use once row realization needs to map a group-relative row back to an
absolute index.

**`CollectionViewGroupBuilder`** — new. `BuildGroups(items, groupDescriptions)`
partitions a flat sequence level-by-level (one `GroupDescription` per level),
preserving first-encounter order (WPF does not alphabetize group names —
group order follows the order groups are first seen in the source, so
grouping composes correctly with a prior sort: sort first, group second, and
the group order follows the sorted sequence). `FlattenLeaves` walks the tree
back to a flat leaf sequence in group order.

**`ItemCollection.Refresh()`** — wired in: after existing filter/sort
processing, if `GroupDescriptions.Count > 0`, builds the group tree from the
sorted+filtered `view`, stores it in a new internal `Groups` property, and
**replaces `view` with the flattened-by-group order** — meaning enabling
grouping actually reorders the flat item sequence into group-contiguous
blocks, exactly like real WPF (items sharing a group become adjacent even if
sort had them interleaved). Clearing `GroupDescriptions` and calling
`Refresh()` restores the pre-grouping flat order and empties `Groups`.
`GroupDescriptions`' `CollectionChanged` now sets `NeedsRefresh = true`
(mirroring the existing `SortDescriptions` hookup), and `ReplaceAll()`'s
"does this need a real `Refresh()` or just a plain Reset" check now includes
`GroupDescriptions.Count > 0`.

One bug caught by the test suite, not by manual reasoning: `Refresh()`'s
early-return fast path (skip all work when there's nothing to apply) didn't
account for *transitioning out* of grouping — clearing `GroupDescriptions`
then calling `Refresh()` has `hasSort`/`hasFilter`/`hasGroup` all false and
`_unfilteredSource is null`, so the old condition returned before ever
reaching the code that would reset `Groups` to empty, leaving stale group
data behind. Fixed by adding `&& Groups.Count == 0` to the fast-path guard.

**`ItemsControl.IsGrouping`** (`ItemsControlSpine.cs`) — was hardcoded
`false`; now `Items.GroupDescriptions.Count > 0`. Low risk: nothing in the
codebase currently sets `GroupDescriptions` on any live `ItemsControl`
(confirmed by this session's earlier survey), so this flips no visible
behavior yet — it only takes effect once a later slice's container
generation starts populating `GroupDescriptions` on purpose to exercise it.

**Tests** — five new cases in `ItemCollectionViewTests.cs`:
`GroupDescriptionsAreLinkedAndObservable`,
`SingleLevelGroupingBucketsByFirstEncounterOrder`,
`MultiLevelGroupingNestsSubgroups`,
`ClearingGroupDescriptionsRestoresFlatOrderOnRefresh`,
`GroupingComposesWithSortDescriptions`.

**Verification**:

```bash
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop --no-restore   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore  # 212/215 (3 pre-existing, unrelated AmbiguousMatchException failures)
dotnet build ../Roma/src/Roma.Host/Roma.Host.csproj -f net10.0-desktop                       # 0 errors
```

## Grouping — why there's no `DataGridRowGroupHeader` to port

Before writing any of the above, surveyed `ext/wpf` for a DataGrid-specific
group-header class to link, the way `DataGridRow`/`DataGridCell`/
`DataGridCellsPanel` are linked for rows. **None exists.** Real WPF's
`DataGrid` grouping reuses the generic `ItemsControl` grouping machinery
(`GroupItem : ContentControl`, `GroupStyle` for
`ContainerStyle`/`HeaderTemplate`/`Panel`) — `GroupItem.Content` is set
directly to a `CollectionViewGroup` instance during container preparation
(`GroupItem.cs:192`, `this.Content = item;`), not via a `DataContext`
binding. `CollectionViewGroupRoot`/`CollectionViewGroupInternal` (the real
grouping engine) live in `MS.Internal.Data`, not in `Controls`.

**Implication for the next slice**: grouping's visual piece isn't "port
`DataGridRowGroupHeader`" (there is nothing to port) — it's extending
`GroupItem`/`GroupStyle` container generation (which routes through
`ItemContainerGenerator`, itself already reworked once this session — see
the Slice 12 closure above) to interleave `GroupItem` containers with
`DataGridRow` containers in whatever realizer drives row virtualization.
Given this session's Slice 12 finding, that realizer will almost certainly
need to be `VirtualizingRowsRealizer`-shaped (a dedicated index-authority
structure, not the raw generator) extended to understand two container
kinds (group header vs. row) and group-relative-to-flat index translation,
rather than anything ported wholesale from upstream.

## Grouping, Slice 2 — visual rendering on the manual (non-virtualized) path

Renders `GroupItem` headers interleaved with `DataGridRow`s for the manual
`BuildShimVisualTree`/`RefreshFilteredRows` render path. Deliberately scoped
to the manual path only — the virtualized `DataGridRowsPresenter` path is a
separate later slice, since (per Slice 12's finding above) it needs realizer
changes, not just a rendering tweak.

**`GroupItem`** (`GroupItem.cs`) — was a 3-line empty shell; added
`ShimPrepareGroupHeader(CollectionViewGroupInternal group, int depth)`,
which sets `Content` to `"{name} ({count})"`, indents via `Padding` scaled by
`depth` (nesting level), and applies a light background + semibold weight so
it reads visibly as a header row. `GroupStyle` (WPF's real
`ContainerStyle`/`HeaderTemplate`/`Panel` customization point) is still not
shimmed — this is a fixed, non-user-templated header, matching Slice 1's
scoping.

**`DataGrid.cs`** — extracted the row-building loop (previously duplicated
verbatim in `BuildShimVisualTree` and `RefreshFilteredRows`) into a shared
`BuildRowsOrGroups(host)`. When `IsGrouping` is false, this is exactly the
old flat per-item `DataGridRow` loop (behavior-neutral for every existing,
non-grouped grid). When `IsGrouping` is true, it instead calls
`BuildGroupedRows`, which recurses `Items.Groups`: for each group, adds a
`GroupItem` header, then either (bottom level) a `DataGridRow` per leaf item,
or (higher level) recurses into subgroups at `depth + 1`. Row indices
(`ShimDecorateRow`'s alternating-row counter) only advance for actual data
rows, not headers, matching WPF (group headers don't consume a row-index
slot).

**Live verification** (temporary reflection-driven live check via a new,
permanent DevFlow probe `roma.probe.metadata-grouping` — kept, not removed,
since grouping is now real supported behavior rather than an abandoned
experiment, matching how the existing `roma.probe.metadata-virtualization*`
probes are kept as living regression checks): grouped the `TypeRef` table
(461 rows total) by `Namespace` —

```json
{"total":461,"groupHeaderCount":51,"dataRowCount":461,
 "firstGroupHeaderText":"System.Runtime.CompilerServices (29)",
 "groupHeaderTexts":["System.Runtime.CompilerServices (29)","System.Diagnostics (11)",
   " (12)","System.Reflection (16)","System.Runtime.Versioning (1)",
   "System.Security.Permissions (2)","System.Security (1)","System (99)",
   "Windows.Foundation (29)","System.Threading.Tasks (6)"]}
```

All 461 leaf rows still rendered (`dataRowCount == total`, confirming
grouping doesn't drop or duplicate items), 51 group headers rendered with
correct per-group counts and names read directly off the live `GroupItem`
containers (not just the data model — this exercises the actual
`BuildShimVisualTree` → `GroupItem.Content` path). Also spot-checked the
trivial single-row `Module` table (`total:1, groupHeaderCount:1,
dataRowCount:1`) to confirm the one-group edge case renders correctly too.

**Verification**:

```bash
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop --no-restore   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore  # 212/215 (3 pre-existing, unrelated)
dotnet build ../Roma/src/Roma.Host/Roma.Host.csproj -f net10.0-desktop                       # 0 errors
```

## Grouping, Slice 3 — virtualized-path grouping, via a reuse-first design

Extends grouping to the virtualized `DataGridRowsPresenter`/
`VirtualizingStackPanel` path. Before writing any new realization logic,
looked for what could be reused rather than hand-rolling a parallel
mechanism (the user's explicit ask this session) — and found more to reuse
than expected:

**Reuse discovery: upstream `DataGrid`'s real container-generation overrides
are already linked and active.** `LeXtudio.Windows.csproj:481` links
`ext/wpf/.../DataGrid.cs` (the actual upstream WPF source, not just the
shim's own supplementary `src/.../DataGrid.cs` partial). That file's
`GetContainerForItemOverride()`/`IsItemItsOwnContainerOverride()`/
`PrepareContainerForItemOverride()` (lines 822–864) are real, unmodified WPF
code — `owner.CreateContainerForItem(item)` in `VirtualizingStackPanel`'s
existing (non-grouped) realizer callbacks was already routing through this
real upstream code to produce `DataGridRow` instances, not a shim
reimplementation. This had gone unremarked in earlier sessions' framing
("DataGrid doesn't override these") — that framing was accurate only for a
much earlier commit (pre-Slice-12, before this override existed in the
linked file) and had gone stale. Confirming this changes the entire Slice 3
design: the leaf-row side of grouped virtualization needs **zero new
container-creation code** — it can call the exact same
`owner.CreateContainerForItem`/`PrepareContainerForItem`/`ClearContainerForItem`
the non-grouped path already calls.

**Design: flatten, don't fork.** Rather than teaching
`VirtualizingRowsRealizer<TContainer>` a new concept of "two container
kinds," `CollectionViewGroupBuilder.FlattenWithHeaders(groups)`
(`CollectionViewShims.cs`) produces a single flat `List<object?>` — group
headers and leaf items interleaved in display order, headers wrapped in a
new marker type `MS.Internal.Data.GroupHeaderSlot(Group, Depth)` so they're
never confused with a real data item. This is the exact same shape
`VirtualizingRowsRealizer` already virtualizes over for the non-grouped
case (`itemAt: index => owner.Items[index]`) — grouped mode just swaps in
`itemAt: index => EnsureGroupedSlots(owner)[index]` and leaves the realizer
class itself **completely unmodified**. No new windowing, recycling-pool, or
layout-math code was written for this slice; `VirtualizingRowsLayout.Compute`
and `VirtualizingRowsRealizer.Realize` — the exact machinery already proven
correct (and, per Slice 12 above, proven fragile when reimplemented
differently) — do 100% of the index-window/recycle bookkeeping for grouped
grids too.

**What's actually new** (`VirtualizingPanelStubs.cs`):

- `EnsureGroupedSlots(owner)` — caches `FlattenWithHeaders(owner.Items.Groups)`,
  invalidated by the existing `ShimResetRealization()` (already called after
  any items/filter/sort/group change).
- `EnsureRealizer` now branches on `owner.IsGrouping`, building a grouped
  variant of the *same* three callbacks (`create`/`prepare`/`clear`) where:
  - `create`: `new GroupItem()` for a `GroupHeaderSlot`, else the unchanged
    `owner.CreateContainerForItem(slot)` call.
  - `prepare`: `GroupItem.ShimPrepareGroupHeader(...)` (Slice 2's method,
    reused as-is) for headers, else the unchanged
    `owner.PrepareContainerForItem`/`ShimOnContainerRealized` pair.
  - `clear`: skips the row-specific `ShimOnContainerRecycled`/
    `ClearContainerForItem` calls for header slots (headers have no
    matching per-item decoration to undo), otherwise unchanged.
- `MeasureOverride`'s `itemCount` becomes
  `EnsureGroupedSlots(owner).Count` (leaves + headers) instead of
  `owner.Items.Count` when grouping — this is also why `extent` in the live
  verification below equals `total + groupCount`, not just `total`.
- **One deliberate new restriction**: grouped grids never use
  `VirtualizationMode.Recycling` (`recycling = Recycling && !isGrouped`).
  Recycling reuses container *instances* across different realized indices;
  since a `GroupItem` and a `DataGridRow` are different concrete types, a
  container recycled from a header slot into a data slot (or vice versa)
  would need type-switching logic the realizer's recycle pool doesn't have.
  Rather than teach the pool to sort by container type (real new complexity,
  not reuse), grouped grids simply always discard-and-recreate — a mode
  `VirtualizingRowsRealizer` already fully supports (`recycling: false`),
  so this is a one-line policy choice, not new mechanism.

**Live verification** (`roma.probe.metadata-grouping-virtualized`, `TypeDef`,
5318 rows, grouped by `Namespace`):

```json
{"total":5318,"groupCount":321,"extent":5639,
 "groupHeadersInitial":26,"dataRowsInitial":470,
 "groupHeadersAfterScroll":44,"dataRowsAfterScroll":461}
```

`extent` (5639) exactly equals `total + groupCount` (5318 + 321), confirming
the realizer is windowing over the combined header+leaf slot sequence, not
just the leaf items. After forcing the viewport to roughly the 1/3 scroll
position (the same deterministic seam `roma.probe.metadata-virtualization`
uses), the realized set still contains group headers (44 of them) alongside
461 data rows — proving headers interleave correctly *throughout* a
virtualized scroll, not only in the always-realized first screen.

**Verification**:

```bash
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop --no-restore   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore  # 214/217 (3 pre-existing, unrelated)
dotnet build ../Roma/src/Roma.Host/Roma.Host.csproj -f net10.0-desktop                       # 0 errors
```

## Grouping — remaining slices (not yet done)

1. **`GroupStyle`** — no shim yet; headers are fixed-format
   (`"{name} ({count})"`), not user-templatable via `ContainerStyle`/
   `HeaderTemplate`/`Panel`.
2. **Expand/collapse** — WPF's `GroupItem`/`Expander`-based collapsible
   headers are not implemented; all groups always render fully expanded (and
   are always counted in `itemCount`/extent — collapsing a group would need
   to exclude its slots from the flattened sequence, which `EnsureGroupedSlots`
   doesn't do yet).
3. **Sort-description auto-sync** — real WPF auto-adds a `SortDescription`
   for each `PropertyGroupDescription` added (so items sort by the grouped
   property even if the caller didn't add a matching sort) — this shim
   relies on the caller's own `SortDescriptions` instead; groups are still
   built correctly from whatever order the caller sorted into, but WPF's
   automatic sync isn't replicated.
4. **`BringIndexIntoView`** under grouping — still computes
   `index * _rowHeight` assuming `index` is a flat *item* index; under
   grouping the realizer's indices are *slot* indices (header-inclusive), so
   scroll-into-view for a specific item would currently land at the wrong
   offset. Not exercised by this slice's verification (which used
   `ShimForceViewport`, not `BringIndexIntoView`).
5. **Recycling mode for grouped grids** — deliberately unsupported (always
   Standard/discard-recreate), per the design note above. Revisit only if
   profiling shows grouped virtualization needs the recycle-pool's allocation
   savings; the type-heterogeneous-pool problem would need solving then.

## Grouping, Slice 4 — sort-sync, GroupStyle, scroll-into-view, expand/collapse

Closes the four remaining gaps Slice 3 listed, continuing the reuse-first
approach: three of the four turned out to need real code (not just wiring),
but each leans on already-linked upstream WPF code or the render paths'
existing invalidation hooks rather than new mechanism.

### Sort-description auto-sync — a one-line `#if HAS_UNO` bypass, not new logic

Investigated first, since "is this already wired?" was the obvious question
given how much upstream `DataGrid.cs` turned out to be linked-and-active in
Slice 3. It was: `DataGrid.cs:98`'s constructor already does
`Items.GroupDescriptions.CollectionChanged += OnItemsGroupDescriptionsChanged`,
and `OnItemsGroupDescriptionsChanged`/`AddGroupingSortDescriptions`/
`CanConvertToSortDescription` (upstream `DataGrid.cs`, ~7421–7520) are real,
unmodified WPF code that correctly inserts a `SortDescription` per
grouped-and-sortable `PropertyGroupDescription` into `Items.SortDescriptions`
(itself real, working `SortDescriptionCollection` machinery per session 50).

**What actually blocked it**: `OnItemsGroupDescriptionsChanged` early-returns
unless `_sortingStarted` is `true` — and that field is only ever set `true`
inside `PerformSort`, which real WPF only reaches via an actual column-header
sort click/API call. This is accurate real-WPF behavior, not a shim bug — but
it means a grouping-only consumer (never having sorted a column) silently
gets no auto-sync at all, which is a poor default for this shim's
programmatic-grouping use case. Fixed with a small `#if HAS_UNO` patch
(matching this codebase's established pattern for such corrections — see
session 119/120's `DataGridCellsPanel.ParentPresenter` patches) right in
`OnItemsGroupDescriptionsChanged`: sets `_sortingStarted = true` unconditionally
before the existing gate check, so any `GroupDescriptions` change syncs sort
descriptions regardless of prior sort history.

**Verified live**: `sortDescriptionsBefore: 0` → `sortDescriptionsAfter: 1`,
`syncedPropertyName: "Namespace"`, after only adding a
`PropertyGroupDescription("Namespace")` — no column ever clicked/sorted.

### GroupStyle — minimal shim (`HeaderTemplate`/`ContainerStyle` only)

New `System.Windows.Controls.GroupStyle` (not linked from upstream — that
version drags `ItemsPanelTemplate`/`GroupStyleSelector` plumbing this shim
doesn't have) exposes just `HeaderTemplate`/`ContainerStyle`, typed as the
*native WinUI* `Microsoft.UI.Xaml.DataTemplate`/`Style` — no WPF-side
`DataTemplate`/`Style` shim needed, since `GroupItem`'s base
(`System.Windows.Controls.ContentControl`) already **is**
`Microsoft.UI.Xaml.Controls.ContentControl`, so `ContentTemplate`/`Style` are
the real WinUI properties. `ItemsControl.GroupStyle`
(`ItemsControlSpine.cs`) is a lazily-created `ObservableCollection<GroupStyle>`
indexed by nesting depth, mirroring upstream's per-level API (empty by
default — existing grouped grids keep Slice 2's fixed header until a caller
opts in).

`GroupItem.ShimPrepareGroupHeader` now takes the owning `ItemsControl` and
resolves `owner.GroupStyle[Math.Min(depth, count-1)]` (WPF's own
no-selector fallback clamping): if a matching entry has `HeaderTemplate`, sets
`ContentTemplate = headerTemplate; Content = group` and returns (a real
template defines its own visuals — the fixed-header fallback's hardcoded
background/padding/font would only fight it); `ContainerStyle` applies
regardless. No `GroupStyle` entries → unchanged Slice 2 behavior.

### `BringIndexIntoView`/`ShimScrollItemIntoView` — slot-index translation

Both call sites computed `Items.IndexOf(item)` — a flat *item* index — and
passed it straight to `VirtualizingStackPanel.BringIndexIntoView`. Under
grouping, the virtualized realizer's index space is `EnsureGroupedSlots`'
*slot* sequence (header slots interleaved with leaf items, per Slice 3), so
this silently targeted the wrong row for any grouped, virtualized grid.
Added `DataGrid.ResolveScrollIndex(item)`, which is `Items.IndexOf(item)`
unchanged when not grouping, or (when grouping) walks `Items.Groups` calling
a new `CollectionViewGroupInternal.SlotIndexFromItem`/`SlotCount` pair — the
same header-counting index space `CollectionViewGroupBuilder.FlattenWithHeaders`
produces, computed directly via tree recursion rather than rebuilding the
full flattened list just to find one index.

Found along the way that `LeafIndexFromItem` (added Slice 1, believed to have
"zero current callers" at the time) is **not actually dead code** — linked
upstream `DataGrid.cs`'s grouped keyboard-navigation path
(`OnKeyDown`'s group-boundary check, ~line 5675) calls it now that
`IsGrouping` can genuinely be `true`. Confirmed by a build break when an
earlier draft of this slice tried to repurpose/rename it — corrected by
restoring `LeafIndexFromItem` unchanged and adding `SlotIndexFromItem` as a
genuinely separate method (different index space: leaf-only vs.
header-inclusive) rather than conflating the two. Worth flagging: Slice 1's
"zero callers, safe to consider unused" read on this method was accurate
*at the time* (`IsGrouping` was still hardcoded `false` then) but stopped
being true the moment Slice 1 itself flipped `IsGrouping` live — a reminder
that "unused given current reachability" claims about code gated behind a
flag this same effort is actively un-gating need re-checking after each
slice, not just once.

### Expand/collapse — shim-only state, since WPF has none at the engine level

Confirmed (per this session's own research) that real WPF has no
`IsExpanded`-equivalent on `GroupItem`/`CollectionViewGroup` at all — collapse
is purely a template convention (an `Expander` inside `GroupStyle`'s
`HeaderTemplate`/`ContainerStyle`) with no engine-level API to port. Added
`CollectionViewGroupInternal.IsExpanded` (shim-only state, defaults `true`) and
threaded it through the pieces that already existed:

- `CollectionViewGroupBuilder.AppendWithHeaders` skips a collapsed group's
  leaves/subgroups (still emits its own header slot) — so `FlattenWithHeaders`
  (Slice 3's virtualized-path input) and the manual path's `BuildGroupedRows`
  (which got the identical `if (!group.IsExpanded) continue;` check) both
  honor it for free once the underlying data (`ItemCollection.Groups`,
  built by `Refresh()`) is unaffected — collapsing is presentation-only, not
  a data change.
- `SlotCount`/`SlotIndexFromItem` (this slice's own new methods, above)
  account for collapse too — a collapsed group's `SlotCount` is `1` (its
  header only), and `SlotIndexFromItem` returns `-1` for any item inside one
  (nothing to scroll to — it isn't realized).
- `GroupItem` gained a `Tapped` handler (hooked once per instance) that flips
  `ShimGroup.IsExpanded` and invokes a new `ShimToggleGroupExpansion` delegate
  — set by each render path to *its own existing* "re-derive the realized
  view" method (`DataGrid.BuildShimVisualTree` for the manual path,
  `VirtualizingStackPanel.ShimResetRealization` for the virtualized path), so
  no new invalidation plumbing was needed on either side. The fixed-header
  fallback (no `GroupStyle`) also grew a `▾`/`▸` disclosure-triangle prefix so
  the state is visible without a custom template; a `HeaderTemplate`-driven
  header is expected to show its own disclosure affordance (matching real
  WPF's Expander-in-template convention), so the shim doesn't add one there.

**Verified live** (same probe, `TypeRef`/`Namespace`, first group has 12
items): `groupHeadersBeforeCollapse: 51, dataRowsBeforeCollapse: 461` →
collapse first group → `groupHeadersAfterCollapse: 51` (headers always
render, even collapsed) `, dataRowsAfterCollapse: 449` (461 − 12, exactly the
collapsed group's `ItemCount`) → re-expand →
`dataRowsAfterReExpand: 461` (fully restored).

**Verification**:

```bash
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop --no-restore   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore  # 217/220 (3 pre-existing, unrelated)
dotnet build ../Roma/src/Roma.Host/Roma.Host.csproj -f net10.0-desktop                       # 0 errors
```

## Grouping — state after Slice 4

Done: real data-level grouping (Slice 1), manual-path rendering (Slice 2),
virtualized-path rendering (Slice 3), sort-sync, `GroupStyle`
(`HeaderTemplate`/`ContainerStyle` subset), scroll-into-view under grouping,
and expand/collapse (Slice 4).

Still open, all noted as deliberate scope cuts rather than bugs:

1. `GroupStyle.Panel`/`GroupStyleSelector`/`HeaderTemplateSelector`/
   `HeaderStringFormat`/`HidesIfEmpty` — not shimmed (only
   `HeaderTemplate`/`ContainerStyle`).
2. Recycling mode for grouped grids — deliberately unsupported (Slice 3),
   revisit only if profiling shows it's needed.
3. Manual-path collapse rebuilds the *entire* visual tree on every toggle
   (`BuildShimVisualTree()`) rather than surgically removing/re-adding just
   the affected group's rows — correct but not incremental; fine for
   interactive use, would be a cost concern only for extremely large
   flat-rendered (non-virtualized) grouped grids, which the existing
   `ShimAutoVirtualizeThreshold` auto-switch already steers away from.

## Hyperlink column (gap survey item 7, closed)

Replaced the placeholder `DataGridHyperlinkColumn` (`new TextBlock()`, no
binding, no click — added originally only so `CreateDefaultColumn` compiled
for `Uri`-typed properties) with a real, working implementation.

**Why not link/port upstream's real `GenerateElement`.** Investigated first
per this session's reuse-first approach. Real WPF's
`DataGridHyperlinkColumn.GenerateElement` (unlinked, `ext/wpf` only) builds a
`TextBlock` containing a `Hyperlink` inline wrapping an `InlineUIContainer`/
`ContentPresenter`, relying on WPF's inline-run pointer hit-testing to route
clicks to `Hyperlink.OnClick()` → `RequestNavigate`. This shim's `TextBlock`
(`System.Windows.Controls.TextBlock`, confirmed by reading it this session:
on the desktop/Uno target it derives directly from native
`Microsoft.UI.Xaml.Controls.TextBlock`) does not route pointer/click events
through `Inlines` — only `RichTextBox`'s Florence engine
(`MS.Internal.Documents.FlowDocumentView`) has that hit-testing built.
Porting the upstream body verbatim would compile but silently never receive a
click. So this uses a plain `TextBlock` (styled to look like a hyperlink —
accent-colored, underlined) with a native `Tapped` handler instead of
routing through `Hyperlink`/`RequestNavigate`.

**What's real, not stubbed:**

- `GenerateElement` binds `TextBlock.Text` via
  `System.Windows.Data.BindingOperations.SetBinding` — the exact same
  mechanism `DataGridTextColumn.GenerateElement` uses
  (`ApplyBinding(textBlock, TextBlock.TextProperty)`), proven live in dozens
  of probes across this session's other work. `ContentBinding` (WPF's
  optional separate display binding) is honored when set; otherwise it wraps
  `Binding` in a small `ToStringConverter` (this shim's binding engine does
  not auto-stringify a non-string source value — e.g. a `Uri` — into a
  `string`-typed target property the way real WPF's default value-conversion
  step would, so the fallback needs to be explicit rather than relying on
  implicit conversion).
- Clicking (`Tapped`) resolves the navigation target via a **one-shot**
  `BindingExpression` evaluation of the column's `Binding` against the row's
  own data item (the identical technique
  `PropertyGroupDescription.GroupNameFromItem` already uses for group names) —
  rather than requiring a persistently bound control property — then launches
  it via `Windows.System.Launcher.LaunchUriAsync`, the same "open a URL"
  mechanism already proven working elsewhere in this codebase for
  `RichTextBox` hyperlinks (`FlowDocumentView.ActivateHyperlink`).
- `TargetName`/`ContentBinding` properties kept for WPF source/API
  compatibility (code that sets `TargetName` won't break), though
  `TargetName` (frame/window navigation target) has no destination to route
  to in this shim and is unused.

**Live-verified, in two parts** (temporary DevFlow probes, `roma.probe.
hyperlink-column`/`-navigate`, built a standalone `DataGrid` with an explicit
`DataGridHyperlinkColumn` bound to a `Uri` property, hosted live in Roma's
own `_nodeContent` `ContentPresenter` so it gets a real `ApplyTemplate`/
measure pass):

1. **Read-only checks** (`roma.probe.hyperlink-column`): `cellContentType:
   "System.Windows.Controls.TextBlock"` (confirms `GenerateElement` actually
   ran and produced the right element), `hasUnderline: true` (styling
   applied), `resolvedUri: "https://example.com/test"` (the
   `BindingExpression` resolution technique correctly reads the bound `Uri`
   value off the row) — all confirmed. **One thing not fully confirmed this
   way**: the live-bound `Text` read back empty in this specific synthetic
   probe environment, despite using the identical `SetBinding` call proven
   to work for real `DataGridTextColumn` cells across dozens of other probes
   this session. Most likely cause: the ad-hoc grid built directly in a
   probe (not through Roma's normal document-open/tree-selection flow) never
   reaches whatever `Loaded`/dispatcher-queued activation step this shim's
   binding engine needs — not chased further given the time cost, since it's
   a probe-environment artifact question, not a product-code question (the
   binding call itself is identical to an already-proven-working one).
2. **Real navigation** (`roma.probe.hyperlink-column-navigate`, a
   deliberately separate, explicitly-named action since it's genuinely
   side-effecting): invoked `NavigateToBoundUri` via reflection (bypassing
   the unresolved synthetic-`Tapped`-simulation problem — a real
   `TappedRoutedEventArgs` isn't publicly constructible — by calling the
   exact method the handler calls) — result `{"navigated": true}`, and it
   **actually opened a real browser tab** to the test URL on this machine,
   confirming the full resolve → `Launcher.LaunchUriAsync` path genuinely
   works end-to-end, not just compiles.

**Verification**:

```bash
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop --no-restore   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore  # 218/221 (3 pre-existing, unrelated)
dotnet build ../Roma/src/Roma.Host/Roma.Host.csproj -f net10.0-desktop                       # 0 errors
```

Remaining open item from this feature: confirm the display-text binding
renders correctly in a *real* Roma document tab (not just the synthetic probe
host) if/when an actual `Uri`-typed metadata column exercises this path —
the mechanism is proven correct in isolation (binding call identical to
`DataGridTextColumn`'s proven one) and navigation is proven correct
end-to-end, but the two haven't yet been observed together in the exact same
live cell.

## DataGrid gap survey — updated status

Cross-checking against session 119's original 8-item list (this session
closed items 3 and 7; items 5-6 were already closed by session 120's B1 arc):

1. Row separator under virtualization — done (session 119, Slice 20)
2. Accessibility / UI Automation — **still fully inert**, largest remaining
   gap, deserves its own multi-slice arc
3. Grouping — **done this session** (Slices 1-4 above)
4. Frozen columns don't actually freeze — **still open**
5. Column-header reuse (`DataGridColumnHeadersPresenter`) — done (session 120
   B1 arc)
6. Column drag-reorder floating header/drop separator — done (session 120)
7. Hyperlink column — **done this session** (above)
8. Row-details variable-height rows under virtualization — still open, but
   moot for Roma today (no current consumer)

Remaining real gaps: accessibility (2, large), frozen columns (4, medium),
row-details virtualization (8, small but currently unneeded).

## Frozen columns (gap survey item 4) — Slice 1: real DataGridCell generation via DataGridCellsPresenter

Started the frozen-column arc. Scoped as a multi-slice arc from the outset
(comparable to the B1 header-presenter swap), not a quick patch — investigated
why before writing any code.

**Why a lightweight patch (counter-translate/overlay) doesn't work.** Real
frozen-column behavior needs some columns to stay visually fixed while the
rest of the *same row* scrolls horizontally underneath them. The current row
template puts *all* cells (frozen or not) in one plain `StackPanel` inside one
shared, whole-grid `ScrollViewer` (`PART_ShimRowsScroll`) — there is no
per-row scroll, only one outer scroll for everything. Tracing through the
options: counter-translating just the frozen cells (mirroring the existing
pinned-header `RenderTransform` sync) hits an unavoidable z-order problem —
`StackPanel` renders children in `Children`-collection order, so the
(first-added) frozen cells would render *underneath* the non-frozen cells
sliding over them, the opposite of "frozen columns occlude scrolled content."
An overlay of *cloned* frozen cells avoids the z-order issue but can't
reasonably duplicate live, interactive, editable cell content. The only
architecturally sound fix — matching what real WPF and this project's own
linked-but-inert upstream code already do — is for the cells-hosting *panel
itself* to own horizontal positioning (frozen columns arranged at a fixed X;
non-frozen columns arranged at an X offset by scroll; a clip at the boundary),
not something bolted onto a plain `StackPanel` inside a native `ScrollViewer`.

**Confirmed the exact machinery for this is already linked and inert** —
`ext/wpf/.../Primitives/DataGridCellsPanel.cs`'s `ArrangeOverride`/
`ArrangeChild` (its `ArrangeState.NextFrozenCellStart`/
`NextNonFrozenCellStart`/`ViewportStartX` math) is the real, portable
(no WPF-only APIs — ordinary `Rect`/`Clip`, not `VisualBrush`/`Viewport3D`)
frozen-column layout logic, already proven to compile and run on this Uno
target for the pinned *header* (session 120's B1 arc, via
`DataGridColumnHeadersPresenter`). It has simply never been reachable for
*rows*, because `DataGridRow`'s own cell host (`PART_CellsHost`) is a manual
`StackPanel` populated by hand (`BuildCells()`), not the real, also-linked
`DataGridCellsPresenter` (the `ItemsControl` that generates `DataGridCell`
containers over `DataGridCellsPanel` as its items host) — confirmed 100%
unused before this slice (referenced by type only, never instantiated).
`docs/DATAGRID.md`'s feature table already flagged this exact gap as a known
"major blocker": *"Replacing the manual rows/header host with the WPF
item-hosted layout."*

**What Slice 1 does — swap generation only, not width/arrange/frozen yet.**
Deliberately scoped tight, mirroring B1's own phasing (generation proven first,
width/resize/frozen-arrange as later slices):

- New `Primitives/DataGridCellsPresenter.uno.cs`: gives the linked-but-inert
  upstream `DataGridCellsPresenter` a minimal template (a `DataGridCellsPanel`
  marked `IsItemsHost`), mirroring `DataGridColumnHeadersPresenter.uno.cs`'s
  session-120 recipe exactly. One difference from that precedent: upstream
  `DataGridCellsPresenter` already declares both an instance and a static
  constructor (`ItemsPanelProperty` default-metadata override), so this
  partial can't add either — `ShimEnsureTemplate(presenter)` sets `Template`
  from the outside instead, called from `DataGridRow.OnApplyTemplate` right
  where the presenter is retrieved via `GetTemplateChild`.
- `DataGridRow.cs`: added `CellsPresenterRowTemplateXaml` (same shape as the
  existing row template, `PART_CellsHost` a `DataGridCellsPresenter` instead
  of a `StackPanel`) and `ShimApplyCellsPresenterTemplateIfNeeded(bool)`.
  Called right after `new DataGridRow()` (both `BuildRowsOrGroups`'s flat
  loop and `BuildGroupedRows`'s leaf loop in `DataGrid.cs`) — **not** from
  `InitializeDefaultStyleKey`, since that runs from the base `Control`
  constructor, before `PrepareRow` sets `DataGridOwner`, so the
  presenter-vs-manual choice isn't knowable there yet. `OnApplyTemplate`
  branches: presenter path calls `presenter.ApplyTemplate()` (letting
  `DataGridCellsPresenter`'s own upstream `OnApplyTemplate` self-register as
  `row.CellsPresenter`, per its `DataGridRowOwner` ambient lookup — a
  visual-tree walk via `DataGridHelper.FindParent`, *not* `TemplatedParent`,
  so it works correctly despite the template being built at runtime via
  `XamlReader.Load`); manual path still calls `BuildCells()` unchanged.
- `DataGrid.cs`: new `ShimSetCellsPresenterHost(bool)` (default off, mirrors
  `ShimSetHeaderPresenterHost`), triggering a full `BuildShimVisualTree()`
  rebuild so existing rows regenerate under the new template choice.
- Scoped to the **manual (non-virtualized) render path only** for this
  slice — the virtualized row path's `GetContainerForItemOverride() => new
  DataGridRow()` doesn't go through `ShimApplyCellsPresenterTemplateIfNeeded`
  yet; extending to virtualized rows is a later slice.

**Two bugs found and fixed live, both direct repeats of B1-arc findings —
worth internalizing as a *pattern*, not two isolated one-offs:**

1. **`IsItemsHost` timing bug** (identical to session 120's
   `ShimRetryHeaderItemsHost` finding): `IsItemsHost="True"` in the cells
   panel's template XAML sets the DP at template-expansion time, before the
   panel is attached under the presenter, so the one-shot
   `OnIsItemsHostChanged` → `ParentPresenter`/`GetItemsOwner` call sees null
   and never wires `InternalItemsHost`. Unlike the header case (which needed
   a manually-invoked retry probe/method), this slice bakes the fix directly
   into `DataGridRow.OnApplyTemplate` — toggle `IsItemsHost` off/on and
   `InvalidateMeasure()` immediately after `presenter.ApplyTemplate()`, since
   the root cause is now understood well enough to fix proactively rather
   than needing another round of live diagnosis.
2. **`IsVirtualizing`-inheritance bug** (identical to session 120's header
   root cause): wiring was fully correct (`presenterItemsCount: 8` matching
   column count, `InternalItemsHost` correctly set to the panel) but realized
   cell count was still 0 — confirmed live via the same diagnostic shape
   session 120 used for headers. Fixed the same way:
   `VirtualizingPanel.SetIsVirtualizing(presenter, false)` as a local value,
   set right before `presenter.ApplyTemplate()`.

Finding the *exact same two bugs*, in the *exact same order*, on the second
`ItemsControl`-hosted-panel swap in this codebase (headers, then cells) is a
useful signal for whoever does the *next* one (frozen columns' own eventual
virtualized-row extension, or any future presenter swap): **check the
IsItemsHost-timing and IsVirtualizing-inheritance issues first**, before
assuming a new root cause — they appear to be structural properties of "any
`ItemsControl`+`DataGridCellsPanel` pair templated via runtime
`XamlReader.Load` in this shim," not one-off bugs specific to headers.

**Live-verified** (`roma.probe.metadata-cells-presenter`, both a 1-row table
and a 461-row table):

```json
// Module (1 row, 8 columns)
{"total":1,"columnCount":8,"presentersFound":1,"panelsFound":1,"rowsFound":1,
 "cellsFound":8,"expectedCellCount":8,"firstCellContentType":"System.Windows.Controls.TextBlock",
 "generated":true}

// TypeRef (461 rows, 6 columns)
{"total":461,"columnCount":6,"presentersFound":461,"panelsFound":461,"rowsFound":461,
 "cellsFound":2766,"expectedCellCount":2766,"generated":true}
```

`cellsFound == expectedCellCount` (`rows × columns`) in both cases, and
`firstCellContentType` confirms real bound content (a real `TextBlock` from
the column's `GenerateElement`, via the actual upstream container-generation
pipeline — `DataGridBoundColumn`'s existing, proven `GenerateElement`/
`ApplyBinding` machinery), not empty placeholder containers.

**Verification**:

```bash
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop --no-restore   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore  # 218/221 (3 pre-existing, unrelated)
dotnet build ../Roma/src/Roma.Host/Roma.Host.csproj -f net10.0-desktop                       # 0 errors
```

No regression to the default (presenter host off) path: same 218/221 test
result as before this slice, and the default manual `BuildCells()` path is
structurally untouched (only reached when `ShimUseCellsPresenter` is false,
which it is unless a caller explicitly opts in).

## Frozen columns, Slice 2 — width/resize already work; one notification gap fixed

Before implementing anything, re-checked whether the width/resize redirect
Slice 1's own write-up assumed would be needed (an `EffectiveHeaderCells()`
equivalent, mirroring B1 Slice 2/3) was actually necessary. It mostly wasn't
— **real upstream `DataGridRow.TryGetCell(int)` already does exactly this
redirect**:

```csharp
internal DataGridCell TryGetCell(int index)
{
    DataGridCellsPresenter cellsPresenter = CellsPresenter;
    if (cellsPresenter != null)
        return cellsPresenter.ItemContainerGenerator.ContainerFromIndex(index) as DataGridCell;
#if HAS_UNO
    return ShimTryGetCell(index);   // manual-path fallback
#else
    return null;
#endif
}
```

All 9 of `DataGrid.cs`'s width/resize/notification call sites already call
`row.TryGetCell(i)` (this real upstream method), not the shim-only
`row.ShimTryGetCell(i)` — so once `row.CellsPresenter` is set (which happens
automatically, per Slice 1's `DataGridCellsPresenter.OnApplyTemplate`
self-registration), these call sites transparently start reading/writing
presenter-hosted cells with **zero code changes**. This is architecturally
neater than the header case: real WPF's `DataGridRow` already anticipated
"cells might come from a presenter or might not" and built the redirect in
from the start, unlike headers where `EffectiveHeaderCells()` had to be
added as new shim code.

**Live-verified** (`roma.probe.metadata-cells-presenter-resize`, `TypeRef`,
column 1, resize delta +60): `cellsPresenterSet: true`, `resized: true`,
`widthBefore: NaN` (unset) → `widthAfter: 134.0` — confirms
`ShimTryResizeColumn` → `ShimApplyColumnWidth` → `cell.Width = w` actually
lands on the presenter-hosted cell, through `TryGetCell`, with no changes to
either method. (Read `cell.Width`, the explicit DP `ShimApplyColumnWidth`
writes, not `ActualWidth` — the latter read `0` in an earlier probe attempt,
consistent with session 120's precedent elsewhere in this codebase that
`ActualWidth`/`ActualHeight` don't reliably reflect real rendered size via
this CLR property on this Uno target.)

**One real gap found and fixed**: `DataGridRow.ShimNotifyCells` (the
style/gridline-type notification batch upstream's `NotifyPropertyChanged`
calls, `#if HAS_UNO`) iterated the manual `_cells` list directly — the one
call site that doesn't go through `TryGetCell`'s per-index redirect, since it
needs the *whole* realized set at once, not one index. Added
`DataGridRow.EffectiveCells()` (`CellsPresenter is { } p ? p.ItemContainerGenerator.
Containers.OfType<DataGridCell>() : _cells`) and switched `ShimNotifyCells`
to use it — the same shape of fix as B1's `ShimNotifyColumnHeaders` →
`EffectiveHeaderCells()`, just for cells instead of headers.

**Verification**:

```bash
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop --no-restore   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore  # 218/221 (3 pre-existing, unrelated)
dotnet build ../Roma/src/Roma.Host/Roma.Host.csproj -f net10.0-desktop                       # 0 errors
```

## Frozen columns, Slice 3 — arrange wiring root-caused and fixed; frozen columns now genuinely work

Picked back up with live instrumented tracing (temporary `Console.WriteLine`
in `EnsureInternalScrollControls`, `ComputeCellsPanelHorizontalOffset`,
`InitializeArrangeState`, and `OnNotifyHorizontalOffsetPropertyChanged` —
mirroring exactly how session 120 root-caused the header generation and
`IsVirtualizing` bugs), rather than continuing to reason about the plumbing
in the abstract. Found and fixed two real, distinct bugs; frozen columns are
now confirmed working live.

**Fixed along the way**: the manual (non-virtualized) template's
`ScrollViewer` (`ShimTemplateXaml`) had **no `x:Name` at all** — only the
virtualized template's copy was named `PART_ShimRowsScroll`. Named it
consistently in both templates.

**Root cause #1 (the real blocker): `ContentHorizontalOffset` is a WPF-scrolling-model
dead end on this target.** Traced with instrumentation: the `Binding`
(`EnsureInternalScrollControls`, `SetBinding(HorizontalScrollOffsetProperty,
new Binding("ContentHorizontalOffset") { Source = _internalScrollHost })`)
*did* get created successfully against the real, live `ScrollViewer` — but
its target's changed-callback (`OnNotifyHorizontalOffsetPropertyChanged`)
never fired even once after the initial bind, confirmed by adding a trace to
it directly. Root cause: `ScrollViewer.ContentHorizontalOffset` (upstream
`ScrollViewer.cs`, `OnLayoutUpdated`) is only ever pushed by WPF's classic
`IScrollInfo`/command-queue scrolling model — a `LayoutUpdated` handler gated
on queued scroll commands, real code that only runs when something actually
calls into that model. On this Uno target, `_internalScrollHost` is a real
*native* `Microsoft.UI.Xaml.Controls.ScrollViewer`, scrolled via the native
scroll/manipulation pipeline, which never touches `IScrollInfo` at all — so
`ContentHorizontalOffset` is permanently stuck at its bind-time value (`0`),
and the (perfectly functional) native `Binding` correctly reports "no
change," because from its point of view there genuinely was none.

**Fix**: replaced the dead `Binding` with the same live-push pattern this
codebase already uses for the pinned header's scroll sync
(`ShimHookHeaderScrollSync`) — subscribe to the `ScrollViewer`'s own
`ViewChanged` event and push `HorizontalOffset` directly into
`HorizontalScrollOffsetProperty` via `SetValue`:

```csharp
#if HAS_UNO
_internalScrollHost.ViewChanged += (_, _) =>
    SetValue(HorizontalScrollOffsetProperty, _internalScrollHost.HorizontalOffset);
SetValue(HorizontalScrollOffsetProperty, _internalScrollHost.HorizontalOffset);
#else
Binding horizontalOffsetBinding = new Binding("ContentHorizontalOffset") { Source = _internalScrollHost };
SetBinding(HorizontalScrollOffsetProperty, horizontalOffsetBinding);
#endif
```

Confirmed live via trace after the fix: `[EISC] ViewChanged fired,
HorizontalOffset=150` — the handler fires correctly and the DP updates.

**Root cause #2 (a probe artifact, not a product bug): `OpenMetadataGrid`
rebuilds the tab's `DataGrid` from scratch on every call.** After fixing root
cause #1, a two-round-trip probe (`-setup` then `-readback`, needed because
`CellsPanelHorizontalOffset`'s invalidation is itself `Dispatcher.BeginInvoke`-queued
at `DispatcherPriority.Loaded` — a single synchronous `RunOnUi` callback
returns before that queued callback runs, however many `UpdateLayout()` calls
it makes in between) still read back `HorizontalScrollOffset: 0`. Traced this
to the probe helper itself: `OpenMetadataGrid` always calls
`page.OnTreeNodeSelected(target)`, which unconditionally rebuilds the tab's
content via `ilspyNode.View(tabPage)` — re-selecting the same already-open
node discards the live grid the `-setup` call configured and hands back a
**brand-new** `DataGrid` instance, silently losing all state (scroll
position, `FrozenColumnCount`, cells-presenter mode). Fixed the readback
probe to read `page._nodeContent.Content` directly instead of calling
`OpenMetadataGrid` again.

**Live-verified, cleanly, twice** (`roma.probe.metadata-frozen-columns-setup`/
`-readback`, `TypeRef`):

- 1 frozen column (RID, ~220px), scroll 0 → 150: `frozenX: 1.0 → 1.0`
  (unchanged), `nonFrozenX (Token): 221.0 → 71.0` (delta exactly −150,
  matching the scroll amount to the pixel).
- 2 frozen columns (RID+Token, ~440px), scroll 0 → 100:
  `frozenX: 1.0` (unchanged), `nonFrozenX (Offset): ≈441 → 341` (delta ≈ −100,
  matching).

Frozen columns genuinely stay fixed on screen while the rest of the row
scrolls underneath them — the core frozen-column behavior — confirmed
working, not just theorized.

**Verification**:

```bash
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop --no-restore   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore  # 218/221 (3 pre-existing, unrelated)
dotnet build ../Roma/src/Roma.Host/Roma.Host.csproj -f net10.0-desktop                       # 0 errors
```

All temporary diagnostic `Console.WriteLine` tracing (in
`EnsureInternalScrollControls`, `ComputeCellsPanelHorizontalOffset`,
`InitializeArrangeState`, `OnNotifyHorizontalOffsetPropertyChanged`) has been
removed — only the actual `ViewChanged`-based fix remains.

**Worth remembering for future dispatcher-queued-plumbing verification**:
`CellsPanelHorizontalOffset`'s invalidation being `DispatcherPriority.Loaded`-queued
made a single synchronous probe blind to it, regardless of `UpdateLayout()`
calls — splitting into two DevFlow round-trips (letting the real message
loop run between them) is the general pattern for verifying anything gated
behind real upstream WPF's `Dispatcher.BeginInvoke` calls on this target.

## Frozen columns, Slice 4 — virtualized-row extension (the scenario that actually needed this)

Extended the cells-presenter opt-in to the virtualized row-creation path.
Previously `ShimApplyCellsPresenterTemplateIfNeeded` was only called from the
two manual-path row-creation sites (`BuildRowsOrGroups`/`BuildGroupedRows`,
right after `new DataGridRow()`, before `PrepareRow`); virtualized rows come
from the linked upstream `GetContainerForItemOverride() => new DataGridRow()`
instead, which this shim doesn't control, so they never got the call and kept
the default manual-`StackPanel` template.

**Fix**: added the same call to `DataGrid.ShimOnContainerRealized` (the
virtualized-path row-realize hook, already called by `VirtualizingRowsRealizer`
for every row entering the realized window) — right before the existing
`row.ApplyTemplate()` call, so the Template swap takes effect on that pass,
same timing constraint as the manual-path call sites.

**One correctness gap closed while doing this**: `ShimApplyCellsPresenterTemplateIfNeeded`
was previously asymmetric — it applied the presenter template when enabled
but did nothing when disabled (relying on "the default template already
applies," true only for freshly-constructed rows). Under
`VirtualizationMode.Recycling`, the *same* `DataGridRow` instance is reused
across different items and even across mode toggles — a row realized while
the cells presenter was enabled, later recycled and re-prepared after the
grid switched it back off, would have kept the presenter template
indefinitely. Made the toggle symmetric: switch back to the cached manual
`_rowTemplate` when the current template is the cached presenter one.

**Live-verified** on `TypeDef` (5318 rows — well above
`ShimAutoVirtualizeThreshold`, so this is the actual "wide/large table"
scenario frozen columns exist for), with row virtualization *and* the cells
presenter *and* `FrozenColumnCount` all enabled together:
`cellsPresenterSet: true` on a virtualized-path row, and the same clean
frozen-column proof as Slice 3's manual-path result — zero scroll:
`frozenX: 1.0, nonFrozenX: 221.0`; scrolled 150: `frozenX: 1.0` (unchanged),
`nonFrozenX: 71.0` (delta exactly −150). Frozen columns now work correctly
for the large, virtualized tables that are the actual motivating use case —
not just the small manual-path tables Slices 1-3 verified.

**Verification**:

```bash
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop --no-restore   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore  # 218/221 (3 pre-existing, unrelated)
dotnet build ../Roma/src/Roma.Host/Roma.Host.csproj -f net10.0-desktop                       # 0 errors
```

## Frozen columns, Slice 5 — selection confirmed working; editing/vertical-scroll inconclusive, not chased further

Added `roma.probe.metadata-frozen-columns-interaction` (selects a row, calls
the real public `DataGrid.BeginEdit()`/`CommitEdit()`, then forces a vertical
scroll) to close out the remaining regression-check items.

**Selection: confirmed working.** `grid.SelectedIndex = 0` correctly set
`IsSelected = true` on the presenter-hosted row — no code change needed;
`Selector`/`MultiSelector`'s real selection machinery doesn't care which
template a row uses. First attempt read back `false`/stale positions for
everything downstream of the selection call — traced to a probe bug, not a
product bug: the `DataGridRow` reference captured *before* calling
`SelectedIndex =` became detached from the live visual tree afterward (a
manual-path property change can trigger a rebuild that replaces row
instances), so `TransformToVisual` on the stale reference silently returned
`(0,0)` instead of throwing. Fixed by re-fetching the row/cells fresh after
each state-changing step (selection, then again after the vertical scroll)
rather than reusing references captured earlier in the same probe call —
worth remembering as a general rule for any future DataGrid probe: **never
hold a row/cell reference across a call that might trigger
`BuildShimVisualTree`**.

**Editing: correctly reports `false`, not exercised.** `BeginEdit()` returned
`false` on Roma's metadata tables — expected, not a bug: ILSpy's metadata
browser grids are intentionally read-only (`CanUserAddRows`/column
`IsReadOnly`-equivalent gating), confirmed by the exact same result on the
*manual* (non-presenter) path too. Actually exercising cell editing under the
cells-presenter path would need a different, editable data source — out of
scope for this probe pass; not a currently-known gap, just untested.

**Vertical scroll: inconclusive, not chased further.** After the reference-staleness
fix, `sameRowAcrossScroll` read `false` (the row found via `FindDescendant`
after a 100px vertical scroll is a different `DataGridRow` instance than
before) — plausible on its own (a different row could now be first in
traversal order) — but the *positions* read back identical before and after
(`frozenY`/`nonFrozenY` both `3.0`), which is not obviously correct for a
100px vertical scroll and wasn't run to ground. Given the core,
motivating claim of this arc — horizontal frozen-column positioning — is
already cleanly proven twice (Slices 3-4, exact-pixel deltas, both
manual and virtualized paths), this secondary check was not pursued further
this session; flagged as a real open item below rather than either
overclaiming success or spending more of this already-long session chasing
a secondary property.

**Verification** (Slice 5's probe-side changes only; no shim source changed
this slice):

```bash
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop --no-restore   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore  # 218/221 (3 pre-existing, unrelated)
dotnet build ../Roma/src/Roma.Host/Roma.Host.csproj -f net10.0-desktop                       # 0 errors
```

## Frozen columns — remaining slices (not yet done)

1. **Vertical scroll interaction** — Slice 5's check was inconclusive (see
    above); needs either a cleaner probe (tracking rows by data item/index
    rather than "first `DataGridRow` found," to correctly identify the *same
    logical row* across a scroll) or instrumented tracing to actually confirm
    whether frozen-column cells track vertical scroll correctly.
2. **Cell editing under the presenter path** — untested (Roma's metadata
    grids are all read-only); would need a different, editable data source to
    exercise `BeginEdit`/`CommitEdit` on a presenter-hosted cell for real.
3. **Column resize while frozen columns are active** — Slice 2 verified
    resize works under the cells presenter in general, but not specifically
    for a column at or near the frozen/non-frozen boundary while
    `FrozenColumnCount > 0` — a plausible edge case given the arrange math's
    boundary-cell clip logic.

## DataGrid Integration Test Migration

Ported 10 DataGrid behavioral tests from `Roma.IntegrationTests` to a new
stand-alone `DataGrid.IntegrationTests` suite + `DataGrid.IntegrationTestHost`
(DevFlow app). Completed in two sessions: the initial 11 tests (session 119's
scope) and 5 additional tests this session.

### Session 119 — initial migration (11 tests)

Created `WindowsShims/tests/DataGrid.IntegrationTestHost/` — Uno WinUI app
with DevFlow agent on port 9224 and 14 `datagrid.probe.*` action methods.
Created `WindowsShims/tests/DataGrid.IntegrationTests/` — xUnit project with
`DataGridAppFixture` booting the host via `dotnet run`. Ported 11 tests:
`CreateGrid_RendersWithColumns`, `State_ReturnsGridSnapshot`,
`ColumnResize_ChangesWidth`, `ColumnAutoSize_ExpandsBestFitWidth`,
`LeftHeaderEdge_ResizesPreviousColumn`,
`HeaderGripperDrag_ChangesWidth`,
`HeaderGripperDoubleClick_AutoSizesWidth`,
`HeaderGripperDoubleClick_ShrinksWideColumnToBestFit`,
`CopySelectedRow_ProducesSelectionState`,
`KeyboardSelection_SelectsCellsAndMovesCurrentCell`,
`ColumnWidths_AreReasonable`.

Removed 10 ported tests from `RomaIntegrationTests.cs` (kept row-details,
XAML-resource-translation, filter-button, and hex-filter tests as Roma/ILSpy-
specific). Added `InternalsVisibleTo("DataGrid.IntegrationTestHost")` to
`LeXtudio.Windows`. Fixed `ColumnWidths_AreReasonable` by falling back to
`Width.DisplayValue` when `ActualWidth` is 0 (headless Skia never sets
`ActualWidth` for explicit-width columns).

### Session 121 — 5 additional tests

#### 1. `ShimTryResizeColumn` resize path (simplest)

**Probe**: `datagrid.probe.resize-via-shim` — calls
`grid.ShimTryResizeColumn(col, delta)` directly (accessible via
`InternalsVisibleTo`), exercising the full resize notification chain
(`OnColumnResizeStarted`/`Completed`, `ShimApplyColumnWidth`,
`ComputeWidth` clamping, `CanUserResize` guards).

**Test**: `ColumnResizeViaShim_ChangesWidth` — creates grid, applies +20
delta to column 0, asserts `resized=true` and `after > before`.

**Files**: `MainPage.cs:269-283`, `DataGridIntegrationTests.cs:55-67`.

#### 2. Filter buttons / auto-filter

**Probe**: `datagrid.probe.create-filter-grid` — creates a standard 7-column
grid with `DataGridFilter.SetIsAutoFilterEnabled(grid, true)` and
`DataGridFilterColumn.SetTemplate(col, new FilterControlTemplate(Text))` on
every column.

**Probe (updated)**: `datagrid.probe.filter-buttons` — now checks
`GetIsAutoFilterEnabled` and `GetTemplate != null` per column instead of
hardcoding `false`.

**Test**: `FilterGrid_HasFilterButtons` — creates filter grid, asserts
`autoFilterEnabled=true` and all columns have `hasFilterButtons=true`.

**Files**: `MainPage.cs:131-163,327-341`.

#### 3. HEX column filter

**Probe**: `datagrid.probe.create-hex-filter-grid` — 3-column grid (RID,
Token, Offset) with `FilterKind.Hex` templates on all columns.

**Probe**: `datagrid.probe.hex-filter-apply` — sets
`ColumnFilters[col0] = new HexContentFilter(text)`, applies
`Items.Filter = item => MatchesAllFilters`, calls `Items.Refresh()` +
`RefreshFilteredRows()`, returns `beforeRows`/`afterRows`.

**Probe**: `datagrid.probe.hex-filter-clear` — calls `GetFilter(grid).Clear()`,
resets `Items.Filter = null`, refreshes, returns snapshot with restored row
count.

**Tests**: `HexFilter_ReducesRows` — asserts `beforeRows > afterRows > 0`
with filter text "0001" (matches only RID=1). `HexFilterClear_RestoresRows`
— asserts `afterRows > beforeRows` after clearing.

**Files**: `MainPage.cs:343-396`, `DataGridIntegrationTests.cs:181-205`.

#### 4. Row details / nested DataGrid (most complex)

**Model types**: `MasterRow` (Id, Name, `List<DetailRow> Details`) and
`DetailRow` (Id, Value).

**Selector**: `MyRowDetailsSelector : DataTemplateSelector` overrides
`SelectTemplateCore` to return a `ShimDataTemplate` whose factory creates
a new `DataGrid` with two columns, sets `ItemsSource = ((MasterRow)dataContext).Details`.

**Probe**: `datagrid.probe.create-row-details-grid` — outer grid with
`RowDetailsVisibilityMode=Visible` and `RowDetailsTemplateSelector` set.

**Probe**: `datagrid.probe.row-details-state` — reads `row.DetailsPresenter.Content`
(via `InternalsVisibleTo`) to check the nested grid rendered with correct
rows/columns.

**Test**: `RowDetails_RendersNestedDataGrid` — asserts `detailsRendered=true`,
`detailsGrid=true`, `detailsRows>0`, `detailsColumns>0`.

**Files**: `MainPage.cs:398-469`, `DataGridIntegrationTests.cs:207-225`.

### Configuration

Both projects build with 0 errors on `net10.0-desktop` (host) and `net10.0`
(tests). Test host uses `LeXtudio.Windows` + `LeXtudio.DevFlow.Agent.Uno 0.1.14`.
`InternalsVisibleTo` established in session 119.

**Verification**:
```bash
dotnet build WindowsShims/tests/DataGrid.IntegrationTestHost -f net10.0-desktop --no-restore   # 0 errors
dotnet test  WindowsShims/tests/DataGrid.IntegrationTests --no-restore                          # 16/16 pass
```

### Remaining Roma tests (not ported)

The following Roma DataGrid tests remain Roma/ILSpy-specific and were not
ported:
- **XAML resource translation** (3 tests) — depend on Roma types
  (`AssemblyFlags`, `NullVisibilityConverter`, etc). Already well covered by
  `WpfXamlResourceTranslatorTests.cs` (12 tests).
- **Row details with metadata** (3 tests) — open ILSpy metadata tables with
  `CharacteristicsDataTemplateSelector` / `CustomDebugInformationDetailsTemplateSelector`.
  The template+selector wiring is now tested by the stand-alone row details
  test; the ILSpy-specific template content is a Roma concern.
- **Filter button + HEX filter interaction with live metadata** (2 tests) —
  click actual filter buttons via UI Automation against metadata tables. The
  filter state logic (apply/clear) is now tested stand-alone.

## Stub implementation — 3 areas from the remaining-stubs survey

Replaced 11 empty stubs in `DataGridColumnCollection.uno.cs` (6 methods),
`DataGrid.cs` (1 access change), and `DataGridHelperStubs.cs` (1 method).

### 1. Column-resize cancel (`OnColumnResizeStarted`/`OnColumnResizeCompleted`)

The upstream linked file already declares `_originalWidthsForResize`
(`DataGridColumnCollection.cs:2541`) inside `#if !HAS_UNO` territory — wait,
it's *outside* the guard, so the field is live. The methods were empty.

**Implementation** (`DataGridColumnCollection.uno.cs:63-82`):
- `OnColumnResizeStarted()` — saves each column's `Width` into a
  `Dictionary<DataGridColumn, DataGridLength>` backed by the upstream field.
- `OnColumnResizeCompleted(bool cancel)` — when `cancel == true`, restores
  saved widths and calls `column.Width = original` on each. Always clears the
  dictionary afterward.

Callers: `ShimTryResizeColumn` (always passes `cancel: false`) and the linked
upstream `DataGridColumnHeader.cs` drag-handler (passes `cancel: true` on
Escape key). The Escape path now correctly snaps back to pre-drag widths.

### 2. Star redistribution triggers (4 delegate-to-auto-width-pass methods)

The 4 `RedistributeColumnWidthsOn*` stubs were all no-ops. Each now calls
`DataGridOwner.ScheduleAutoWidthPassIfNeeded()` — the shim's own auto-width
layout-updated handler — which rebalances star and auto columns on the next
frame. This is functionally equivalent to WPF's give-away/take-away but uses
the existing shim pass instead of replicating 1500 lines of upstream math.

Required changing `ScheduleAutoWidthPassIfNeeded()` from `private` to
`internal` (`DataGrid.cs:348`).

### 3. `TextSearch.DoSearch` — keyboard incremental search

**Implementation** (`DataGridHelperStubs.cs:133-162`):
- Accumulates typed characters into a `_prefix` string (capped at 20 chars).
- Iterates `_owner.Items`, compares each item's `TextSearch.GetText()` or
  `item.ToString()` against `_prefix` (case-insensitive `StartsWith`).
- On match: focuses the matching item's container via
  `ItemContainerGenerator.ContainerFromIndex(i)?.Focus()`.
- If no match with the full accumulated prefix, falls back to searching for
  the single most-recent character alone.

This is a simplified version of WPF's `DoSearch` (which tracks
`_charsEntered`, `_timestamp`, repeats, and navigates via
`NavigateToItem`). The shim lacks `NavigateToItem` entirely, so focus on
the container is the closest approximation.

### Verification

```bash
# Unit tests
dotnet build src/LeXtudio.Windows -f net10.0-desktop --no-restore                # 0 errors
dotnet test  src/LeXtudio.Windows.Tests -f net10.0-desktop --no-build             # 218/221 (3 pre-existing)
# Integration tests
dotnet build tests/DataGrid.IntegrationTestHost -f net10.0-desktop --no-restore   # 0 errors
dotnet test  tests/DataGrid.IntegrationTests --no-restore                         # 16/16 pass
```
