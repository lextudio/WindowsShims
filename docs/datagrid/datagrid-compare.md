# DataGrid comparison: WCT v7 / WinUI.TableView / CommunityToolkit DataTable / this project's WPF-ported DataGrid

Four independent DataGrid-family implementations exist in the [Uno Platform](https://github.com/unoplatform/uno)/ [WinUI](https://github.com/microsoft/microsoft-ui-xaml) ecosystem.
This document compares all four: **WindowsCommunityToolkit v7's** `DataGrid`
(`Microsoft.Toolkit.Uwp.UI.Controls.DataGrid`, removed from the toolkit in v8),
**WinUI.TableView** (`WinUI.TableView.TableView`, a `ListView`-based grid from
the community), **CommunityToolkit DataTable** (`CommunityToolkit.WinUI.Controls.
DataTable`, a lightweight Panel-based column-alignment helper from
CommunityToolkit Labs-Windows), and **this repository's** `DataGrid`
(`System.Windows.Controls.DataGrid`, a real WPF source port running on Uno Platform/WinUI
via the shim types in this library).

They are **not variants of the same code** — they solve "give me a grid on UWP/WinUI"
in fundamentally different ways, and none is a superset of another. Understanding
*why* they differ architecturally explains every feature gap below.

## Sources

- **WCT v7**: `CommunityToolkit/WindowsCommunityToolkit` (archived) at
  `https://github.com/CommunityToolkit/WindowsCommunityToolkit`, path
  `Microsoft.Toolkit.Uwp.UI.Controls.DataGrid/`.
- **WinUI.TableView**: independent community project at
  `https://github.com/w-ahmad/WinUI.TableView` (MIT).
  Referenced here for architectural and feature comparison only.
- **CommunityToolkit DataTable**: `CommunityToolkit/Labs-Windows` (experimental) at
  `https://github.com/CommunityToolkit/Labs-Windows`, path
  `components/DataTable/`. Referenced
  here for comparison.
- **Ours**: real WPF source (`System.Windows.Controls.DataGrid` and friends) at
  `https://github.com/lextudio/wpf` (a `dotnet/wpf` fork), vendored as the
  `ext/wpf` submodule, `#if HAS_UNO`-guarded, supplemented by local `.uno.cs`
  bridge files in `src/LeXtudio.Windows/`.

## Fundamental architectural difference

| | WCT v7 `DataGrid` | WinUI.TableView | CommunityToolkit `DataTable` | This project's `DataGrid` |
|---|---|---|---|---|
| **Design origin** | Written from scratch, specifically for UWP | Written from scratch, based on WinUI `ListView` | Panel-based ListView column-alignment helper (experimental Labs) | Real WPF `System.Windows.Controls.DataGrid` source, ported to run on Uno/WinUI |
| **Target framework** | `uap10.0.17763` (native UWP `Windows.UI.Xaml`) | `net8.0+`, multi-target with WinUI/Uno | UWP + WinAppSdk + Uno (via CommunityToolkit Labs infrastructure) | `net10.0-desktop` / WinUI 3 via Uno |
| **Namespace** | `Microsoft.Toolkit.Uwp.UI.Controls.DataGrid` | `WinUI.TableView` | `CommunityToolkit.WinUI.Controls` | `System.Windows.Controls.DataGrid` (WPF's own namespace) |
| **Goal** | *A* DataGrid control usable in UWP apps | A ListView-based grid that inherits virtualization | Align columns between ListView header and item template without a full grid control | WPF's *actual source code*, linked into the build via shim types that map WPF types onto WinUI/Uno |
| **Base class tower** | Its own `Control`-derived hierarchy | `ListView` (inherits all ListView infrastructure) | `Panel` (layout-only; not a control with a template) | WPF's `ItemsControl → Selector → MultiSelector → DataGrid`, rebased onto WinUI via shim types |
| **Nature** | Full grid control | Full grid control | Lightweight layout helper for ListView columns | Full grid control |
| **Size** | ~38,700 lines across 76 files | ~18,500 lines across 88 C# + 10 XAML files | ~340 lines across 3 source files | ~5,200 local shim lines + ~23,700 linked-upstream WPF lines |
| **Virtualization model** | Custom realize/recycle loop | Inherited `ItemsStackPanel` from ListView | Inherited from ListView (host is the consumer's ListView) | Shimmed VirtualizingPanel + custom container recycling |
| **License** | MIT (.NET Foundation) | MIT (w-ahmad) | MIT (.NET Foundation / CommunityToolkit) | MIT (dotnet/wpf) |

Because our port links WPF's DataGrid source and compiles it against shim types instead
of real WPF assemblies, the project uses a **dual-source strategy**:
- **Linked from `ext/wpf` (real WPF source):** the DataGrid control itself
  (`DataGrid.cs`, `DataGridRow.cs`, `DataGridColumn.cs`, `DataGridCell.cs`,
  `DataGridCellsPanel.cs`, `DataGridColumnHeader.cs`, `SortDescriptionCollection.cs`)
  and the DataGrid-level orchestration logic (editing pipeline, selection, sorting,
  frozen-column arrange math, column-header drag-resize).
- **Local shims in `src/LeXtudio.Windows/`:** infrastructure types that have no
  WinUI equivalent — `VirtualizingPanel`/`VirtualizingStackPanel`,
  `ItemContainerGenerator`, `ItemCollection`, `CollectionView`,
  `CollectionViewGroup`, `GroupItem`, `GroupStyle` — plus partial-class extensions
  on the linked WPF types for Uno-specific template/rendering bridge code.

This means the **majority** of WPF documentation, StackOverflow answers, and existing
WPF-targeting code describing `System.Windows.Controls.DataGrid` behavior applies
directly to ours (the DataGrid-level API and behavior), but the lower-level
infrastructure (how containers are generated and recycled, how groups are built,
how collections are viewed) is a shimmed approximation that mirrors WPF's model
behaviorally rather than reusing its source code.

**However**, the shim layer is not invisible. Several areas deviate from real WPF
because the underlying platform (WinUI/Uno Platform) differs from WPF's:
- **UI Automation** — completely stubbed (the single largest gap; see below).
- **Style/theme system** — WPF's `System.Windows.Style` is shimmed over WinUI's
  `Microsoft.UI.Xaml.Style`, which has different precedence, lookup, and trigger rules.
- **Event routing** — WPF's `RoutedEvent` is shimmed onto WinUI's, with subtle
  differences in tunneling/bubbling and event-arg types.
- **Clipboard, drag-drop, input** — shimmed via `System.Windows` types that map to
  thin wrappers over WinUI equivalents; edge cases may diverge.
- **Hyperlink column** — renders as a styled `TextBlock` + `Tapped` handler rather
  than a true inline `Hyperlink` (documented in `docs/session121.md`).
- **DesiredSize binding bug** — a root-caused shim issue in `FrameworkElement`'s
  `DesiredSize` propagation that can cause incorrect layout when the DataGrid is
  consumed by parent layout code (tracked in `docs/session121.md` gap survey item 6).

So while "it's WPF's source" is the correct headline, the shim layer introduces a
handful of behavioral nuances that make it not quite a 1:1 substitute in every
detail. WCT v7's DataGrid, WinUI.TableView, and CommunityToolkit DataTable, despite
superficially similar naming, are independent designs and share even less.

**Important caveat about DataTable**: Unlike the other three, `CommunityToolkit.
WinUI.Controls.DataTable` is **not a grid control**. It is a `Panel` subclass
designed to be placed inside a `ListView.Header`, with a matching `DataRow` panel
in the `ItemTemplate`. It handles column-width alignment (star/absolute/auto) and
column resize via `ContentSizer` grippers, but has **no cell abstraction, no
editing, no selection, no sorting, no filtering**. It is more accurate to think
of it as "a column-width coordinator for ListView" than as a DataGrid alternative.
It is included here because its name (`DataTable`) invites comparison, but
architecturally it occupies a completely different niche from the other three.

## Feature-by-feature comparison

### DataTable as a special case

`CommunityToolkit.WinUI.Controls.DataTable` is **not a grid control** and many standard
grid features (editing, selection, sorting, filtering, grouping, virtualization, UI
automation) do not apply to it — they come from whichever `ListView`/`ItemsControl`
hosts it. Because it does not implement these features itself, it is not included in
the individual feature sections below. Its relevant capabilities are summarized here
once instead of repeated as "N/A" in every section:

- **Column sizing**: `DesiredWidth` (GridLength — Star/Absolute/Auto), reused from
  `Grid`'s well-understood model rather than a custom column-size DSL. Auto-sizing
  is currently header-only (a TODO notes row-content measurement as future work).
- **Column resize**: via `ContentSizer` gripper drag (from
  `CommunityToolkit.WinUI.Controls.Sizers`). Drag converts the column to absolute width.
- **Row virtualization**: inherited from the host `ListView`/`ItemsStackPanel`.
- **Grouping**: inherited from the host `ItemsControl`.
- **TreeView support**: `DataRow.MeasureOverride` handles tree indentation for
  `HeaderedTreeView` integration.
- **Hybrid mode**: `DataRow` can detect a `Grid` header (no `DataTable` needed)
  and align to its `ColumnDefinitions`.
- **Size**: ~340 lines across 3 source files (`DataTable.cs` 168, `DataColumn.cs` 127,
  `DataRow.cs` 227). Experimental (Labs-Windows) status.
- **Nothing built-in**: no cells (just panel children), no editing, no selection, no
  sort, no filter, no clipboard, no accessibility peers, no validation.

### Virtualization

- **WCT v7**: `DataGridRowsPresenter` is a plain `Panel` (not a `VirtualizingPanel`)
  with a hand-written realize/recycle loop built specifically for this control.
  Nothing about it is shared with any other WinUI `ItemsControl`.
- **WinUI.TableView**: **inherits UI virtualization from `ListView`**. Every row
  is a `ListViewItem` (subclassed as `TableViewRow`), recycled by the standard
  `ItemsStackPanel`. This is zero-effort, proven infrastructure, but it also
  inherits ListView's focus behavior, selection model, and visual states. No
  data virtualization beyond `ISupportIncrementalLoading` pass-through.
- **Ours**: shimmed virtualization — a custom `VirtualizingPanel` (shim over WinUI's
  panel infrastructure) plus container-recycling logic that mirrors WPF's
  generator/panel model behaviorally, without directly linking WPF's
  `ItemContainerGenerator` source. Extended with variable-row-height support (a per-index
  height cache + cumulative-offset layout, for `RowDetails` rows that are taller
  than others) — see `docs/session121.md`, gap survey item 8.

### Grouping

- **WCT v7**: purpose-built for the grid — its own `DataGridRowGroupHeader` class
  and `ICollectionViewGroup` model, native UWP grouping UI designed specifically for
  this control.
- **WinUI.TableView**: **not supported**. No `CollectionGroups` implementation;
  `CollectionGroups` property returns `null`. This is the single largest feature
  gap vs. the other two projects.
- **Ours**: real WPF has *no* DataGrid-specific group-header class at all — WPF's
  DataGrid reuses the generic `ItemsControl`/`GroupItem`/`CollectionViewGroup`
  machinery shared with `ListView` etc., and that's exactly what our port does too.
  Extended to full `GroupStyle` API coverage: `HeaderTemplate`/
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
- **WinUI.TableView**: separate `FrozenCellsPanel` and `ScrollableCellsPanel`
  (both plain `StackPanel`) inside `TableViewRowPresenter`. Frozen columns are
  rendered in the frozen panel; scrollable columns in the scrollable panel.
  Horizontal scrolling is achieved by `ArrangeOverride`-translating the scrollable
  panel, **not** by setting per-cell offsets. This is a simpler, more direct
  approach than WPF's `DataGridCellsPanel` arrange math.
- **Ours**: ports WPF's real `DataGridCellsPanel.ArrangeOverride` frozen/non-frozen
  clip-and-offset math — the actual WPF frozen-column algorithm, not a
  reimplementation of the visual effect. Verified working under virtualization,
  with real cell editing and selection, and correct behavior across boundary-column
  resize — see `docs/session121.md`, frozen columns Slices 1-6.

### Accessibility / UI Automation

- **WCT v7**: ships a **complete** automation-peer family purpose-built for its own
  types — `DataGridAutomationPeer` (987 lines, implements `IGridProvider`,
  `IScrollProvider`, `ISelectionProvider`, `ITableProvider`),
  `DataGridCellAutomationPeer`, `DataGridRowAutomationPeer`,
  `DataGridColumnHeaderAutomationPeer`, `DataGridColumnHeadersPresenterAutomationPeer`,
  `DataGridDetailsPresenterAutomationPeer`, `DataGridGroupItemAutomationPeer` (576
  lines), `DataGridItemAutomationPeer` (534 lines), `DataGridRowGroupHeaderAutomationPeer`,
  `DataGridRowHeaderAutomationPeer`, `DataGridRowsPresenterAutomationPeer` — 3,090
  lines total, all targeting native UWP `Windows.UI.Xaml.Automation.Peers`/`.Provider`.
- **WinUI.TableView**: ships **5 automation peer classes** targeting
  `Microsoft.UI.Xaml.Automation.Peers`: `TableViewAutomationPeer`,
  `TableViewRowAutomationPeer`, `TableViewCellAutomationPeer`,
  `TableViewColumnHeaderAutomationPeer`, `TableViewRowHeaderAutomationPeer`.
  Not as extensive as WCT v7's family (no grid/scroll/selection/table pattern
  providers), but covers the main element types that screen readers encounter.
- **Ours**: **not implemented**. This is the single largest, most-repeatedly-flagged
  gap across every session of this DataGrid port (`docs/DATAGRID.md`,
  `docs/session121.md`'s gap survey item 2). `AutomationPeer.FromElement` is
  deliberately stubbed to return `null` and `ListenerExists` returns `false` — an
  honest "not supported" rather than a silently-broken partial implementation.
  Real WPF's own accessibility model (`System.Windows.Automation.Peers.
  DataGridAutomationPeer` et al., present in the linked `ext/wpf` submodule) is
  COM/UIA-based and was judged too costly to bridge (~36 call sites) relative to
  its priority so far.

**Attempted and confirmed not directly reusable** (WCT v7 and TableView peers):
tried linking WCT v7's `DataGridAutomationPeer.cs` straight into this project's
`.csproj` (the same trick that works for `ext/wpf` files) to see whether it could
be adapted cheaply. It cannot — the build produced 60+ compile errors, none fixable
by adding `using` aliases:
- WCT targets `Windows.UI.Xaml.Automation.*` (old UWP namespace); this project
  only has `Microsoft.UI.Xaml.*` (WinUI3/Uno Platform) — none of `IGridProvider`,
  `IScrollProvider`, `ISelectionProvider`, `ITableProvider`, `AutomationPeer`,
  `AutomationControlType`, `PatternInterface`, `ScrollAmount`,
  `IRawElementProviderSimple`, etc. exist in this build.
- The peer constructors take WCT's *own* `DataGrid`/`DataGridRow`/`DataGridColumn`
  types (`Microsoft.Toolkit.Uwp.UI.Controls.DataGrid`), not
  `System.Windows.Controls.DataGrid`, and reach into WCT-internal helper
  namespaces (`Microsoft.Toolkit.Uwp.UI.Controls.DataGridInternals`,
  `Microsoft.Toolkit.Uwp.Utilities`) that don't exist here at all.

WinUI.TableView's peers are slightly closer (same `Microsoft.UI.Xaml` namespace,
peers inherit from `FrameworkElementAutomationPeer` which does exist in this
project), but they take TableView's own types (`WinUI.TableView.TableViewRow`,
`WinUI.TableView.TableViewCell`, etc.) — not
`System.Windows.Controls.DataGridRow`/`DataGridCell`. The same causal gap applies:
different type system, different base classes, different visual-tree structure.

Unlike the `ext/wpf` linking pattern (which works because this project's shim
types are deliberately modeled 1:1 on real WPF's API), both WCT v7's and
WinUI.TableView's peers are independent designs with no structural relationship
to this project's types. Making accessibility work here would require either (a)
a genuine bridge of WPF's own `System.Windows.Automation.Peers.DataGridAutomationPeer`
family onto Uno's native automation peer model, or (b) fresh peer classes written
against this project's `System.Windows.Controls.DataGrid`/`DataGridRow`/
`DataGridCell`, using either project's peers purely as a *behavioral reference*
(which UIA patterns to expose and how) — not something either project makes free
to reuse via linking.

### Editing

- **WCT v7**: its own `DataGridDataConnection` + `ListCollectionView`/
  `EnumerableCollectionView` (custom collection-view implementations under
  `CollectionViews/`) drive begin/commit/cancel edit.
- **WinUI.TableView**: in-place editing via `BeginCellEditing`/`EndCellEditing`,
  swapping between display `Element` and `EditingElement` per cell. Editing
  lifecycle events: `BeginningEdit`, `PreparingCellForEdit`, `CellEditEnding`,
  `CellEditEnded`. Does **not** support add-new row (no new-item placeholder),
  delete row, or validation (`INotifyDataErrorInfo`/`IEditableObject`).
- **Ours**: WPF's DataGrid editing pipeline (linked from `ext/wpf` — `DataGrid.BeginEdit`,
  `CommitEdit`, `CancelEdit` in `DataGrid.cs`) drives the lifecycle, built on top of
  shimmed infrastructure (`ItemCollection` in `src/LeXtudio.Windows/System.Windows/
  Controls/ItemCollection.cs` implementing `IEditableCollectionView`). Direct-list
  only (no `ItemsSource`-as-external-view bridging), but
  `AddNew`/new-item-placeholder/`CancelEdit` **are fully supported**:
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
  columns and virtualization (`docs/session121.md`, frozen columns Slice 6),
  including root-causing and fixing a DataContext-propagation bug that was silently
  blocking bound cell **text** from ever resolving in programmatically-built grids.

### Column types

- **WCT v7**: `DataGridTextColumn`, `DataGridCheckBoxColumn`,
  `DataGridComboBoxColumn`, `DataGridTemplateColumn`. No hyperlink column.
- **WinUI.TableView**: widest column-type selection of the three: `Text`, `Number`,
  `Date`, `Time`, `CheckBox`, `ComboBox`, `ToggleSwitch`, `Hyperlink`, `Template`.
  Includes typed columns (`TableViewNumberColumn` with `NumberBox`,
  `TableViewDateColumn` with `TableViewDatePicker`, `TableViewTimeColumn` with
  `TableViewTimePicker`, `TableViewToggleSwitchColumn`) that the other two projects
  don't have.
- **Ours**: WPF's real column types, plus a working `DataGridHyperlinkColumn`
  (real binding + click-navigate; this project's `TextBlock` doesn't route pointer
  events through `Inlines` the way WPF's does, so it renders as a styled,
  `Tapped`-handled `TextBlock` rather than a true inline `Hyperlink` — see
  `docs/session121.md`'s hyperlink column section for why).

### Sorting / filtering

- **WCT v7**: header-click-driven sort via `DataGridColumnHeader.InvokeProcessSort`;
  no built-in filtering.
- **WinUI.TableView**: multi-column sort via its own `CollectionView` (internal)
  with `SortDescription` list; header-click toggles Ascending/Descending/None.
  **Excel-like column filter flyout** with checkboxes for each unique value,
  search box, and count display — the most full-featured filtering UI of the
  three projects. Custom `ColumnFilterHandler` drives filter state.
- **Ours**: real WPF `SortDescriptionCollection`/`ICollectionView.Sort` (works from
  code, not just header clicks), plus an optional filter extension
  (`DataGridExtensions`-style `IsAutoFilterEnabled`/`DataGridFilterColumn`) that is
  this project's own addition, not part of WPF's DataGrid — noted here for
  completeness, not as a WPF-parity point.

### Row details

- **WCT v7**: supported, via its own `DataGridDetailsPresenter`.
- **WinUI.TableView**: supported, with `RowDetailsTemplate` and visibility modes
  (Visible, VisibleWhenSelected, VisibleWhenExpanded). Includes
  `AreRowDetailsFrozen` property to keep details visible during horizontal scroll.
- **Ours**: linked WPF source `RowDetailsVisibilityMode`/`RowDetailsTemplateSelector`,
  plus a shim extension for variable-height rows under virtualization — see
  `docs/session121.md`, gap survey item 8.

### Column header interaction (resize/reorder)

- **WCT v7**: has its own `DataGridColumnHeader`/`DataGridColumnHeaderInteractionInfo`
  for drag-resize and reorder.
- **WinUI.TableView**: drag-to-resize column headers (drag header edge), drag-to-reorder
  with visual drop indicator. Supported at the `TableView` API level.
- **Ours**: real WPF column-header drag-resize handler (linked upstream) plus a
  ported floating drag-header + drop-indicator overlay for column reorder, and
  Escape-to-cancel-resize (restores pre-drag widths) — see `docs/session121.md`,
  "Stub implementation" section, item 1.

## Features unique to one project

Each project has capabilities the other three lack:

**CommunityToolkit DataTable only:**
- **Panel-based column layout** — not a control at all, a `Panel` subclass for
  ListView header-row column alignment with `GridLength` size model
  (Star/Absolute/Auto).
- **Grid header hybrid mode** — detects native `Grid` column definitions in the
  header and aligns row children to them, no `DataTable` panel needed.
- **No cell abstraction** — children are direct panel children, not cells.
- **TreeView integration** — `DataRow.MeasureOverride` handles indentation for
  `HeaderedTreeView`.
- **Experimental Labs status** — single-digit source files, ~340 lines, no
  accessibility, no editing, no selection, no sorting, no filtering.

**WCT v7 only:**
- `DataGridGroupItemAutomationPeer` / `DataGridItemAutomationPeer` / `DataGridDetailsPresenterAutomationPeer`
  — deepest automation-peer tree of the three.
- `DataGridFrozenGrid` (`Grid` subclass) frozen-column approach.

**WinUI.TableView only:**
- **Clipboard paste** (Ctrl+V, tab-delimited, into current cell) — `TableView.Paste.cs`.
- **CSV export** (built-in file save picker, `ExportAllToCSV` / `ExportSelectedToCSV`).
- **Drag-to-select cells** (mouse-drag across cells with visual rectangle + auto-scroll).
- **Excel-like column filter flyout** (checkboxes, search, counts).
- **Typed columns**: `NumberColumn` (NumberBox), `DateColumn`/`TimeColumn` (pickers),
  `ToggleSwitchColumn`.
- **Conditional cell styling** (predicate-based styles at column and TableView level).
- **Compact mode** (Fluent Compact sizing).
- **Localization** via `TableViewLocalizedStrings`.
- **Native AOT compatibility** (compiled value getters/setters via expression trees).
- **Corner button** (SelectAll or Options flyout).
- **Frozen row details** (`AreRowDetailsFrozen`).

**Ours only:**
- **WPF source fidelity** — the DataGrid-level orchestration logic (editing,
  selection, sorting, frozen-column arrange, column-header drag) runs WPF's actual
  linked source; lower-level infrastructure (virtualizing panel, container generator,
  collection view, group types) is shimmed locally to mirror WPF's behavior. See
  the architectural dual-source description above for details.
- **Add-new row** with full `IEditableCollectionViewAddNewItem` / new-item-placeholder
  end-to-end.
- **Row validation** (`RowValidationRules` via `BindingGroup`).
- **`IEditableObject`** commit/cancel transactions.
- **Grouping** with shimmed `CollectionViewGroup` tree, expand/collapse, full `GroupStyle` API.
- **Variable row heights under virtualization** (per-index height cache + cumulative offsets).
- **Escape-to-cancel-resize** (restores pre-drag column widths).

## Summary

| Capability | WCT v7 | WinUI.TableView | CommunityToolkit DataTable | Ours |
|---|---|---|---|---|
| What it is | Full grid control | Full grid control | ListView column-layout helper (Panel) | Full grid control |
| Base class | `Control` | `ListView` | `Panel` | `ItemsControl → DataGrid` (WPF source) |
| Size | ~38,700 lines, 76 files | ~18,500 lines, 98 files | ~340 lines, 3 source files | ~5,200 shim + ~23,700 WPF lines |
| Virtualization | Custom, from scratch | Inherited from ListView (`ItemsStackPanel`) | Inherited from host ListView | Shimmed VirtualizingPanel + custom recycling, variable row heights |
| Grouping | Custom `DataGridRowGroupHeader` | **Not supported** | Inherited from host | Shimmed `GroupItem`/`CollectionViewGroup`, full `GroupStyle` API |
| Frozen columns | Custom `DataGridFrozenGrid` | Separate frozen/scrollable panels | **N/A** (not a grid) | Real WPF `DataGridCellsPanel` arrange math |
| **Accessibility / UI Automation** | **Complete** (own peer family, 3,090 lines) | **5 automation peers** | **None** (Panel has no peers) | **Not implemented** (largest gap) |
| Editing | Custom collection views | In-place element swap | **N/A** (no cells) | WPF editing pipeline (linked) + shimmed `ItemCollection`/`CollectionView`, incl. AddNew/placeholder/validation |
| Column types | Text/CheckBox/ComboBox/Template | Text/Number/Date/Time/CheckBox/ComboBox/ToggleSwitch/Hyperlink/Template | **N/A** (direct panel children) | WPF real types + Hyperlink |
| Sorting | Header-click only | Multi-column, `SortDescription` list | **N/A** (no sort concept) | Real `SortDescriptionCollection` (code-driven too) |
| Filtering | None built in | **Excel-like filter flyout** | **N/A** | Optional add-on (not WPF-native) |
| Row details | Custom presenter | Supported, `AreRowDetailsFrozen` | **N/A** | WPF source (linked) + shim variable-height extension |
| Column resize/reorder | Custom | Drag resize + reorder | Drag resize via `ContentSizer` (no reorder) | Real WPF handler + floating drag header/drop indicator, Escape-cancel |
| Clipboard | Copy only | **Copy + paste + CSV export** | **N/A** | Copy only |
| Row headers | Supported | Supported (template + selector) | **N/A** | Supported (glyph-based) |
| Validation | Limited | **Not supported** | **N/A** | `IDataErrorInfo` + `RowValidationRules` + `IEditableObject` |
| TreeView support | **N/A** | **N/A** | `DataRow` indents tree items | **N/A** |
| Status | Archived (removed in v8) | Active community project | Experimental (Labs-Windows) | Active port (WPF source) |

**Bottom line**: WCT v7 is a complete, native-UWP-first control with best-in-class
accessibility but a design independent of WPF. WinUI.TableView is a ListView-based
grid with the best column-type variety and Excel-like filtering, but it inherits
ListView's constraints (no grouping, no add-new-row, no validation). CommunityToolkit
DataTable is a fundamentally different thing — not a grid at all but a Panel that
aligns ListView columns, with the unique advantage of being too small and simple
to fail (~340 lines), and interesting hybrid/Grid-header-detection ideas. Ours is a
dual-source port: the DataGrid-level logic (editing, selection, sorting, frozen
columns, column-header drag) is linked from real WPF source, while the
infrastructure (virtualizing panel, container generator, collection view, group
types) is locally shimmed to mirror WPF's behavior — with gaps in accessibility
(stubbed) and shim-induced nuances in theme resolution, event routing, and input.
The four codebases are not directly interoperable — none of the others' source can be linked
in as-is — but all can serve as behavioral references for future work (accessibility
patterns from WCT v7, column-type ideas and filter-flyout UX from TableView,
column-layout ideas from DataTable).
