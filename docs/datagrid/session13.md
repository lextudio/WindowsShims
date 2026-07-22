# DataGrid Port - Session 13

Date: 2026-06-10

## Goal

Climb rung 3 of the linked-`DataGrid.cs` ladder: bring the cell-selection
collection stack (`VirtualizedCellInfoCollection`, `SelectedCellsCollection`,
`SelectedCellsChangedEventArgs`/`Handler`) online against guarded `DataGrid`
internals.

## What Changed

- Compared rung 3 (cell collections) against rung 4 (selector spine) and
  confirmed rung 3 is the cheap one: `VirtualizedCellInfoCollection`'s owner
  contract is only `Columns`, `ColumnFromDisplayIndex`, `Items`,
  `ItemInfoFromIndex`, and `Dispatcher.VerifyAccess`. The spine
  (`Selector` ~3k + `ItemsControl` ~4k lines) remains the expensive rung.
- Linked four upstream files: `VirtualizedCellInfoCollection.cs`,
  `SelectedCellsCollection.cs`, `SelectedCellsChangedEventArgs.cs`,
  `SelectedCellsChangedEventHandler.cs`.
- Extended the local `DataGrid` shell with guarded internals: a plain
  `ItemCollection` `Items` list, `ItemInfoFromIndex` (no generator container
  resolution yet), and a subset `OnSelectedCellsChanged` that raises the new
  public `SelectedCellsChanged` event without WPF's selection-unit validation
  or pending-change coalescing. Exposed `SelectedCells` as
  `IList<DataGridCellInfo>`.
- Added four SR strings for the collection's exception paths.
- Added `VerifyAccess`/`CheckAccess` to the `Dispatcher` shim and to the
  `CoreDispatcher` extensions (WinUI's native `Dispatcher` property shadows
  the WPF-shaped extension, so upstream calls land on `CoreDispatcher`).
- Added `DataGridSelectedCellsTests` covering the new shell surface, the
  linked collection type hierarchy, and `SelectedCellsChangedEventArgs`
  read-only wrapping and null validation.

## Verification

Commands:

```bash
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
```

Result: build succeeded; tests passed with 56 passed, 0 failed.

## Notes

The whole 1.7k-line `VirtualizedCellInfoCollection` compiled with only two
shim gaps (SR strings and `VerifyAccess`), which is a good signal for the
linked approach: the WPF source increasingly compiles against contracts the
shims already carry. Selection behavior through these collections is
dispatcher-bound (DataGrid construction), so plain NUnit coverage stays at
surface/event-args level until a runtime sample exists.

## Next Session

1. Rung 4, the selector spine: probe-link `Selector.cs` (and `MultiSelector.cs`
   on top) to catalog their first-order contracts, then decide between source
   linking with bridges and a WPF-shaped local `ItemsControl`/`Selector`/
   `MultiSelector` chain over the existing shims.
2. Alternatively pick off rung 5/6 leaves first (Thumb drag args possibly
   aliasable to WinUI primitives; `ComponentResourceKey`; `UncommonField<>`)
   if the spine probe is discouraging.
3. After the spine: header/presenter shells, then re-probe `DataGrid.cs`.
