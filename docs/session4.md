# DataGrid Port - Session 4

Date: 2026-06-06

## Goal

Move from the base column shell into bound-column infrastructure without
pulling in the full WPF DataGrid control or WPF binding engine.

## What Changed

- Added `System.Windows.Data.BindingOperations` as a WPF-facing facade.
- `BindingOperations.SetBinding` delegates to Uno
  `Microsoft.UI.Xaml.Data.BindingOperations.SetBinding` after converting the
  WPF-shaped `BindingBase`.
- `BindingOperations.ClearBinding` clears the target dependency property.
- Added a minimal `DataGridCell` shell deriving from `ContentControl`, with
  `IsEditing`, `Column`, and internal `BuildVisualTree()`.
- Added a local partial `DataGridBoundColumn` shell with `Binding`,
  `ElementStyle`, `EditingElementStyle`, `ClipboardContentBinding`,
  `ApplyBinding`, `ApplyStyle`, and refresh behavior.
- Extended `DataGridColumn` with `SortMemberPath`, virtual clipboard binding,
  and virtual refresh/generation hooks used by bound-column code.
- Added reflection-based tests for the new bound-column/cell/binding-ops
  surface and included the test file in the explicit test project compile list.

## Verification

Commands:

```bash
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
```

Result: build succeeded; tests passed with 17 passed, 0 failed.

## Notes

Direct instantiation tests are still avoided for `DependencyObject`-derived
types because the plain NUnit desktop run does not create the Uno dispatcher
queue. The new tests verify API shape and dependency-property fields only.

The local `DataGridBoundColumn` shell intentionally does not source-link the
upstream WPF file yet. The WPF implementation depends on metadata coercion and
the wider DataGrid owner/cell generation stack; current WindowsShims coercion is
a no-op bridge.

## Next Session

1. Add a local `DataGridTextColumn` shell that can generate a Uno `TextBlock`
   and bind its `Text` property through `DataGridBoundColumn.ApplyBinding`.
2. Decide whether editing should wait for a WPF-shaped `TextBox` shim or use a
   temporary Uno `TextBox` bridge.
3. Add tests for `DataGridTextColumn` type surface and protected generation
   hooks without requiring runtime dispatcher construction.
