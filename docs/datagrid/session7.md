# DataGrid Port - Session 7

Date: 2026-06-06

## Goal

Add `DataGridTemplateColumn` as the next low-behavior concrete column and map
its WPF template surface to WinUI/Uno template types.

## What Changed

- Added a local partial `DataGridTemplateColumn` shell.
- Added `CellTemplate`, `CellTemplateSelector`, `CellEditingTemplate`, and
  `CellEditingTemplateSelector` dependency properties.
- Used WinUI `DataTemplate`, `DataTemplateSelector`, and `ContentPresenter`
  rather than introducing separate WPF template shims.
- Implemented display/edit generation by creating a content presenter when a
  template or selector is available.
- Bound the content presenter to the current data item through the local
  `System.Windows.Data.BindingOperations` bridge.
- Added reflection coverage for the template-column API surface.

## Verification

Commands:

```bash
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
```

Result: build succeeded; tests passed with 20 passed, 0 failed.

## Notes

The upstream WPF `DataGridTemplateColumn` includes sort coercion through
`CanUserSortProperty` and `OnCoerceCanUserSort`. That is intentionally deferred
until the base column and owner-control sorting contracts exist.

The generation methods can still hit dispatcher-bound WinUI paths if exercised
inside the plain NUnit desktop harness. Tests remain reflection-based until a
runtime harness can create Uno UI objects safely.

## Next Session

1. Add column event and collection API shells before attempting the owner
   `DataGrid` shell.
2. Start with `DataGridColumnEventArgs` and related event args, then
   `DataGridColumnCollection`.
3. Keep `DataGridComboBoxColumn` deferred unless a consumer needs it, because
   it pulls on selected-value/display-member/item-source binding contracts.
