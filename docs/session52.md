# DataGrid Port - Session 52

Date: 2026-06-13

## Goal

Land the first staged **editing reuse** slice from session 51: route commit
through the linked WPF `DataGrid.CommitEdit` command path, remove the shim's
duplicate commit orchestration, and keep the existing edit/validation/row-edit
behavior green.

## What Changed

- `DataGridCell.BeginEdit` now sets `CurrentCellContainer`, so the linked WPF
  command handlers have a real current cell to target.
- `CommandBinding` / `RoutedCommand` now resolve the **invocation target** to
  the matching ancestor instance, not just a matching descendant target. This
  makes class command bindings execute with `sender == DataGrid` when the
  command is raised against a `DataGridCell`, matching WPF's class-command
  routing contract.
- Cell-originated commits now funnel through
  `DataGrid.CommitEdit(DataGridEditingUnit.Row, true)`. The linked WPF
  `OnExecutedCommitEdit` raises `CellEditEnding` / `RowEditEnding`, calls
  `cell.CommitEdit()`, and drives the row commit path; the cell shim no longer
  raises those events or commits the row itself.
- A narrow re-entrancy flag (`ShimExecutingCommitEditCommand`) lets
  `DataGridCell.CommitEdit()` switch to pure cell-core behavior when the WPF
  command handler calls back into the cell, avoiding recursion and duplicate
  events.
- The editable-view bridge now carries `IEditableObject` transactions:
  `ItemCollection.EditItem/CommitEdit/CancelEdit` call
  `BeginEdit/EndEdit/CancelEdit`, and the shim row-edit bookkeeping uses that
  bridge instead of calling `IEditableObject` directly.
- Row validation is now wired into the routed commit path via
  `ShimValidateRowCommit(row)`, so `RowValidationRules` still block the commit,
  flag the row, and keep the cell editing.

## Verification

Build/test/probe green:

- `dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj`
  → 122 passed, 0 failed
- `dotnet run --project WindowsShims/src/LeXtudio.Windows.Sample/LeXtudio.Windows.Sample.csproj --framework net10.0-desktop -- --probe`
  → `DONE failures=0`

Probe evidence for the reuse:

- `grid.CommitEdit(Row)` now commits edits through the routed WPF path.
- A vetoed commit raises `CellEditEnding` once and `RowEditEnding` zero times.
- A successful commit raises `CellEditEnding=1`, `RowEditEnding=1`, writes the
  value, ends `IEditableObject`, and clears edit mode.
- Row validation still blocks the commit and keeps the cell editing.

## Notes / honest limits

- This session reuses the **commit** command path only. `BeginEdit` still uses
  the shim entry path because upstream `OnExecutedBeginEdit` depends on heavier
  unshimmed substrate (`EditableItems` new-item placeholder flow,
  selected-cell-region maintenance, and related current-cell helpers).
- The WPF row-commit flow is now active, but the begin-edit side is still a
  hybrid: shim begin, WPF commit.

## Next Session

1. Stage begin-edit reuse: guard or bridge the new-item-placeholder /
   selected-cell-region branches in `OnExecutedBeginEdit`.
2. Apply the same routed-command approach to `CancelEdit`, so cancel follows
   the same single WPF orchestration path as commit.
3. Revisit selection-command reuse once the current-cell / edit substrate is
   less hybrid.
