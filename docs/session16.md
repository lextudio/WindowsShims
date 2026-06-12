# DataGrid Port - Session 16

Date: 2026-06-11

## Goal

Decide the selector-spine mechanism and enable `Selector`/`MultiSelector`.

## What Changed

- Decision: fork-patch with `#if HAS_UNO` guards (the established `ext/wpf`
  precedent), after growing the cheap shim surface to minimize guard count.
- Shim growth (see DATAGRID.md session-16 probe results for the full list):
  currency-tracking `ItemCollection`; completed `ItemInfo` (lazy generated
  sentinels, full WPF equality, `Reset`, `Key`); `ItemsControl` info surface,
  generator property, `IGeneratorHost`, and bare-call members
  (`CheckAccess`/`CoerceValue`/`SetValue(DependencyPropertyKey)`/
  `SetCurrentValueInternal`/`AddHandler`/`RemoveHandler`/`RaiseEvent`);
  instance-capable `KeyboardNavigation`; `SystemXmlHelper`,
  `CollectionViewSource`, `ItemContainerGenerator`/`GeneratorStatus`,
  `FrameworkAppContextSwitches` (consolidated to `MS.Internal`); eight SR
  strings; `FrameworkPropertyMetadataOptions.Journal`;
  `BindingExpressionBase` global alias.
- Fork guards in `Selector.cs` (seven sites): attached-handler statics, two
  effective-value coercion blocks, automation event body, `LayoutUpdated`
  signature adapters, deferred selected-index write, plus one
  `DisconnectedItem` qualification.
- Linked permanently: `Selector.cs`, `MultiSelector.cs`,
  `SelectedItemCollection.cs`, `SelectionChangedEventArgs.cs` (with handler).
- Added `SelectorSpineTests` (type hierarchy, selection API surface,
  `SelectionChangedEventArgs` round trip).

## Verification

Commands:

```bash
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
```

Result: build succeeded; tests passed with 74 passed, 0 failed.

## Notes

The error ladder across the session: 79 unique sites (session-15 layer 2) →
59 after the first shim batch → 49 → 32 → 0 after the second batch plus seven
fork guards. The guards are confined to genuinely engine-bound code
(effective-value entries, deferred references, automation peers, WinUI event
signatures); everything else compiles against real shim behavior.

Known debt: the spine carries selection state but has no container
generation, input selection, currency sync, or automation;
`System.Windows.BindingExpressionBase` (EarlyBatch shim) shadows the
System.Windows.Data bridge via enclosing-namespace lookup and should be
consolidated.

## Next Session

1. `DataGrid` shell convergence: now that `MultiSelector` exists, evaluate
   rebasing the local `DataGrid` shell from `Control` onto `MultiSelector` so
   its `Items`/selection surface comes from the spine, then re-probe
   `DataGrid.cs` to get the next control-root catalog.
2. Header/presenter shells (`DataGridColumnHeader`,
   `DataGridColumnHeadersPresenter`) and validation/binding-group bridges
   (`ValidationRule`, `BindingGroup`, `IEditableCollectionView`,
   `PropertyGroupDescription`) remain on the rung list.
3. Consolidate the duplicate `BindingExpressionBase` shims.
