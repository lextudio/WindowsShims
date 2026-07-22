# DataGrid Port - Session 55

Date: 2026-06-13

## Goal

Batch the next add-new-related substrate instead of another isolated command
 tweak: enable the WPF `DataGrid` add-new collection-view contract, surface the
 placeholder row in the shim render path, and keep placeholder visibility in
 sync with `CanUserAddRows`.

## What Changed

- `ItemCollection` now implements `IEditableCollectionViewAddNewItem` in
  addition to `IEditableCollectionView`.
- Added direct-list add-new state:
  `CanAddNew`, `CanAddNewItem`, `IsAddingNew`, `CurrentAddItem`,
  `AddNewItem`, `AddNew`, `CommitNew`, `CancelNew`, and
  `NewItemPlaceholderPosition`.
- The collection now manages `CollectionView.NewItemPlaceholder` directly,
  inserting/removing it at the beginning or end as required and keeping it out
  of the sort payload during `Refresh()`.
- `DataGridRow.PrepareRow()` now marks placeholder / current-add rows via
  `IsNewItem`.
- The shim render path now re-synchronizes placeholder state before rebuilds,
  and `DataGrid.UpdateLayout()` forces a shim-tree refresh before the normal
  layout pass so option changes like `CanUserAddRows` are reflected visually.
- Probe coverage now verifies the add-new substrate end-to-end at the
  collection-view level:
  enabling `CanUserAddRows` surfaces the placeholder row, `AddNewItem(...)`
  starts an add transaction, and `CommitNew()` keeps the item while restoring
  the placeholder.
- Unit coverage was updated to reflect that add-new is no longer wholly
  unsupported.

## Verification

- `dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --no-restore`
  → 122 passed, 0 failed
- `dotnet run --project WindowsShims/src/LeXtudio.Windows.Sample/LeXtudio.Windows.Sample.csproj --framework net10.0-desktop -- --probe`
  → `DONE failures=0`

## Result

This session lands the **collection/add-new substrate** needed by upstream WPF
 `DataGrid` add-new behavior:

- placeholder visibility exists
- add transactions exist
- commit/cancel state exists
- sorting preserves placeholder placement

What is still not fully closed is the higher rung where a user begins editing
 directly on the placeholder cell and the full routed `OnExecutedBeginEdit`
 add-new branch drives creation and focus transition automatically.

## Next Batch

1. Finish placeholder-cell edit entry so the routed WPF begin-edit add-new
   branch is probe-verified, not just the underlying collection contract.
2. Then batch the remaining current-cell / selection interactions that depend
   on the add-new path behaving like WPF.
