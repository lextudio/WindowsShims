# DataGrid Port Plan

This file tracks the WPF `System.Windows.Controls.DataGrid` source-first port to
`LeXtudio.Windows`, following the RichTextBox pattern: link upstream WPF source
when it can compile cleanly, add narrow Uno bridges where the WPF framework
contract is missing, and avoid local rewrites until a source file proves too
coupled for the current milestone.

## Current Baseline

- Project: `src/LeXtudio.Windows/LeXtudio.Windows.csproj`
- Upstream WPF source root:
  `ext/wpf/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls`
- First build target: `net10.0-desktop`
- Session 15 build/test status: green for the low-coupling DataGrid surface,
  binding bridge, local column/bound-column/text/checkbox/template-column/
  combo-box-column shells, cell/row shells, owner/column collection shell, the
  full linked event-args/notification/clipboard-helper layer, linked
  `DataGridCellInfo` over the `ItemInfo` bridge, the linked new-item event
  args, the linked cell-selection collection stack, the rung-5/6 leaves, and
  the spine layer-1 bridges (shim `ItemsControl` virtuals, linked
  `BooleanBoxes`, untargeted `BindingExpression` bridge,
  `DynamicValueConverter` bridge, `BuildInfo`/attribute shims).
- Mechanism note (discovered in session 15): `ext/wpf` is a patched fork that
  uses `#if !HAS_UNO` guards inside upstream files (for example `Window.cs`,
  `AdornerLayer`, `TextBoxBase`). Fork-patching is an established third
  option alongside direct linking and local bridges.
- Control-shell direction (decided in session 12): pursue the linked
  `DataGrid.cs` path with guarded internals, growing bridges rung by rung,
  rather than a local shell over Uno `ListView`/`Grid`.

## Status Key

- `linked-upstream`: upstream WPF source compiles directly or with narrow
  bridges.
- `local-bridge`: compatibility layer used by linked upstream files.
- `blocked`: known required item that cannot be enabled yet.
- `deferred`: known WPF source outside the current milestone.

## Porting Strategy

DataGrid is a wider surface than RichTextBox. The control itself is not a good
first source file because `DataGrid.cs` immediately pulls on WPF
`ItemsControl`, `MultiSelector`, item containers, virtualization, collection
views, automation peers, command routing, and template parts. The first phase is
therefore an API-foundation pass: bring over independent value types and enums
that downstream code can compile against while we map the larger container
spine.

## Minimum DataGrid Spine

| Area | WPF source / family | Current WindowsShims source | Status | Notes / blocker |
|---|---|---|---|---|
| Sizing model | `DataGridLength`, `DataGridLengthUnitType`, `DataGridLengthConverter` | Linked WPF source plus `MS.Internal/DataGridSizingShims.cs` | `linked-upstream` / `local-bridge` | `DoubleUtil` and `PixelUnit` are bridged locally with the converter subset needed by these files. |
| Public mode enums | `DataGridClipboardCopyMode`, `DataGridEditAction`, `DataGridEditingUnit`, `DataGridGridLinesVisibility`, `DataGridHeadersVisibility`, `DataGridRowDetailsVisibilityMode`, `DataGridSelectionMode`, `DataGridSelectionUnit` | Linked WPF source | `linked-upstream` | Compiles independently. |
| Resource strings | `DataGridLength_Infinity`, `DataGridLength_InvalidType` | `System.Windows/Documents/SR.cs` | `local-bridge` | Added as invariant strings to match the existing SR shim approach. |
| Sizing tests | `DataGridLength` construction/conversion behavior | `LeXtudio.Windows.Tests/DataGridLengthTests.cs` | `linked-upstream` / verified | Covers pixels, star sizing, descriptive values, physical units, and invalid infinity values. |
| Binding bridge | `System.Windows.Data.BindingBase`, `Binding`, `PropertyPath`, mode/update enums | `System.Windows/Data/Binding.cs` | `local-bridge` | Stores WPF-shaped binding state and adapts to Uno `Microsoft.UI.Xaml.Data.Binding` at runtime. Plain NUnit cannot instantiate WinUI binding without a dispatcher, so tests cover state/mapping/converter adapter only. |
| Binding operations | `System.Windows.Data.BindingOperations` | `System.Windows/Data/Binding.cs` | `local-bridge` | WPF facade delegates `SetBinding` to Uno `Microsoft.UI.Xaml.Data.BindingOperations` and uses `ClearValue` for `ClearBinding`. |
| Column base | `DataGridColumn` | `System.Windows/Controls/DataGridColumn.cs` | `local-shell` | Local partial shell avoids the Uno generator partial collision that blocks direct upstream source-linking. Constructor is dispatcher-bound because it derives from WinUI `DependencyObject`; plain tests verify type surface only. |
| Bound column base | `DataGridBoundColumn`, `DataGridHelper` subset | `System.Windows/Controls/DataGridBoundColumn.cs` | `local-shell` | Adds binding/style API surface and binding/style application helpers. Property-engine coercion remains deferred because current WPF-style coercion shims are no-ops. |
| Cell shell | `DataGridCell` | `System.Windows/Controls/DataGridCell.cs` | `local-shell` | Minimal `ContentControl` shell with `IsEditing`, `Column`, and `BuildVisualTree()` for bound-column refresh paths. |
| Clipboard cell content | `DataGridClipboardCellContent` | Linked WPF source | `linked-upstream` | Enabled after the local column shell landed. |
| Text column | `DataGridTextColumn` | `System.Windows/Controls/DataGridTextColumn.cs` | `local-shell` | Display generation binds Uno `TextBlock.Text`; editing generation uses explicit Uno `TextBox` and minimal select-all edit prep. WPF font-property syncing and caret-placement details remain deferred. |
| Checkbox column | `DataGridCheckBoxColumn` | `System.Windows/Controls/DataGridCheckBoxColumn.cs` | `local-shell` | Generates/binds Uno `CheckBox`, exposes `IsThreeState`, and performs minimal edit prep. WPF input-triggered begin edit and hit-test toggling remain deferred. |
| Template column | `DataGridTemplateColumn` | `System.Windows/Controls/DataGridTemplateColumn.cs` | `local-shell` | Exposes cell/editing templates and selectors over WinUI `DataTemplate`/`DataTemplateSelector`; generates a WinUI `ContentPresenter` when a template/selector exists. WPF sort coercion remains deferred. |
| Column event args | `DataGridColumnEventArgs`, `DataGridSortingEventArgs`, `DataGridSortingEventHandler`, `DataGridColumnReorderingEventArgs`, `DataGridAutoGeneratingColumnEventArgs`, `DataGridCellClipboardEventArgs` | Linked WPF source | `linked-upstream` | Session 11 replaced the session-8 local shells with direct links; `ItemPropertyInfo` comes from linked WindowsBase `IItemProperties.cs`. |
| Row/edit event args | `DataGridRowEventArgs`, `DataGridBeginningEditEventArgs`, `DataGridCellEditEndingEventArgs`, `DataGridPreparingCellForEditEventArgs`, `DataGridRowDetailsEventArgs`, `DataGridRowEditEndingEventArgs`, `DataGridRowClipboardEventArgs` | Linked WPF source | `linked-upstream` | Enabled by the minimal local `DataGridRow` shell. |
| Row shell | `DataGridRow` | `System.Windows/Controls/DataGridRow.cs` | `local-shell` | Minimal `Control` shell (`Item`, `IsEditing`, internal owner) to unblock row/edit event args. Container generation, details, headers, and visual states remain deferred. |
| Notification target | `DataGridNotificationTarget` | Linked WPF source | `linked-upstream` | Session 11 replaced the session-9 local copy (byte-identical enum). |
| Clipboard/format helpers | `DataGridClipboardHelper`, `DataGridItemAttachedStorage`, `DataGridHeadersVisibilityToVisibilityConverter` | Linked WPF source | `linked-upstream` | Clipboard helper needed `DataFormats.CommaSeparatedValue` added to the clipboard shim; converter compiles against the WPF-shaped `IValueConverter` shim. |
| Item property metadata | `IItemProperties`, `ItemPropertyInfo` | Linked WPF source (WindowsBase) | `linked-upstream` | Self-contained; enables the internal auto-generation event args constructor. |
| New-item pipeline args | `AddingNewItemEventArgs`, `InitializingNewItemEventArgs`, `InitializingNewItemEventHandler`, `IProvideDataGridColumn` | Linked WPF source | `linked-upstream` | Leaf files from the session-12 probe; no new bridges needed. |
| Cell identity | `DataGridCellInfo` | Linked WPF source | `linked-upstream` | Compiles over the local `ItemsControl.ItemInfo` bridge, `DataGrid.NewItemInfo`, and `DataGridCell` row-owner internals. |
| Item info bridge | `ItemsControl.ItemInfo`, `ItemsControl.EqualsEx` | `System.Windows/Controls/ItemsControlItemInfo.cs` | `local-bridge` | Item/container/index equality subset; WPF sentinel containers and generator `Refresh` are omitted until virtualization paths are linked. |
| Thumb drag args | `DragStartedEventArgs`, `DragDeltaEventArgs`, `DragCompletedEventArgs` + handlers | Linked WPF source | `linked-upstream` | Linked over a minimal local `Thumb` shell that carries the three drag routed-event identities; Thumb input behavior is not implemented. |
| Resource keys | `ResourceKey`, `ComponentResourceKey` | Linked WPF source | `linked-upstream` | Needed a `MarkupExtension` shim, a `ComponentResourceKeyConverter` shim, and two SR strings. |
| Container tracking | `ContainerTracking<>` | Linked WPF source | `linked-upstream` | Self-contained linked-list node used by row/cell container tracking. |
| Uncommon field bridge | `UncommonField<>` | `MS.Internal/UncommonField.cs` | `local-bridge` | `ConditionalWeakTable`-backed; WPF's effective-value-table storage is not reachable on WinUI. |
| Focus direction | `FocusNavigationDirection` | `System.Windows/Input/FocusNavigationDirection.cs` | `local-bridge` | Enum shim mirroring WPF member order. |
| Known boxes | `BooleanBoxes` | Linked WPF source (WindowsBase) | `linked-upstream` | Replaced the local nested-class shim; now a real `MS.Internal.KnownBoxes` namespace as upstream files expect. |
| Spine virtuals | `ItemsControl` `OnItemsChanged`/`OnItemsSourceChanged`/`Prepare`/`ClearContainerForItemOverride`/`AdjustItemInfoOverride`/`OnInitialized`/`OnIsKeyboardFocusWithinChanged` | `System.Windows/Controls/ItemsControlSpine.cs` | `local-bridge` | No-op virtual hooks Selector/MultiSelector override; real behavior waits on item-container generation. |
| Binding expression bridge | `BindingExpressionBase`, `BindingExpression`, `BindingExpressionUncommonField`, `DynamicValueConverter`, `Binding.XPath` | `System.Windows/Data/BindingExpression.cs`, `MS.Internal/Data/DataBridges.cs` | `local-bridge` | Untargeted expression walks dotted CLR property paths via reflection for the selector `SelectedValue` paths; XML/indexer/property-engine evaluation not supported. Converter uses component-model converters with `UnsetValue` failure contract. |
| Spine attribute/info shims | `AttachedPropertyBrowsableForChildrenAttribute`, `MS.Internal.PresentationFramework.BuildInfo` | local shims | `local-bridge` | Designer metadata flattened to `Attribute`; BuildInfo constants mirror `RefAssemblyAttrs.cs` (not linked because it carries assembly-level attributes). |
| Selector spine | `Selector`, `MultiSelector` | Not enabled | `blocked` | Layer-2 probe: 79 unique contracts — rich `ItemsControl` surface, `ItemCollection`-as-`CollectionView` currency, property-engine internals (`SetCurrentValueInternal`, `EffectiveValueEntry`, `DependencyPropertyKey` set paths), automation peers, `ItemInfo` sentinels, `SystemXmlHelper`. Needs fork-patching or a local spine bridge decision. |
| Cell selection collections | `SelectedCellsCollection`, `VirtualizedCellInfoCollection`, `SelectedCellsChangedEventArgs`/`Handler` | Linked WPF source | `linked-upstream` | Compiles over guarded `DataGrid` internals (`Items` item list, `ItemInfoFromIndex`, subset `OnSelectedCellsChanged`), four new SR strings, and `CoreDispatcher.VerifyAccess`/`CheckAccess` extensions. `DataGrid.SelectedCells` and `SelectedCellsChanged` are exposed on the shell. |
| Column owner/collection | `DataGrid`, `DataGridColumnCollection` | `System.Windows/Controls/DataGrid.cs`, `DataGridColumnCollection.cs` | `local-shell` | Adds WPF-shaped `Columns`, internal owner tracking, display-index lookup, and notification stubs. Full width redistribution, virtualization maps, sorting, selection, and item ownership remain deferred. |
| Combo box column | `DataGridComboBoxColumn` | `System.Windows/Controls/DataGridComboBoxColumn.cs` | `local-shell` | Exposes `SelectedItemBinding`/`SelectedValueBinding`/`TextBinding` with WPF effective-binding precedence and maps `ItemsSource`/`DisplayMemberPath`/`SelectedValuePath` onto Uno `ComboBox`. WPF `OnInput` drop-down opening, flow-direction caching, style keys, and sort-member coercion remain deferred. |
| Hyperlink column | `DataGridHyperlinkColumn` | Not enabled | `blocked` | Needs navigation/routed-command pieces (`Hyperlink` content binding, `OnExecutedRouted` style command plumbing). |
| Row/cell container behavior | upstream `DataGridRow.cs`, `DataGridCell.cs`, `DataGridCellsPanel`, presenters | Not enabled (local shells only) | `blocked` | Requires item container generation, virtualization, layout override parity, visual states, and automation support. |
| Control root | `DataGrid.cs` | Not enabled | `blocked` | Depends on WPF selector/items stack and should be attempted after the column and container contracts are cataloged. |

## Session Ladder

1. Establish independent API surface and compile green. Completed in session 1.
2. Add sizing tests and catalog first `DataGridColumn` dependency errors.
   Completed in session 2.
3. Add the WPF binding bridge and local `DataGridColumn` shell. Completed in
   session 3.
4. Add bound-column infrastructure: `BindingOperations`, `DataGridCell`, and a
   local `DataGridBoundColumn` shell. Completed in session 4.
5. Add a local `DataGridTextColumn` shell over Uno `TextBlock`/`TextBox`.
   Completed in session 5.
6. Add a local `DataGridCheckBoxColumn` shell over Uno `CheckBox`. Completed in
   session 6.
7. Add a local `DataGridTemplateColumn` shell over WinUI templates and
   `ContentPresenter`. Completed in session 7.
8. Add low-dependency column event args. Completed in session 8.
9. Add collection and notification types: `DataGridColumnCollection` and the
   minimal owner notification hooks it requires. Completed in session 9.
10. Enable remaining low-behavior concrete column types in this order:
   combo box/hyperlink columns as their dependencies become clear. Combo box
   column completed in session 10; hyperlink column remains queued behind
   command/navigation pieces.
11. Re-link pass: replace local event-args/notification shells with direct
   upstream links and pull in the leaf helper files
   (`DataGridClipboardHelper`, `DataGridItemAttachedStorage`,
   `ItemPropertyInfo`), unblocking row/edit event args with a minimal
   `DataGridRow` shell. Completed in session 11.
12. Control-shell milestone, linked path chosen: probe-link upstream
   `DataGrid.cs`, catalog its first-order contracts, and land the first rungs
   (new-item args, `ItemInfo` bridge, linked `DataGridCellInfo`). Completed in
   session 12.
13. Climb the remaining linked-`DataGrid.cs` ladder recorded in the session-12
   probe results: cell-selection collections (completed in session 13),
   rung-5/6 leaves and the spine probe (completed in session 14), spine
   layer-1 bridges and layer-2 catalog (completed in session 15), selector
   spine enablement (fork-patch vs local bridge decision pending),
   header/presenter shells, validation/binding-group bridges, then the
   control root itself.
14. Bring row/cell container behavior and presenters online only when the
   control shell has tests proving the owner/column/item contracts.

## Test Plan

- Always run `dotnet build WindowsShims/src/LeXtudio.Windows/LeXtudio.Windows.csproj`
  after each source-link batch.
- Run `dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop`
  after changes to the DataGrid sizing bridge.
- Once a control shell exists, add a small runtime sample/test with static items,
  explicit columns, and no virtualization before trying auto-generation,
  editing, or selection.

## Probe Results

### Session 2: `DataGridColumn.cs`

Temporary source-linking `DataGridColumn.cs` produced 15 compile errors before
the link was removed to keep the baseline green. The first blocker is a
`DataGridColumn` partial collision from generated/reference source rather than
a hand-written WindowsShims file. After that, the first-order missing contracts
are:

- DataGrid owner/control contract: `DataGrid`
- Row/cell containers: `DataGridRow`, `DataGridCell`
- Clipboard args: `DataGridCellClipboardEventArgs`
- Notification flags: `DataGridNotificationTarget`
- Data binding surface: `BindingBase`
- Input event base: `InputEventArgs`
- Item-property metadata: `ItemPropertyInfo`

This argues for a column-shell session before source-linking the full upstream
column base.

### Session 3: binding bridge and local column shell

Session 3 followed the column-shell path. `System.Windows.Data.BindingBase` is a
local WPF-shaped bridge rather than a WPF source port; it can convert to Uno's
`Microsoft.UI.Xaml.Data.Binding` in runtime contexts. `DataGridColumn` is a
local partial `DependencyObject` shell with core properties (`Header`, `Width`,
`MinWidth`, `MaxWidth`, `Visibility`, `IsReadOnly`, `DisplayIndex`,
`ClipboardContentBinding`) so source-linked clipboard structures and later
bound-column work have a stable type.

The full upstream `DataGridColumn.cs` remains blocked because Uno source
generation requires a partial class and the WPF file is not partial.

### Session 4: bound-column infrastructure

Session 4 added `System.Windows.Data.BindingOperations` as a WPF facade over
Uno binding operations, then added local partial shells for `DataGridCell` and
`DataGridBoundColumn`. `DataGridColumn` gained `SortMemberPath`, virtual
clipboard binding, and virtual refresh/generation hooks so bound columns can
compile against the expected WPF-shaped contract.

Direct upstream source-linking is still not the right path for
`DataGridBoundColumn` at this point: it owns dependency properties and depends
on WPF property metadata/coercion semantics. The local shell keeps the API
surface moving while the container/control spine remains absent.

Session 4 verification: `dotnet build WindowsShims/src/WindowsShims.slnx
--framework net10.0-desktop --no-restore` succeeded, and `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 17 tests.

### Session 5: text-column shell

Session 5 added a local partial `DataGridTextColumn` over
`DataGridBoundColumn`. The display path creates a Uno `TextBlock` and binds
`TextBlock.TextProperty` through the bound-column binding bridge. The editing
path creates a Uno `TextBox`, binds its `TextProperty`, and performs minimal
focus/select-all preparation.

This is intentionally below full WPF behavior: the upstream implementation's
font property synchronization, flow-direction cache/restore, typed-text
replacement, mouse caret placement, validation commit behavior, and default
style setters are deferred until there is a real DataGrid owner/control shell
and dispatcher-capable runtime coverage.

Session 5 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 18 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 6: checkbox-column shell

Session 6 added a local partial `DataGridCheckBoxColumn` over
`DataGridBoundColumn`. The display and editing paths create/reuse a Uno
`CheckBox`, synchronize `IsThreeState`, apply the selected style, and bind
`CheckBox.IsCheckedProperty` through the bound-column bridge.

The WPF source remains deferred because its user-input behavior depends on the
full DataGrid owner/edit pipeline: `OnInput`, `BeginEdit`, mouse hit testing,
space-key routing, and immediate toggle rules. The local shell keeps the public
column API and generated element contract available without pretending the
owner-control edit state exists yet.

Session 6 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 19 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 7: template-column shell

Session 7 added a local partial `DataGridTemplateColumn`. It exposes
`CellTemplate`, `CellTemplateSelector`, `CellEditingTemplate`, and
`CellEditingTemplateSelector` using WinUI template types, and generates a WinUI
`ContentPresenter` bound to the row data item through the local WPF
`BindingOperations` facade.

Direct upstream source-linking is still deferred. The upstream file includes
sort coercion through `CanUserSortProperty` and `OnCoerceCanUserSort`, which
belong to the wider `DataGridColumn`/owner sorting model that is not present in
the current shell.

Session 7 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 20 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 8: column event args

Session 8 added local shells for the low-dependency DataGrid column event args:
`DataGridColumnEventArgs`, `DataGridSortingEventArgs`,
`DataGridColumnReorderingEventArgs`, `DataGridAutoGeneratingColumnEventArgs`,
and `DataGridCellClipboardEventArgs`.

Row/edit event args are intentionally deferred because their WPF constructors
and properties require `DataGridRow` and the edit pipeline. The local event args
also avoid the upstream internal `ItemPropertyInfo` constructor until
auto-generation is wired to a real item-property discovery path.

Session 8 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 25 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 9: owner and column collection shell

Session 9 probed upstream `DataGridColumnCollection` and confirmed it is not a
good direct source-link candidate yet. The WPF file is coupled to the full owner
control: display-index maps, frozen-column invalidation, star-width
redistribution, realized-column virtualization blocks, cell-info selection
collections, and column/header presenter notifications.

Instead, session 9 added a local partial `DataGrid` shell with a WPF-shaped
`Columns` property backed by an internal `DataGridColumnCollection`. The
collection tracks `DataGridColumn.DataGridOwner`, rejects column reuse across
owners, normalizes default display indexes, and provides basic
`ColumnFromDisplayIndex` / `ColumnIndexFromDisplayIndex` lookups. The
notification target enum is also local now, but propagation remains a no-op
outside column forwarding until row/header/presenter shells exist.

Session 9 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 29 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 10: combo-box-column shell

Session 10 probed upstream `DataGridComboBoxColumn` and confirmed the binding
and item-source surface can be local-shelled without porting selector
item-container behavior: Uno `ComboBox` already provides `ItemsSource`,
`DisplayMemberPath`, `SelectedValuePath`, `SelectedItem`, `SelectedValue`, and
`Text` natively.

The local partial shell derives from `DataGridColumn` (matching WPF, not
`DataGridBoundColumn`) and exposes `SelectedItemBinding`,
`SelectedValueBinding`, and `TextBinding` as plain CLR properties with the WPF
effective-binding precedence (selected item, then selected value, then text)
used for clipboard fallback and one-way read-only coercion. Display and
editing generation both produce a Uno `ComboBox` with bindings and column
properties applied; `RefreshCellContent` rebinds or re-syncs the matching
combo property on column property changes.

Deferred upstream behavior: `TextBlockComboBox` styling via
`ComponentResourceKey`, `OnInput` drop-down opening (F4/Alt+Up/Alt+Down)
because there is no owner edit pipeline, flow-direction cache/restore, and
`SortMemberPath` coercion from the effective binding because the WPF-style
coercion shims are no-ops.

Session 10 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 34 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 11: re-link pass

Session 11 audited all 46 upstream `DataGrid*` files against the contracts the
shims now provide and converted everything that compiles cleanly to
`linked-upstream`:

- Replaced the session-8 local event-args shells and the session-9 local
  `DataGridNotificationTarget` copy with direct links
  (`DataGridColumnEventArgs`, `DataGridSortingEventArgs`,
  `DataGridSortingEventHandler`, `DataGridColumnReorderingEventArgs`,
  `DataGridAutoGeneratingColumnEventArgs`, `DataGridCellClipboardEventArgs`).
- Linked the leaf helpers `DataGridClipboardHelper` (needed one new
  `DataFormats.CommaSeparatedValue` constant), `DataGridItemAttachedStorage`,
  and `DataGridHeadersVisibilityToVisibilityConverter` (compiles against the
  WPF-shaped `IValueConverter` shim).
- Linked WindowsBase `IItemProperties.cs`, providing
  `System.ComponentModel.ItemPropertyInfo` for the internal auto-generation
  event-args constructor.
- Added a minimal local `DataGridRow` shell (`Item`, `IsEditing`, internal
  owner), which unblocked direct links for all seven row/edit event args:
  `DataGridRowEventArgs`, `DataGridBeginningEditEventArgs`,
  `DataGridCellEditEndingEventArgs`, `DataGridPreparingCellForEditEventArgs`,
  `DataGridRowDetailsEventArgs`, `DataGridRowEditEndingEventArgs`,
  `DataGridRowClipboardEventArgs`.

Net effect: 18 new linked upstream files, two local shell files deleted, and
the event-args backlog cleared. `DataGridRowClipboardEventArgs` round-trips
through the real WPF `DataGridClipboardHelper` CSV/text formatting in tests.

Still blocked after the audit: the behavioral core (`DataGrid.cs`,
`DataGridColumnCollection.cs`, `DataGridCellsPanel.cs`, upstream
`DataGridColumn.cs`/`DataGridRow.cs`/`DataGridCell.cs`, `DataGridHelper.cs`,
concrete column sources, `DataGridCellInfo`, `DataGridColumnHeaderCollection`,
drag/drop header visuals) on the WPF property engine
(`OverrideMetadata`/`AddOwner`/coercion), the Uno generator partial collision,
and the `ItemsControl`/`MultiSelector`/virtualization stack.

Session 11 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 44 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 12: linked `DataGrid.cs` probe and first rungs

The control-shell direction is decided: linked upstream `DataGrid.cs` with
guarded internals. Session 12 probe-linked the 8,628-line upstream file (with
the local `DataGrid` shell excluded) and captured the first-order catalog: 27
unique unresolved contracts. Notably, `CommandManager`, `KeyboardNavigation`,
`VirtualizingPanel`, `ItemsPanelTemplate`, and `FrameworkElementFactory`
already resolve from the RichTextBox-era shims. Error-type cascades suppress
member-level errors, so deeper layers will surface as these clear.

The catalog clusters into a bridge ladder:

1. New-item pipeline leaves (`AddingNewItemEventArgs`,
   `InitializingNewItemEventArgs`/`Handler`, `IProvideDataGridColumn`) —
   linked in session 12.
2. Cell identity (`DataGridCellInfo` over `ItemsControl.ItemInfo`,
   `DataGrid.NewItemInfo`, cell row-owner internals) — linked in session 12.
3. Cell-selection collections (`VirtualizedCellInfoCollection`,
   `SelectedCellsCollection`, `SelectedCellsChangedEventArgs`/`Handler`) —
   need DataGrid items/generator internals.
4. Selector spine (`MultiSelector` base, `SelectedItemCollection`,
   `ItemNavigateArgs`, `FocusNavigationDirection`) — the load-bearing rung;
   needs a WPF-shaped `ItemsControl`/`Selector`/`MultiSelector` bridge.
5. Headers/presenters (`DataGridColumnHeader`,
   `DataGridColumnHeadersPresenter`) and Thumb drag args
   (`DragStarted`/`DragDelta`/`DragCompletedEventArgs`, likely aliasable to
   WinUI primitives).
6. Validation/binding (`ValidationRule`, `BindingGroup`,
   `IEditableCollectionView`, `PropertyGroupDescription`, `MS.Internal.Data`)
   and infra (`UncommonField<>`, `ContainerTracking<>`,
   `ComponentResourceKey`).

The `ItemInfo` bridge intentionally omits WPF's sentinel containers (they
require constructing bare `DependencyObject` instances, dispatcher-bound on
Uno) and generator `Refresh`; equality keeps the item/container/index subset.
`DataGrid.NewItemInfo` returns caller-provided state until a real item
container generator exists.

Session 12 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 51 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 13: cell-selection collection stack

Session 13 compared the two candidate rungs. The selector spine
(`MultiSelector` is only 102 lines, but it sits on `Selector` at ~3k and
`ItemsControl` at ~4k lines) is the expensive rung; the cell-selection
collections turned out to be cheap because `VirtualizedCellInfoCollection`'s
owner contract is narrow: `Columns`, `ColumnFromDisplayIndex` (already
present), plus `Items`, `ItemInfoFromIndex`, and `Dispatcher.VerifyAccess`.

Guarded internals added to the local `DataGrid` shell: a plain `ItemCollection`
`Items` list (until the selector spine provides a real one),
`ItemInfoFromIndex` without generator container resolution, and a subset
`OnSelectedCellsChanged` that skips WPF's selection-unit validation and
pending-change coalescing and just raises the public `SelectedCellsChanged`
event. `DataGrid.SelectedCells` is now exposed as `IList<DataGridCellInfo>`
over the linked `SelectedCellsCollection`.

Supporting shim work: four SR strings
(`SelectedCellsCollection_InvalidItem`/`_DuplicateItem`,
`VirtualizedCellInfoCollection_DoesNotSupportIndexChanges`/`_IsReadOnly`) and
`VerifyAccess`/`CheckAccess` on both the `Dispatcher` shim and the
`CoreDispatcher` extensions (WinUI's native `Dispatcher` property shadows the
WPF-shaped extension, so the upstream call lands on `CoreDispatcher`).

Session 13 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 56 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 14: selector spine probe and rung-5/6 leaves

Session 14 probe-linked `Selector.cs` + `MultiSelector.cs` and got a
surprisingly small first-order catalog: 16 unique unresolved contracts. The
wall is not `ItemsControl` member surface as expected, but the WPF data-engine
internals: `BindingExpression`, `BindingExpressionUncommonField`,
`DynamicValueConverter`, `MS.Internal.Data`, `MS.Internal.KnownBoxes`, plus
seven missing virtuals on the shim `ItemsControl`
(`OnItemsChanged`/`OnItemsSourceChanged`/`Prepare`/`ClearContainerForItemOverride`/
`AdjustItemInfoOverride`/`OnInitialized`/`OnIsKeyboardFocusWithinChanged`) and
`AttachedPropertyBrowsableForChildren`. The probe was reverted; the usual
caveat applies that error-type cascades hide deeper member-level errors.

The session then landed the rung-5/6 leaves:

- Linked Thumb drag args (`DragStartedEventArgs`, `DragDeltaEventArgs`,
  `DragCompletedEventArgs` with their handler delegates) over a minimal local
  `Thumb` shell carrying the three drag routed-event identities.
- Linked `ResourceKey` + `ComponentResourceKey` over new `MarkupExtension`
  and `ComponentResourceKeyConverter` shims and two SR strings.
- Linked `ContainerTracking<>` (self-contained).
- Added a `ConditionalWeakTable`-backed `UncommonField<>` bridge (WPF's
  effective-value-table storage is unreachable on WinUI).
- Added a `FocusNavigationDirection` enum shim mirroring WPF member order.

Session 14 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 61 tests; the solution build
also succeeds for `net10.0-desktop`.

### Session 15: spine layer-1 bridges and layer-2 catalog

Session 15 cleared the session-14 spine catalog and re-probed. What landed:

- Seven no-op WPF-shaped virtuals on the shim `ItemsControl`
  (`ItemsControlSpine.cs`).
- Linked WindowsBase `KnownBoxes.cs`, deleting the local shim whose
  static-class shape broke `using MS.Internal.KnownBoxes;`.
- An untargeted `BindingExpression` bridge (`BindingExpressionBase` with the
  `DisconnectedItem` sentinel, reflection-based dotted-path evaluation with
  `Activate`/`Deactivate`/`Value`), `BindingExpressionUncommonField`, a
  `DynamicValueConverter` bridge over component-model converters, and
  `Binding.XPath` storage.
- `AttachedPropertyBrowsableForChildrenAttribute` (flat designer shim) and
  `MS.Internal.PresentationFramework.BuildInfo` constants.

Discovery: `ext/wpf` is a patched fork using `#if !HAS_UNO` guards inside
upstream files (`Window.cs`, `AdornerLayer`, `TextBoxBase`). Fork-patching is
therefore an established third mechanism alongside linking and local bridges.

The layer-2 re-probe of `Selector.cs`/`MultiSelector.cs` surfaced 79 unique /
302 total errors. Clusters: rich `ItemsControl` member surface (`NewItemInfo`,
`NewUnresolvedItemInfo`, `HasItems`, `ItemInfoFromIndex`,
`GetItemOrContainerFromContainer`, `IsItemItsOwnContainerOverride`),
`ItemCollection` behaving as a `CollectionView` (`CurrentItem`,
`MoveCurrentToPosition`, `IsEmpty`), WPF property-engine internals
(`SetCurrentValueInternal`, `GetValueEntry`/`LookupEntry`,
`EffectiveValueEntry`, `DependencyPropertyKey` set paths), `ItemInfo`
sentinels (intentionally omitted from the bridge), automation peers,
`KeyboardNavigation.Current`, `SystemXmlHelper`, `SelectedItemCollection`,
`DeferredSelectedIndexReference`, handler add/remove signature differences,
and several SR strings. Probe reverted; baseline stays green.

Conclusion: clean source-linking of the spine is not reachable by bridging
alone — the property-engine cluster has no Uno equivalent. The next decision
is fork-patching `Selector.cs`/`MultiSelector.cs` with `#if !HAS_UNO` guards
around those clusters versus writing a WPF-shaped local `Selector` spine that
exposes only what `DataGrid.cs` consumes.

Session 15 verification: `dotnet test
WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj
--framework net10.0-desktop --no-restore` passed 70 tests; the solution build
also succeeds for `net10.0-desktop`.

## Open Questions

- Whether the first usable Uno DataGrid should wrap an existing Uno/WinUI grid
  control or preserve the WPF source as the behavioral owner from the start.
- How much WPF `ItemsControl`/selector surface should live in WindowsShims
  generally versus private DataGrid bridges.
- Whether full WPF virtualization semantics are required for the first consumer
  or can be deferred behind static row rendering.
