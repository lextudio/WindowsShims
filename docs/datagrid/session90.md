# Session 90

Goals: (1) finish reducing guards in linked `DataGridCell.cs`; (2) port the real
WPF `DataGridRowHeader` into the Uno build, reusing upstream source and minimizing
local shims.

## Part 1 — DataGridCell guard reduction (17 → 6 pairs)

Continued from session 89. Unified `_owner` and unguarded several methods via the
partial-method-hook pattern.

### NotifyPropertyChanged (pair 4) unguarded
- Removed the `#if !HAS_UNO` wrapping `OnNotifyPropertyChanged` / `NotifyPropertyChanged`.
- Extended `DataGridHelper.TransferProperty` to handle `DataGridCell` (`IsReadOnlyProperty`,
  `StyleProperty`).
- Deleted the duplicate local `NotifyPropertyChanged` and `OnNotifyPropertyChanged` stub
  from the local `DataGridCell.cs` partial.

### `_owner` unified (4 pairs removed)
The local partial had been bypassing WPF's cell-prep pipeline, setting a settable
`RowOwner` auto-property; upstream defined `_owner` + read-only `DataGridOwner`/`RowOwner`/
`RowDataItem` getters, so the two conflicted and were guarded.
- Unguarded `private DataGridRow _owner;`, `DataGridOwner`, `RowOwner`, `RowDataItem` upstream.
- Added `internal void SetOwnerRow(DataGridRow? row) => _owner = row;` to the local partial
  (partial classes share private scope).
- `DataGridRow.BuildCells` now calls `cell.SetOwnerRow(this)` instead of `RowOwner = this`.
- Removed the local `RowOwner`/`DataGridOwner`/`RowDataItem` members.

### Three more guards removed via partial-method hooks / cast trick
- **ctor `#if HAS_UNO` selection-tint** → `partial void OnInitializedShim()` (Uno body in
  local partial, elided under WPF).
- **`NotifyCurrentCellContainerChanged()`** → unguarded the upstream 0-arg method; routed
  Uno focus-border work through `partial void OnCurrentCellContainerChangedShim()`. The local
  method's `oldCell`/`currentCell` params were vestigial, so collapsing to a no-arg hook was clean.
- **`CellsPresenter`** → `... as object as DataGridCellsPresenter` compiles under both type
  hierarchies (WPF `: ItemsControl`, Uno `: Panel` yields null), preserving exact behavior.

### Remaining 6 guards (genuine behavior/type forks, not artifacts)
PrepareCell/ClearCell/Tracker; BuildVisualTree (WPF BindingGroup machinery); IsReadOnly
read-only DP + coercion; BeginEdit/CancelEdit/CommitEdit (signatures differ, returns bool);
`_syncingIsSelected` (proven `DataGrid_CannotSelectCell` regression); GridLines
Measure/Arrange/OnRender (DrawingContext, would double-count the shim BorderThickness).

## Part 2 — DataGridRowHeader port (reuse the real 789-LOC WPF file)

Replaced the ~40-line hand-written stub (formerly in `DataGridPresenters.cs`) with the
upstream `Primitives/DataGridRowHeader.cs`, linked into the build. **Zero `#if` guards
needed** in the linked file.

### Supporting shims added (Step 1)
- `DataGridHelper.FindParent<T>` — visual-ancestor walk used by the header's `ParentRow`.
- 16 `DATAGRIDROWHEADER_*` constants added to the local `VisualStates` (`DataGridHelperStubs.cs`).
- `DataGridRowHeaderAutomationPeer` shim (mirrors `DataGridCellAutomationPeer`), 11 lines.
- `DataGridHelper.TransferProperty` branch for `DataGridRowHeader`: Content ← `row.Header`,
  Width ← `DataGrid.RowHeaderShimWidth`. Style/template transfers are no-ops (no WPF default
  template to coerce against).

### Linking the upstream file (Step 2)
- Added `<Compile Include=...DataGridRowHeader.cs Link=...DataGridRowHeader.upstream.cs />`.
- Marked the upstream class `public partial class`.
- One upstream edit: `OnApplyTemplate` `public` → `protected` to match WinUI — the same
  divergence the fork already made for `DataGridColumnHeader`.
- New local partial `Primitives/DataGridRowHeader.cs` (35 lines) holds only the Uno
  grid-line helper + an owner-row fallback.

### Integration fix (Step 3)
- `DataGridRow.BuildRowHeader` no longer sets the (now read-only) `ParentRow`; it constructs
  the header, parents it, then calls `SyncProperties()`.
- Upstream `ParentRow` resolves via the visual tree, which isn't populated synchronously at
  build time (and broke grid-line rendering — caught by the probe). Added
  `EffectiveRow => ParentRow ?? _shimOwnerRow`; `DataGridRow` records itself via
  `SetShimOwnerRow(this)`. Both `ApplyShimGridLines` and the `TransferProperty` branch use
  `EffectiveRow`. Upstream `ParentRow` remains primary.
- Manual glyph (`RefreshRowHeaderGlyph`, ▶ ✎ ⚠) retained as default content when `Row.Header`
  is unset — a deliberate Uno divergence.

Net new local shim footprint for the row-header feature: **46 lines** (35-line partial +
11-line peer); everything else is reused WPF.

## Part 3 — DataGridDetailsPresenter port (reuse the real 330-LOC WPF file)

Replaced the ~10-line `ContentControl`-based stub with the upstream
`Primitives/DataGridDetailsPresenter.cs` (`: ContentPresenter`), linked into the build.

### New shims
- **`System.Windows.Controls.ContentPresenter`** base shim (`: Microsoft.UI.Xaml.Controls.ContentPresenter`):
  routed-event plumbing, `SetValue(DependencyPropertyKey)`, `CoerceValue`/`AddLogicalChild`/
  `RemoveLogicalChild` no-ops, a dummy `DefaultStyleKeyProperty` (WinUI ContentPresenter is a
  FrameworkElement with none), a `MouseLeftButtonDownEvent` RoutedEvent, and a
  `protected internal virtual OnVisualParentChanged(DependencyObject)` so the upstream override
  binds (never invoked under WinUI).
- `DataGridDetailsPresenterAutomationPeer` shim (mirror of the cell/row-header peers).
- `PresentationSource.IsUnderSamePresentationSource(params DependencyObject?[])` → `true`
  (single-window shim) — lets the upstream mouse-down handler compile and be reused unmodified.
- `DataGridHelper.TransferProperty` branch for `DataGridDetailsPresenter` (ContentTemplate ←
  row.DetailsTemplate ?? grid.RowDetailsTemplate).
- Local partial `Primitives/DataGridDetailsPresenter.cs` with `_shimOwnerRow`/`SetShimOwnerRow`/
  `EffectiveRow` (same fallback as the row header).

### Upstream edits (2 guards + 1 routing)
- `public partial class`.
- `DataGridRowOwner` getter guarded: `#if HAS_UNO` returns `EffectiveRow` (fallback);
  `#else` keeps `FindParent<DataGridRow>`.
- Entire **GridLines region** (`MeasureOverride`/`ArrangeOverride`/`OnRender(DrawingContext)`)
  wrapped `#if !HAS_UNO` — matches how DataGridCell guards its DrawingContext grid-line drawing;
  the shim draws grid lines via BorderThickness elsewhere.
- Everything else reused unmodified: static cctor, automation peer, template coercion,
  `OnVisualParentChanged`, the mouse-down click-to-select handler, `SyncProperties`,
  `NotifyPropertyChanged`, `DetailsElement`.

### DataGridRow.BuildRowDetails rewrite
Now constructs the real presenter, parents it in `PART_DetailsHost`, calls `SetShimOwnerRow`
then `SyncProperties()` (sets `Content = Item`, transfers the template). Removed the stub's
`ParentDataGrid` initializer and `RowDetailsTemplate.LoadContent()` pre-materialization.

### Two debugging finds (probe-driven)
1. WinUI's `ContentPresenter` doesn't flow `Content` to the templated child's DataContext like
   WPF, so `{Binding}` in the details template didn't resolve → set `presenter.DataContext = Item`
   explicitly in BuildRowDetails.
2. The real fix: in a ContentPresenter subclass the upstream's unqualified `ContentTemplateProperty`
   is **WinUI `ContentPresenter.ContentTemplateProperty`**, not `ContentControl.ContentTemplateProperty`.
   The TransferProperty branch was comparing against the wrong DP, so the template was never applied.
   Corrected the comparison.

## Part 4 — Column reorder by drag (shim-native, reusing upstream semantics)

The shim builds the header row **manually** (`DataGrid.BuildHeaderRow` news up headers into a
StackPanel) and does NOT use the upstream `DataGridColumnHeadersPresenter` (1063-LOC ItemsControl,
never instantiated — `ParentPresenter` is always null in the shim). A full presenter port would
mean replacing the entire manual header-generation + frozen arrange architecture, so it is **out
of scope**. Instead, added shim-native reorder that reuses the upstream *event sequence* and
*DisplayIndex semantics*.

### Reused upstream surface (all in the linked DataGrid/column types)
`CanUserReorderColumns`, `DataGridColumn.CanUserReorder`, `OnColumnReordering`/`OnColumnReordered`
(+ the `ColumnReordering`/`ColumnReordered` events), `DataGridColumnReorderingEventArgs.Cancel`,
`DataGridColumnEventArgs`, and `DataGridColumn.DisplayIndex` (whose setter reshuffles siblings).

### New shim code (all in local `DataGrid.cs`)
- A reorder state machine driven by **WinUI pointer events** wired onto each manually-built header
  in `BuildHeaderRow` (PointerPressed/Moved/Released/CaptureLost).
- Drag is only armed past a 4px threshold in PointerMoved (so a plain click still sorts); the
  header is pointer-captured and dimmed to 0.5 opacity once active.
- A minimal **`Border` drop indicator** (2px accent) inserted between headers at the computed
  slot — deliberately avoids the WPF `DataGridColumnFloatingHeader` (VisualBrush) and
  `DataGridColumnDropSeparator`; **those two files are NOT linked/ported** (VisualBrush /
  VisualTreeHelper.GetOffset are unreliable under Uno).
- `ComputeDropSlot` walks realized header widths to find the drop position.
- `ShimTryReorderColumn(column, targetDisplayIndex)` is the core commit: gate → `OnColumnReordering`
  → (abort if `Cancel`) → set `DisplayIndex` → `BuildShimVisualTree()` → `OnColumnReordered`.
  Exposed `internal` so the probe drives the exact same path (headless pointer injection isn't
  available).

### New probe step
"column header drag reorder fires events and changes DisplayIndex": drives `ShimTryReorderColumn`
to move Name from display 0→2, asserts both events fired once with the right column, DisplayIndex
and realized header order updated; then asserts a `Cancel=true` handler leaves order unchanged and
fires no `ColumnReordered`. The existing options-only reorder step is left intact.

## Verification
- `dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj --nologo` → 0 errors.
- `dotnet test src/LeXtudio.Windows.Tests/` → 136/136 pass.
- `dotnet run --project src/LeXtudio.Windows.Sample/ --framework net10.0-desktop -- --probe`
  → `DONE failures=0`, including row-header rendering, grid-lines across all four
  GridLinesVisibility modes, row-details expand/collapse with template binding, and the new
  column-reorder-drag step (move + cancel).
- Guards in `DataGridRowHeader.upstream.cs`: 0. Guards in `DataGridDetailsPresenter.upstream.cs`: 2
  (DataGridRowOwner fallback + GridLines region). No new guards for reorder (shim-native).

## Known limitation
Interactive pointer-driven reorder (capture/hit-test across the StackPanel) is implemented but only
exercised via the direct `ShimTryReorderColumn` seam in the probe; live pointer behavior under Uno
desktop needs manual verification. Frozen-column reorder edge cases are not specially handled.

## Part 5 — DataGrid.cs guard reduction (19 → 16 pairs)

Surveyed all ~19 guard regions in the linked `DataGrid.cs`. Removed the two that were clean,
honest wins (shim the dependency, reuse the WPF code):

- **Telemetry** (`ControlsTraceLogger.AddControl(TelemetryControls.DataGrid)`): added a `DataGrid`
  member to the `MS.Internal.Telemetry.PresentationFramework.TelemetryControls` enum and removed
  the `#if !HAS_UNO`. Also deleted a **duplicate** `ControlsTraceLogger`/`TelemetryControls` in
  `MS.Internal` (DataGridInternals.cs) that caused an ambiguous-reference error once unguarded —
  the `MS.Internal.Telemetry.PresentationFramework` namespace is the WPF-canonical one.
- **Automation peer** (`OnCreateAutomationPeer` → `DataGridAutomationPeer`): the shim already had
  a `DataGridAutomationPeer(DataGrid)` (DataGridAutomationPeers.cs), so removed the
  `#if !HAS_UNO // automation peers are not bridged yet` guard — consistent with the cell/row-header/
  details peers bridged earlier this session. The grid now creates a real automation peer.

- **Dispatcher** (`OnLoadingRowDetails`): unified the `Dispatcher.CurrentDispatcher.BeginInvoke(...)`
  (WPF) / `Dispatcher.BeginInvoke(...)` (Uno) split to the single instance form
  `Dispatcher.BeginInvoke(callback, DispatcherPriority.Loaded, row)`. The `BeginInvoke(Delegate,
  DispatcherPriority, object)` overload exists on both WPF's `Dispatcher` and the Uno shim;
  `Dispatcher.CurrentDispatcher` (static) only failed under Uno because an instance `Dispatcher`
  property shadows the type name. Removed the guard.

The remaining 16 guards are genuine WinUI/WPF divergences, not artifacts: the `partial` class
keyword; `DispatcherTimer` priority ctor + `Tick` handler signature; `new FrameworkElement`
(abstract in WinUI, shim uses `ContentControl`); `Dispatcher.BeginInvoke` vs
`Dispatcher.CurrentDispatcher.BeginInvoke`; transform-bounds visibility test; gridline-thickness
DP vs hardcoded constant; `EffectiveSortMemberPath`; `OnApplyTemplate` public/protected; and the
shim's manual `NotifyPropertyChanged`/`BuildShimVisualTree`/`ShimNotifyColumnHeaders` propagation
that replaces WPF's container-recycling + ColumnHeadersPresenter paths. Forcing these would change
behavior or fail to compile.

Two further candidates were investigated and deliberately left alone:
- **Gridline thickness** (`HorizontalGridLineThickness`/`VerticalGridLineThickness`): the `#if/#else`
  here is upstream WPF's own `#if GridLineThickness` feature flag (disabled in both WPF and Uno
  builds, both using the 1.0 constant) — not a HAS_UNO guard, nothing to shim.
- **DispatcherTimer** (auto-scroll): the priority-ctor + `EventHandler` vs parameterless-ctor +
  `EventHandler<object>` split is a real WinUI/WPF API divergence. Removing it would need a
  namespace-level `System.Windows.Threading.DispatcherTimer` shim, which would shadow WinUI's timer
  for every compiled file using `using System.Windows.Threading` (DataGrid, TextEditor, Window) —
  too broad a blast radius for one guard pair. Left guarded.

Verification unchanged: 0 build errors, 136/136 tests, probe `DONE failures=0`.
