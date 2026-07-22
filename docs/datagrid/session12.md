# DataGrid Port - Session 12

Date: 2026-06-10

## Goal

Start the control-shell milestone on the linked path: probe-link upstream
`DataGrid.cs`, catalog its first-order contracts, and land the first bridge
rungs the probe reveals.

## What Changed

- Decided the control-shell direction: linked upstream `DataGrid.cs` with
  guarded internals, grown rung by rung (user preference), instead of a local
  shell over Uno `ListView`/`Grid`.
- Probe-linked the 8,628-line upstream `DataGrid.cs` and captured 27 unique
  first-order unresolved contracts (catalog and bridge ladder recorded in
  DATAGRID.md). `CommandManager`, `KeyboardNavigation`, `VirtualizingPanel`,
  `ItemsPanelTemplate`, and `FrameworkElementFactory` already resolve from
  RichTextBox-era shims. The probe link was then reverted.
- Linked the new-item pipeline leaves: `AddingNewItemEventArgs`,
  `InitializingNewItemEventArgs`, `InitializingNewItemEventHandler`, and
  `IProvideDataGridColumn`.
- Added a local `ItemsControl.ItemInfo`/`EqualsEx` bridge (item/container/
  index equality subset; WPF sentinel containers and generator `Refresh`
  omitted because they are dispatcher-bound or virtualization-only).
- Added guarded internals: `DataGrid.NewItemInfo` and `DataGridCell`
  `RowOwner`/`DataGridOwner`/`RowDataItem`.
- Linked upstream `DataGridCellInfo.cs` over those bridges.
- Added `DataGridCellInfoTests` covering cell-info argument validation and
  surface, `ItemInfo` bridge equality/clone semantics, and the new-item event
  args.

## Verification

Commands:

```bash
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
```

Result: build succeeded; tests passed with 51 passed, 0 failed.

## Notes

The first-order catalog is suppressed-cascade data: fixing these 27 contracts
will surface member-level errors against existing shims (for example
`OverrideMetadata` calls in the static constructor). The load-bearing rung is
the selector spine — upstream `DataGrid` derives from `MultiSelector`, which
needs a WPF-shaped `ItemsControl`/`Selector`/`MultiSelector` bridge before the
control root can compile.

`SelectedCellsChangedEventArgs` stays blocked even though it looks like a
leaf: its internal constructor calls
`VirtualizedCellInfoCollection.MakeEmptyCollection`, pulling the 1.7k-line
collection that needs DataGrid items/generator internals.

## Next Session

1. Climb rung 3: decide whether `VirtualizedCellInfoCollection` can compile
   against guarded `DataGrid` internals (items, generator stubs) or needs a
   local subset; then link `SelectedCellsCollection` and
   `SelectedCellsChangedEventArgs`/`Handler`.
2. Alternatively start rung 4 (the selector spine bridge) if the cell
   collections prove cheaper to defer — probe `MultiSelector`/`Selector`
   source-link cost first.
3. Headers/presenters, validation/binding-group, and infra types follow as
   later rungs before re-probing `DataGrid.cs` itself.
