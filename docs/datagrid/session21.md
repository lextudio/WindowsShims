# DataGrid Port - Session 21

Date: 2026-06-12

## Goal

Control-root clusters 3 and 4: keyboard-focus traversal and automation.

## What Changed

- Linked WindowsBase `TraversalRequest.cs` (which carries the real
  `FocusNavigationDirection` enum) and deleted the session-14 local enum shim
  it replaces.
- Added `KeyboardNavigationMode` (WPF member order); grew
  `KeyboardNavigation` with `DirectionalNavigation`/`ControlTabNavigation`
  attached properties, `ShowFocusVisual`, instance `IsAncestorOfEx` (false)
  and `PredictFocusedElement` (null).
- Added `Keyboard.Focus(IInputElement)`; `MoveFocus`/`Focus()` on the shim
  `ItemsControl` (report no movement); real zero-argument `Focus()` members
  on `DataGridCell`/`DataGridRow` routing to WinUI programmatic focus.
- Automation: chose stubs over ~36 fork guards. `AutomationPeer.FromElement`
  returns null and `ListenerExists` returns false (paths honestly
  unreachable); added `UIElementAutomationPeer`, `DataGridAutomationPeer`
  raise/find members, `DataGridItemAutomationPeer`,
  `DataGridCellItemAutomationPeer`, the full WPF-ordered `AutomationEvents`,
  and `ValuePatternIdentifiers.ValueProperty`.
- Re-probe: 320 → 248 unique sites; both clusters cleared. Probe reverted.
- Added `FocusAndAutomationBridgeTests` (linked traversal-request validation,
  enum orders, focus pass-through, stub honesty).

## Verification

Commands:

```bash
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
```

Result: build succeeded; tests passed with 95 passed, 0 failed.

## Notes

Two clusters remain before the next link attempt, and they are the most
DataGrid-specific ones:

1. Helper/visual internals: `DataGridHelper.FindVisualParent<T>` (real
   implementation possible over WinUI `VisualTreeHelper`), `IsDefaultValue`,
   `VisualStates`, `Panel.Children`, `ContentElement`, the
   `CoerceValueCallback` argument conversions, and the `IsVisible`
   accessibility clash.
2. Row/cell/presenter/column-collection internals: `DataGridRow` members
   (`DetailsPresenter`, `CellsPresenter`, `DetailsVisibility(+Property)`,
   `DetailsLoaded`, `TryGetCell`, `BindingGroup`), `DataGridCellsPresenter`
   shell, `DataGridCell` notification members,
   `DataGridColumnCollection` width/realization internals
   (`InvalidateColumnRealization`, `FirstVisibleDisplayIndex`,
   `LastVisibleDisplayIndex`, `DisplayIndexMap`),
   `DataGridColumn.SortDirection`, `OnBringItemIntoView`,
   `ItemInfoFromContainer`.

## Next Session

1. Cluster 5: helper/visual internals — implement `FindVisualParent<T>` for
   real over `VisualTreeHelper`; stub/add the rest.
2. Cluster 6: grow the row/cell/presenter shells and the column collection's
   internal surface to what the control root touches.
3. Then the third `DataGrid.cs` link attempt; if it compiles, reconcile the
   local shell and gate behavior work on the runtime sample.
