# DataGrid Port - Session 54

Date: 2026-06-13

## Goal

Batch the remaining edit-entry work into a larger reuse step by routing
**begin edit** through the linked WPF `DataGrid.BeginEdit` command path, so
all three edit commands now share the same upstream orchestration model.

## What Changed

- Added `ShimExecutingBeginEditCommand`, mirroring the existing commit/cancel
  re-entrancy guards.
- `DataGridCell.BeginEdit()` now delegates external begin requests upward to
  `DataGrid.BeginEdit(editingEventArgs)` after syncing `CurrentCellContainer`.
  This makes the linked WPF `OnExecutedBeginEdit` the entry-point owner for
  the common edit path.
- The linked WPF `OnExecutedBeginEdit` now calls back into the cell under the
  begin guard, so the cell performs only local editor creation while upstream
  WPF owns:
  `BeginningEdit`, row-edit startup (`EditRowItem` /
  `IEditableCollectionView.EditItem`), `BindingGroup.BeginEdit`, and row edit
  state.
- Removed the dead fallback branch from `DataGridCell.CancelEdit()` left over
  after session 53's cancel-command routing.
- Probe coverage now asserts that a successful routed begin raises exactly one
  `BeginningEdit` before the commit-veto path runs.

## Verification

- `dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --no-restore`
  → 122 passed, 0 failed
- `dotnet run --project WindowsShims/src/LeXtudio.Windows.Sample/LeXtudio.Windows.Sample.csproj --framework net10.0-desktop -- --probe`
  → `DONE failures=0`

## Result

The edit lifecycle is now structurally consistent:

- begin: linked WPF command path
- commit: linked WPF command path
- cancel: linked WPF command path

The remaining editing gaps are no longer command ownership; they are deeper
substrate items such as placeholder/add-new row behavior and selected-cell
region management.

## Next Batch

1. Group placeholder/new-item substrate work into one larger session instead
   of another narrow command-path slice.
2. Then revisit selection/edit interactions that depend on a more complete WPF
   current-cell and add-new model.
