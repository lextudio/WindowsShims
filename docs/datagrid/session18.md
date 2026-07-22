# DataGrid Port - Session 18

Date: 2026-06-11

## Goal

Land the session-17 control-root prerequisites, then attempt the upstream
`DataGrid.cs` link.

## What Changed

- Linked the validation layer: `ValidationRule.cs`, `ValidationResult.cs`,
  `ValidationStep.cs` (PresentationFramework) and `IEditableCollectionView`/
  `IEditableCollectionViewAddNewItem` (WindowsBase).
- Local bridges: `BindingGroup` (row-validation surface; stores rules/items,
  reports edits committable — transactional semantics need the WPF property
  engine), `GroupDescription`/`PropertyGroupDescription` (group-name
  extraction over the untargeted binding-expression path walker), and minimal
  `DataGridColumnHeader`/`DataGridColumnHeadersPresenter` shells.
- Shim growth: eleven WPF virtuals on the shim `ItemsControl` (including
  `FocusItem`), the nested `ItemNavigateArgs` bridge, an `InputDevice` base
  (with `KeyboardDevice` rebased onto it), and `Keyboard.PrimaryDevice`.
- Fork guards in upstream `DataGrid.cs`: `OnCreateAutomationPeer` excluded
  under `HAS_UNO`; `OnApplyTemplate` accessibility narrowed to protected.
- Probe: with all type-level prerequisites resolved, the link attempt
  surfaced the honest member-level catalog — 386 unique sites in seven
  clusters (command system, sorting/view, focus traversal, automation,
  helper/visual internals, row/cell/presenter internals, metadata friction).
  Catalog recorded in DATAGRID.md; probe reverted; local shell stays active.
- Added `DataGridControlRootPrereqTests` (linked validation round trip,
  `BindingGroup` surface, `PropertyGroupDescription` group-name extraction,
  header shells, editable-view interfaces).

## Verification

Commands:

```bash
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
```

Result: build succeeded; tests passed with 80 passed, 0 failed.

## Notes

Methodology lesson worth keeping: declaration-level errors (the session-17
"21 sites") suppress method-body diagnostics for the affected members, so a
small catalog over a huge file is only trustworthy once the declarations
compile. The 386-site catalog is the real control-root price, and it
decomposes cleanly — `CommandManager` class bindings alone account for ~44
sites and gate begin/commit/cancel/delete editing commands.

A live constraint also got a concrete stack trace this session: constructing
any `DependencyObject`-derived bridge off-dispatcher throws in
`NativeDispatcher.GetHasThreadAccess` (hit by the original `BindingGroup`
test), confirming the surface-level testing rule for generated partials.

## Next Session

1. Cluster 1, the command system: a WPF-shaped `CommandManager` bridge
   (class command/input bindings, `RoutedCommand`/`RoutedUICommand`,
   executed/can-execute routing) sized to what `DataGrid.cs` and the editing
   pipeline invoke. Check what the RichTextBox era already shimmed under
   `System.Windows.Input` first.
2. Then cluster 2 (sorting/view: `SortDescription(Collection)` linkage,
   `ItemCollection.SortDescriptions`, `CollectionView` minimal type,
   `IsGrouping`) — likely partially linkable from WindowsBase.
3. Clusters proceed per the DATAGRID.md staged plan; repeat the link attempt
   once clusters 1-6 are down.
