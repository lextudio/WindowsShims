# DataGrid Port - Session 20

Date: 2026-06-12

## Goal

Control-root cluster 2: sorting/view.

## What Changed

- Linked WindowsBase `SortDescription.cs` + `SortDescriptionCollection.cs`
  (added the `CannotChangeAfterSealed` SR string).
- `ItemCollection` gained `SortDescriptions` (stored, not applied) and an
  `IEditableCollectionView` implementation with direct-list semantics —
  `DataGrid.EditableItems` is a plain cast of `Items`, so the editing
  pipeline binds against it. `AddNew`/placeholders/`CancelEdit` are
  unsupported and reported honestly via the capability properties.
- Added minimal `CollectionView` (stable `NewItemPlaceholder` sentinel),
  `CollectionViewGroupInternal` and `GroupItem` grouping stubs, and
  `IsGrouping => false` on the shim `ItemsControl` (grouping paths become
  unreachable).
- Re-probe of the control root: 355 → 320 unique sites; sorting/view names
  cleared. Probe reverted.
- Added `ItemCollectionViewTests` (observable sort descriptions through the
  linked collection, edit-item bookkeeping, unsupported-operation contracts,
  removal, placeholder identity).

## Verification

Commands:

```bash
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
```

Result: build succeeded; tests passed with 90 passed, 0 failed.

## Notes

Sorting is stored-only: `DataGrid` column-header sorting will manipulate
`SortDescriptions` correctly once the control root links, but the shim view
does not yet reorder items. Applying sort descriptions inside
`ItemCollection` (a simple comparer over the dotted-path evaluator) is a
natural follow-up once the runtime sample exists to observe it.

## Next Session

1. Cluster 3, keyboard-focus traversal: `KeyboardNavigationMode` enum,
   `TraversalRequest` (WindowsBase linkability), `MoveFocus`/one-argument
   `Focus` members, `KeyboardNavigation.IsAncestorOfEx`/
   `PredictFocusedElement` stubs.
2. Cluster 4, automation: likely fork guards (peers are not bridged) plus
   `AutomationEvents` member stubs.
3. Then helper/visual internals (`DataGridHelper.FindVisualParent`/
   `IsDefaultValue`, `VisualStates`, `Panel.Children`) and row/cell/presenter
   internals before the next link attempt.
