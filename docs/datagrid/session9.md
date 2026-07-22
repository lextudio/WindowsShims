# DataGrid Port - Session 9

Date: 2026-06-06

## Goal

Probe `DataGridColumnCollection` and add the smallest owner/collection shell
needed before deeper `DataGrid` control work.

## What Changed

- Added a local `DataGrid` shell exposing WPF-shaped `Columns` as an
  `ObservableCollection<DataGridColumn>`.
- Added an internal `DataGridColumnCollection` that tracks column ownership,
  prevents cross-grid column reuse, normalizes default display indexes, and
  supports basic display-index lookup.
- Added internal `DataGridNotificationTarget` flags and a no-op owner
  notification path for future row/header/presenter propagation.
- Added internal `DataGridColumn.DataGridOwner` and `IsVisible` state used by
  the owner/collection spine.
- Added `DataGridCollectionTests` reflection coverage and included it in the
  explicit test project compile list.

## Verification

Commands:

```bash
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
```

Result: build succeeded; tests passed with 29 passed, 0 failed.

## Notes

The upstream `DataGridColumnCollection` remains deferred. It expects the full
WPF `DataGrid` owner, frozen-column handling, star-width computation,
virtualized realized-column block lists, and selected-cell collection updates.
Those should be layered in only after there are real row/header/presenter
contracts.

## Next Session

1. Probe `DataGridComboBoxColumn` next now that the owner/collection shell
   exists.
2. Decide whether combo box binding/item-source surface can be local-shelled
   without bringing in selector item-container behavior.
3. Keep hyperlink and row/edit event args queued behind command/navigation and
   `DataGridRow` dependencies respectively.
