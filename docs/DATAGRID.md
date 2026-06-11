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
- Session 8 build/test status: green for the low-coupling DataGrid surface,
  binding bridge, local column/bound-column/text/checkbox/template-column
  shells, column event args, cell shell, and clipboard cell content.

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
| Column event args | `DataGridColumnEventArgs`, `DataGridSortingEventArgs`, `DataGridColumnReorderingEventArgs`, `DataGridAutoGeneratingColumnEventArgs`, `DataGridCellClipboardEventArgs` | `System.Windows/Controls/DataGridEventArgs.cs` | `local-shell` | Low-dependency event args are local shells. Row/edit event args remain deferred until `DataGridRow` exists. |
| Other concrete columns | combo box/hyperlink column types | Not enabled | `blocked` | Combo box likely needs item-source/display-member/selected-value binding surface first; hyperlink may need navigation/routed-command pieces. |
| Row/cell containers | `DataGridRow`, `DataGridCell`, `DataGridCellsPanel`, presenters | Not enabled | `blocked` | Requires item container generation, virtualization, layout override parity, visual states, and automation support. |
| Control root | `DataGrid.cs` | Not enabled | `blocked` | Depends on WPF selector/items stack and should be attempted after the column and container contracts are cataloged. |
| Clipboard event args | `DataGridClipboardCellContent`, related event args | Not enabled | `deferred` | `DataGridClipboardCellContent` references `DataGridColumn`; enable after column base lands. |

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
9. Enable remaining low-behavior concrete column types in this order:
   combo box/hyperlink columns as their dependencies become clear.
10. Add collection and notification types: `DataGridColumnCollection` and the
   minimal owner notification hooks it requires.
11. Build the control shell only after column APIs compile: choose either a
   linked `DataGrid.cs` with guarded internals or a short-lived local shell that
   exposes WPF-shaped dependency properties over Uno `ListView`/`Grid`.
12. Bring row/cell containers and presenters online only when the control shell
   has tests proving the owner/column/item contracts.

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

## Open Questions

- Whether the first usable Uno DataGrid should wrap an existing Uno/WinUI grid
  control or preserve the WPF source as the behavioral owner from the start.
- How much WPF `ItemsControl`/selector surface should live in WindowsShims
  generally versus private DataGrid bridges.
- Whether full WPF virtualization semantics are required for the first consumer
  or can be deferred behind static row rendering.
