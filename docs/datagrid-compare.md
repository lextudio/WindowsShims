# DataGrid comparison: Windows Community Toolkit v7 vs. this project's WPF-ported DataGrid

Two independent `DataGrid` implementations exist in the Uno/WinUI ecosystem. This
document compares them in detail: **WindowsCommunityToolkit v7's** `DataGrid`
(`Microsoft.Toolkit.Uwp.UI.Controls.DataGrid`, removed from the toolkit in v8) against
**this repository's** `DataGrid` (`System.Windows.Controls.DataGrid`, a real WPF source
port running on Uno/WinUI via the shim types in this library).

They are **not variants of the same code** — they solve "give me a DataGrid on
UWP/WinUI" in two fundamentally different ways, and neither is a superset of the
other. Understanding *why* they differ architecturally explains every feature gap
below.

## Sources

- **WCT v7**: `CommunityToolkit/WindowsCommunityToolkit` (archived), path
  `Microsoft.Toolkit.Uwp.UI.Controls.DataGrid/`. Vendored into this repo as the
  `ext/wct-v7` git submodule (shallow, `main` branch — the last state before v8
  removed the control) purely for reference/comparison; nothing in it is currently
  linked into the build.
- **Ours**: real WPF source (`System.Windows.Controls.DataGrid` and friends) linked
  directly from the `ext/wpf` submodule (a `dotnet/wpf` fork), `#if HAS_UNO`-guarded,
  supplemented by local `.uno.cs` bridge files in `src/LeXtudio.Windows/`.

## Fundamental architectural difference

| | WCT v7 `DataGrid` | This project's `DataGrid` |
|---|---|---|
| **Design origin** | Written from scratch, specifically for UWP | Real WPF `System.Windows.Controls.DataGrid` source, ported to run on Uno/WinUI |
| **Target framework (of the source)** | `uap10.0.17763` (native UWP `Windows.UI.Xaml`) | `net10.0-desktop` / WinUI 3 via Uno (`Microsoft.UI.Xaml`) |
| **Namespace** | `Microsoft.Toolkit.Uwp.UI.Controls.DataGrid` | `System.Windows.Controls.DataGrid` (WPF's own namespace — drop-in for WPF code) |
| **Goal** | *A* DataGrid control usable in UWP/WinUI apps | WPF's *actual source code*, compiling and running unmodified on Uno/WinUI |
| **Base class tower** | Its own `Control`-derived hierarchy, built directly against `Windows.UI.Xaml.Controls` | WPF's real `ItemsControl → Selector → MultiSelector → DataGrid` tower, rebased onto WinUI `Control` via shim types |
| **Size** | ~38,700 lines across 76 files | ~5,200 local shim lines + ~23,700 linked-upstream WPF lines it reuses |
| **License** | MIT (`.NET Foundation`) | MIT (WPF is `dotnet/wpf`, also MIT) |

Because our port literally *is* WPF's DataGrid (compiled against shim types instead
of real WPF assemblies), any WPF documentation, StackOverflow answer, or existing
WPF-targeting code describing `System.Windows.Controls.DataGrid` behavior applies
directly to ours — including internal implementation details, not just the public
API. WCT v7's DataGrid, despite superficially similar naming (`DataGridColumn`,
`DataGridRow`, `DataGridCell`, etc.), is an independent design and does not share
this property.

## Feature-by-feature comparison

### Virtualization

- **WCT v7**: `DataGridRowsPresenter` is a plain `Panel` (not a `VirtualizingPanel`)
  with a hand-written realize/recycle loop built specifically for this control.
  Nothing about it is shared with any other WinUI `ItemsControl`.
- **Ours**: reuses WPF's actual generator-based virtualization model —
  `VirtualizingStackPanel` + `ItemContainerGenerator`, the same machinery every
  WPF `ItemsControl` uses. Extended this session with variable-row-height support
  (a per-index height cache + cumulative-offset layout, for `RowDetails` rows that
  are taller than others) — see `docs/session121.md`, gap survey item 8.

### Grouping

- **WCT v7**: purpose-built for the grid — its own `DataGridRowGroupHeader` class
  and `ICollectionViewGroup` model, native UWP grouping UI designed specifically for
  this control.
- **Ours**: real WPF has *no* DataGrid-specific group-header class at all — WPF's
  DataGrid reuses the generic `ItemsControl`/`GroupItem`/`CollectionViewGroup`
  machinery shared with `ListView` etc., and that's exactly what our port does too.
  Extended this session to full `GroupStyle` API coverage: `HeaderTemplate`/
  `HeaderTemplateSelector`, `ContainerStyle`/`ContainerStyleSelector`,
  `HeaderStringFormat`, `HidesIfEmpty`, and `ItemsControl.GroupStyleSelector` — see
  `docs/session121.md`, grouping Slices 1-5. One deliberate scope cut:
  `GroupStyle.Panel` (a custom per-group items panel) is not shimmed, since our
  DataGrid grouping flattens all groups into one shared virtualized row list (the
  same list frozen columns/editing/virtualization all depend on) rather than
  hosting each group in its own nested `ItemsControl`+panel subtree.

### Frozen columns

- **WCT v7**: a custom `DataGridFrozenGrid` — a `Grid` subclass with an attached
  `IsFrozen` boolean property, purpose-built to keep certain columns visually
  fixed.
- **Ours**: ports WPF's real `DataGridCellsPanel.ArrangeOverride` frozen/non-frozen
  clip-and-offset math — the actual WPF frozen-column algorithm, not a
  reimplementation of the visual effect. Verified working under virtualization,
  with real cell editing and selection, and correct behavior across boundary-column
  resize — see `docs/session121.md`, frozen columns Slices 1-6.

### Accessibility / UI Automation

The sharpest contrast between the two projects.

- **WCT v7**: ships a **complete** automation-peer family purpose-built for its own
  types — `DataGridAutomationPeer` (987 lines, implements `IGridProvider`,
  `IScrollProvider`, `ISelectionProvider`, `ITableProvider`),
  `DataGridCellAutomationPeer`, `DataGridRowAutomationPeer`,
  `DataGridColumnHeaderAutomationPeer`, `DataGridColumnHeadersPresenterAutomationPeer`,
  `DataGridDetailsPresenterAutomationPeer`, `DataGridGroupItemAutomationPeer` (576
  lines), `DataGridItemAutomationPeer` (534 lines), `DataGridRowGroupHeaderAutomationPeer`,
  `DataGridRowHeaderAutomationPeer`, `DataGridRowsPresenterAutomationPeer` — 3,090
  lines total, all targeting native UWP `Windows.UI.Xaml.Automation.Peers`/`.Provider`.
- **Ours**: **not implemented**. This is the single largest, most-repeatedly-flagged
  gap across every session of this DataGrid port (`docs/DATAGRID.md`,
  `docs/session121.md`'s gap survey item 2). `AutomationPeer.FromElement` is
  deliberately stubbed to return `null` and `ListenerExists` returns `false` — an
  honest "not supported" rather than a silently-broken partial implementation.
  Real WPF's own accessibility model (`System.Windows.Automation.Peers.
  DataGridAutomationPeer` et al., present in the linked `ext/wpf` submodule) is
  COM/UIA-based and was judged too costly to bridge (~36 call sites) relative to
  its priority so far.

**Attempted and confirmed not directly reusable**: tried linking WCT v7's
`DataGridAutomationPeer.cs` straight into this project's `.csproj` (the same trick
that works for `ext/wpf` files) to see whether it could be adapted cheaply. It
cannot — the build produced 60+ compile errors, none fixable by adding `using`
aliases:
- WCT targets `Windows.UI.Xaml.Automation.*` (old UWP namespace); this project
  only has `Microsoft.UI.Xaml.*` (WinUI3/Uno) — none of `IGridProvider`,
  `IScrollProvider`, `ISelectionProvider`, `ITableProvider`, `AutomationPeer`,
  `AutomationControlType`, `PatternInterface`, `ScrollAmount`,
  `IRawElementProviderSimple`, etc. exist in this build.
- The peer constructors take WCT's *own* `DataGrid`/`DataGridRow`/`DataGridColumn`
  types (`Microsoft.Toolkit.Uwp.UI.Controls.DataGrid`), not
  `System.Windows.Controls.DataGrid`, and reach into WCT-internal helper
  namespaces (`Microsoft.Toolkit.Uwp.UI.Controls.DataGridInternals`,
  `Microsoft.Toolkit.Uwp.Utilities`) that don't exist here at all.

Unlike the `ext/wpf` linking pattern (which works because this project's shim
types are deliberately modeled 1:1 on real WPF's API), WCT v7's peers are an
independent design with no structural relationship to this project's types. Making
accessibility work here would require either (a) a genuine bridge of WPF's own
`System.Windows.Automation.Peers.DataGridAutomationPeer` family onto Uno's native
automation peer model, or (b) fresh peer classes written against this project's
`System.Windows.Controls.DataGrid`/`DataGridRow`/`DataGridCell`, using WCT v7's
peers purely as a *behavioral reference* (which UIA patterns to expose and how) —
not something either project makes free to reuse via linking.

### Editing

- **WCT v7**: its own `DataGridDataConnection` + `ListCollectionView`/
  `EnumerableCollectionView` (custom collection-view implementations under
  `CollectionViews/`) drive begin/commit/cancel edit.
- **Ours**: real WPF `CollectionView`/`ItemCollection`/`IEditableCollectionView`
  semantics, direct-list only (no `ItemsSource`-as-external-view bridging), but
  `AddNew`/new-item-placeholder/`CancelEdit` **are supported**, not stubbed out:
  `ItemCollection` (`src/LeXtudio.Windows/System.Windows/Controls/ItemCollection.cs`)
  implements `IEditableCollectionView`/`IEditableCollectionViewAddNewItem` for
  real — `CanAddNew` is `true` whenever a representative item type exists or the
  caller supplies one via `AddNewItem`, `CancelEdit` genuinely rolls back via
  `IEditableObject.CancelEdit()` when an edit is in progress (`CanCancelEdit`
  reports `false` only when nothing is being edited, matching real WPF, not as a
  permanent stub). `DataGrid`'s `CanUserAddRows` DP drives a real "*" new-item-
  placeholder row end-to-end: `DataGrid.cs`'s `EnsureShimNewItemPlaceholderState()`
  sets `Items.NewItemPlaceholderPosition`, and `DataGridCell.BeginEdit()` detects
  `ReferenceEquals(RowDataItem, CollectionView.NewItemPlaceholder)` and routes into
  `DataGrid.ShimBeginEditPlaceholder`, which calls `AddNew()`, fires
  `AddingNewItem`/`InitializingNewItem`, and lets the new row commit through the
  normal `CommitEdit` path — verified by a simulated-UI probe in
  `tests/DataGrid.IntegrationTestHost/MainPage.cs` ("add-new row: placeholder edit
  enters routed WPF add-new path"). Real cell editing verified under frozen
  columns and virtualization this session (`docs/session121.md`, frozen columns
  Slice 6), including root-causing and fixing a DataContext-propagation bug that
  was silently blocking bound cell **text** from ever resolving in
  programmatically-built grids (a genuine correctness fix, not scoped to any one
  feature).

### Column types

- **WCT v7**: `DataGridTextColumn`, `DataGridCheckBoxColumn`,
  `DataGridComboBoxColumn`, `DataGridTemplateColumn`. No hyperlink column.
- **Ours**: WPF's real column types, plus a working `DataGridHyperlinkColumn`
  (real binding + click-navigate; this project's `TextBlock` doesn't route pointer
  events through `Inlines` the way WPF's does, so it renders as a styled,
  `Tapped`-handled `TextBlock` rather than a true inline `Hyperlink` — see
  `docs/session121.md`'s hyperlink column section for why).

### Sorting / filtering

- **WCT v7**: header-click-driven sort via `DataGridColumnHeader.InvokeProcessSort`;
  no built-in filtering.
- **Ours**: real WPF `SortDescriptionCollection`/`ICollectionView.Sort` (works from
  code, not just header clicks), plus an optional filter extension
  (`DataGridExtensions`-style `IsAutoFilterEnabled`/`DataGridFilterColumn`) that is
  this project's own addition, not part of WPF's DataGrid — noted here for
  completeness, not as a WPF-parity point.

### Row details

- **WCT v7**: supported, via its own `DataGridDetailsPresenter`.
- **Ours**: real WPF `RowDetailsVisibilityMode`/`RowDetailsTemplateSelector`,
  including variable-height rows under virtualization (fixed this session — see
  above).

### Column header interaction (resize/reorder)

- **WCT v7**: has its own `DataGridColumnHeader`/`DataGridColumnHeaderInteractionInfo`
  for drag-resize and reorder.
- **Ours**: real WPF column-header drag-resize handler (linked upstream) plus a
  ported floating drag-header + drop-indicator overlay for column reorder, and
  Escape-to-cancel-resize (restores pre-drag widths) — see `docs/session121.md`,
  "Stub implementation" section, item 1.

## Summary

| Capability | WCT v7 | Ours |
|---|---|---|
| Virtualization | Custom, from scratch | Real WPF generator/panel model, extended for variable row heights |
| Grouping | Custom `DataGridRowGroupHeader` | Real WPF `GroupItem`/`CollectionViewGroup`, full `GroupStyle` API |
| Frozen columns | Custom `DataGridFrozenGrid` | Real WPF `DataGridCellsPanel` arrange math |
| **Accessibility / UI Automation** | **Complete** (own peer family) | **Not implemented** (largest gap) |
| Editing | Custom collection views | Real WPF `CollectionView`/`IEditableCollectionView`, incl. working AddNew/placeholder row |
| Column types | Text/CheckBox/ComboBox/Template | + working Hyperlink column |
| Sorting | Header-click only | Real `SortDescriptionCollection` (code-driven too) |
| Filtering | None built in | Optional add-on (not WPF-native) |
| Row details | Custom presenter | Real WPF, variable-height under virtualization |
| Column resize/reorder | Custom | Real WPF handler + floating drag header/drop indicator, Escape-cancel |

**Bottom line**: WCT v7 is a complete, native-UWP-first control with best-in-class
accessibility but a design independent of WPF. Ours is a from-source WPF transplant
with much deeper behavioral fidelity to real WPF everywhere *except* accessibility,
where it currently has nothing. The two codebases are not directly interoperable —
WCT v7's source (now vendored as `ext/wct-v7` for reference) can inform a future
accessibility implementation, but cannot be linked in as-is.
