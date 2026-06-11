# DataGrid Port - Session 3

Date: 2026-06-06

## Goal

Add the agreed WPF binding bridge over Uno/WinUI binding, then use it to move
the DataGrid column surface forward without porting WPF's full binding engine.

## What Changed

- Added `System.Windows.Data.BindingBase`, `Binding`, `PropertyPath`,
  `BindingMode`, and `UpdateSourceTrigger` in `System.Windows/Data/Binding.cs`.
- The binding bridge stores WPF-shaped state and has `ToWinUIBinding()` for
  runtime conversion to `Microsoft.UI.Xaml.Data.Binding`.
- Added converter adaptation from WPF `IValueConverter` to WinUI
  `IValueConverter`.
- Added `LeXtudio.Windows.Tests` as an internals-visible friend so tests can
  cover non-dispatcher mapping/converter helpers.
- Added a local partial `System.Windows.Controls.DataGridColumn` shell with
  core column properties and `ClipboardContentBinding`.
- Linked upstream `DataGridClipboardCellContent.cs`.
- Added tests for binding state/converter adaptation, `DataGridColumn` type
  surface, and clipboard cell content construction.

## Verification

Command:

```bash
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop
```

Result: 13 passed, 0 failed.

## Notes

Plain NUnit cannot instantiate Uno `Microsoft.UI.Xaml.Data.Binding` or a
`DependencyObject`-derived `DataGridColumn` because those paths expect a Uno
dispatcher queue. The production bridge still creates WinUI bindings for runtime
use; tests intentionally cover the WPF-side state, enum mapping, converter
adapter, and type availability.

The upstream `DataGridColumn.cs` source-link remains blocked. The session 2
probe showed Uno's dependency-object source generator emits a partial for the
type, while the WPF source file declares a non-partial class. The local partial
shell is the current safe path.

## Next Session

1. Add the smallest `DataGridCell` shell needed by bound columns.
2. Probe `DataGridBoundColumn.cs`; expect missing element generation/editing
   contracts rather than binding-base blockers.
3. Decide whether `DataGridTextColumn` can be a local shell over WinUI
   `TextBlock`/`TextBox` before attempting source-linked behavior.
