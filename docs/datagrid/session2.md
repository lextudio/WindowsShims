# DataGrid Port - Session 2

Date: 2026-06-06

## Goal

Move beyond the session 1 compile-only DataGrid surface by adding focused tests
for `DataGridLength`, then probe `DataGridColumn.cs` to identify the next
source-link blockers without leaving the main build broken.

## What Changed

- Added `LeXtudio.Windows.Tests` as an NUnitLite executable test project.
- Added the test project to `WindowsShims.slnx`.
- Added `DataGridLengthTests` covering:
  - pixel construction and stored values
  - star sizing conversion
  - `Auto`, `SizeToCells`, and `SizeToHeader` conversion
  - `in`, `cm`, and `pt` physical unit conversion
  - invalid infinity construction
- Pinned `Tmds.DBus.Protocol` to `0.92.0` in the test project to avoid the
  transitive vulnerable version warning from Uno dependencies.
- Probed upstream `DataGridColumn.cs` by temporarily source-linking it, then
  removed the temporary link after collecting errors.
- Updated `DATAGRID.md` with the test status and column probe results.

## Verification

Commands:

```bash
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop
dotnet build WindowsShims/src/LeXtudio.Windows/LeXtudio.Windows.csproj --framework net10.0-desktop
```

Results:

- Tests passed: 9 passed, 0 failed.
- Library build passed with 87 warnings and 0 errors.

The warning set is the existing WindowsShims XML-doc/hiding/platform warning
set plus the known upstream `DataGridLength` XML-doc warnings from session 1.

## Probe Notes

Temporary source-linking `DataGridColumn.cs` failed with 15 errors. The first
error was a `DataGridColumn` partial collision, even though there is no
hand-written `DataGridColumn` source under `WindowsShims/src`; treat this as a
generated/reference-source collision to investigate before attempting a direct
source-link.

The other first-order missing contracts were:

- `DataGrid`
- `DataGridRow`
- `DataGridCell`
- `DataGridCellClipboardEventArgs`
- `DataGridNotificationTarget`
- `BindingBase`
- `InputEventArgs`
- `ItemPropertyInfo`

## Next Session

1. Resolve the `DataGridColumn` partial-collision source. Check Uno SDK
   generated/reference compile items before changing WPF source.
2. Add minimal `System.Windows.Data.BindingBase` and input/item metadata shims if
   no existing bridge already covers them.
3. Prefer a temporary local `DataGridColumn` shell if the generated partial
   collision makes direct upstream linking too expensive for the next step.
