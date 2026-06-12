# DataGrid Port - Session 19

Date: 2026-06-12

## Goal

Control-root cluster 1: the command system, plus the cluster-7
`FrameworkPropertyMetadata` friction.

## What Changed

- Audited the RichTextBox-era command shims: `RoutedCommand` (class-binding
  registry with target-scoped dispatch), `CommandBinding` (handlers +
  `AppliesTo` type filter), `RoutedUICommand`, `KeyGesture`, and collections
  already existed. Only `CommandManager` and `InputBinding` were missing.
- Added the `CommandManager` bridge: `RegisterClassCommandBinding` scopes a
  self-registered binding to its owner type (`CommandBinding.SetClassOwner`),
  `RegisterClassInputBinding` records per-type gesture/command pairs for the
  future key-routing bridge, and `InvalidateRequerySuggested` raises
  `RequerySuggested` directly (WPF batches on the dispatcher). Added the flat
  `InputBinding` bridge.
- Fixed the `FrameworkPropertyMetadata` overload ambiguity hit by linked
  `DataGrid.cs` (~18 sites): Roslyn reports the WPF-delegate and
  WinUI-delegate two-argument constructors as ambiguous for WPF method-group
  arguments even though only the WPF conversion exists (verified by a
  direct-assignment repro). Removed the WinUI two-argument overload — no
  caller in the solution needed it — with a comment preventing its return.
- Re-probed the control root: 386 → 355 unique sites; the command and
  metadata clusters no longer appear. Probe reverted.
- Added `CommandManagerBridgeTests` (owner-type scoping of execute and
  can-execute, requery event, per-type input-binding registry, argument
  validation).

## Verification

Commands:

```bash
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
```

Result: build succeeded; tests passed with 85 passed, 0 failed.

## Notes

The command bridge is fully behavior-tested because the command types are
plain CLR classes — no dispatcher constraint. The DataGrid editing pipeline
(BeginEdit/CommitEdit/CancelEdit/Delete commands) will route through this
bridge once the control root links; input-driven invocation (F2 etc.) stays
inert until the key-routing bridge fires class input bindings.

## Next Session

1. Cluster 2, sorting/view: `SortDescription`/`SortDescriptionCollection`
   (check WindowsBase linkability), `ItemCollection.SortDescriptions`, a
   minimal `CollectionView` type surface, `IsGrouping`/`GroupStyle` on the
   shim `ItemsControl`.
2. Then cluster 3 (keyboard-focus traversal: `KeyboardNavigationMode`,
   `TraversalRequest`, `MoveFocus`, one-argument `Focus`,
   `PredictFocusedElement`, `IsAncestorOfEx`) — mostly shim members plus
   guards for genuinely visual-tree-bound pieces.
3. Automation guards, helper/visual internals, and row/cell/presenter
   internals follow per the staged plan.
