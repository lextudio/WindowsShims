# DataGrid Port - Session 8

Date: 2026-06-06

## Goal

Add the column-focused DataGrid event args that downstream APIs will need before
the first `DataGrid` owner shell.

## What Changed

- Added local shells for `DataGridColumnEventArgs`,
  `DataGridSortingEventArgs`, `DataGridColumnReorderingEventArgs`,
  `DataGridAutoGeneratingColumnEventArgs`, and
  `DataGridCellClipboardEventArgs`.
- Kept the event args independent from dispatcher-bound column construction so
  they can be tested in the current desktop NUnit harness.
- Added `DataGridEventArgsTests` and included it in the explicit test project
  compile list.

## Verification

Commands:

```bash
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
```

Result: build succeeded; tests passed with 25 passed, 0 failed.

## Notes

The upstream row/edit event args remain deferred because they require
`DataGridRow` and edit-pipeline state. The auto-generating-column args also
omit the internal `ItemPropertyInfo` constructor for now; that should be added
with the auto-generation pipeline rather than as a standalone type leak.

## Next Session

1. Probe `DataGridColumnCollection` and identify the smallest owner hooks it
   expects from `DataGrid`.
2. Decide whether to add a short-lived `DataGrid` owner shell before the
   collection, or a local collection that defers owner callbacks.
3. Keep row/edit event args queued until a `DataGridRow` shell exists.
