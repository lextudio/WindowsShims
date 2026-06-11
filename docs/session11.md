# DataGrid Port - Session 11

Date: 2026-06-10

## Goal

Re-link pass: audit all upstream `DataGrid*` files against the contracts the
shims now provide, replace local shells with direct upstream links wherever
they compile cleanly, and unblock the row/edit event args with a minimal
`DataGridRow` shell.

## What Changed

- Deleted the local `DataGridNotificationTarget.cs` (byte-identical to
  upstream) and `DataGridEventArgs.cs` shells; both are now direct links.
- Linked 18 upstream files in total:
  - Column event args and handler: `DataGridColumnEventArgs`,
    `DataGridSortingEventArgs`, `DataGridSortingEventHandler`,
    `DataGridColumnReorderingEventArgs`,
    `DataGridAutoGeneratingColumnEventArgs`,
    `DataGridCellClipboardEventArgs`.
  - Notification flags: `DataGridNotificationTarget`.
  - Leaf helpers: `DataGridClipboardHelper`, `DataGridItemAttachedStorage`,
    `DataGridHeadersVisibilityToVisibilityConverter`.
  - WindowsBase `IItemProperties.cs` for
    `System.ComponentModel.ItemPropertyInfo`.
  - Row/edit event args: `DataGridRowEventArgs`,
    `DataGridBeginningEditEventArgs`, `DataGridCellEditEndingEventArgs`,
    `DataGridPreparingCellForEditEventArgs`, `DataGridRowDetailsEventArgs`,
    `DataGridRowEditEndingEventArgs`, `DataGridRowClipboardEventArgs`.
- Added a minimal local `DataGridRow` shell over the WPF-shaped `Control`
  shim (`Item`, `IsEditing`, internal `DataGridOwner`) to satisfy the row/edit
  event args.
- Added `DataFormats.CommaSeparatedValue` to the clipboard shim for
  `DataGridClipboardHelper`.
- Added `DataGridRowEventArgsTests` covering the row shell surface, all seven
  row/edit event args, the sorting handler delegate, `ItemPropertyInfo`
  feeding the internal auto-generation constructor, and CSV/text clipboard
  formatting through the real `DataGridClipboardHelper`.

## Verification

Commands:

```bash
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
```

Result: build succeeded; tests passed with 44 passed, 0 failed.

## Notes

This clears the event-args backlog entirely. The remaining unlinked upstream
files are the behavioral core (`DataGrid.cs`, `DataGridColumnCollection.cs`,
`DataGridCellsPanel.cs`, upstream column/row/cell sources,
`DataGridHelper.cs`, `DataGridCellInfo`, `DataGridColumnHeaderCollection`,
header drag/drop visuals), all blocked on the WPF property engine, the Uno
generator partial collision, and the `ItemsControl`/selector/virtualization
stack.

## Next Session

1. Start the control-shell milestone (ladder step 12): decide between a linked
   `DataGrid.cs` with guarded internals and a short-lived local shell exposing
   WPF-shaped dependency properties over Uno `ListView`/`Grid`, then grow the
   existing local `DataGrid` shell toward it.
2. Probe `DataGridHyperlinkColumn` for its navigation/routed-command needs if
   a smaller session is preferred.
3. Row/cell container behavior remains queued behind the control shell.
