# DataGrid Port - Session 1

Date: 2026-06-06

## Goal

Start the DataGrid port without repeating the RichTextBox mistake of enabling a
high-coupling root file too early. The session target was a green build with a
small WPF-linked DataGrid surface and a written plan for the larger dependency
ladder.

## What Changed

- Added WPF source links for independent DataGrid API files in
  `src/LeXtudio.Windows/LeXtudio.Windows.csproj`:
  - `DataGridLength.cs`
  - `DataGridLengthConverter.cs`
  - `DataGridLengthUnitType.cs`
  - `DataGridClipboardCopyMode.cs`
  - `DataGridEditAction.cs`
  - `DataGridEditingUnit.cs`
  - `DataGridGridLinesVisibility.cs`
  - `DataGridHeadersVisibility.cs`
  - `DataGridRowDetailsVisibilityMode.cs`
  - `DataGridSelectionMode.cs`
  - `DataGridSelectionUnit.cs`
- Added `MS.Internal/DataGridSizingShims.cs` with the small `DoubleUtil` and
  `PixelUnit` subset required by `DataGridLengthConverter`.
- Added DataGrid length SR constants to `System.Windows/Documents/SR.cs`.
- Wrote the initial long-form DataGrid plan in `docs/DATAGRID.md`.

## Verification

Command:

```bash
dotnet build WindowsShims/src/LeXtudio.Windows/LeXtudio.Windows.csproj
```

Result: build passed for `net10.0-desktop` with 87 warnings and 0 errors.

The warnings are existing XML-doc/hiding/platform warnings plus two expected
upstream XML-doc warnings from `DataGridLength.cs` and
`DataGridLengthConverter.cs`.

## Notes

`DataGrid.cs` is intentionally not enabled yet. It is coupled to the WPF
items/selector stack, virtualization, automation peers, command routing,
template parts, and collection views. The safer next step is to attempt
`DataGridColumn.cs` in isolation, catalog the exact compile errors, and build
the smallest framework bridge set needed for the column model.

## Next Session

1. Add focused tests for `DataGridLength` construction, parsing, star sizing,
   and non-pixel units.
2. Probe `DataGridColumn.cs` and write down its first-order missing contracts.
3. Decide whether to source-link `DataGridColumn.cs` directly or introduce a
   temporary local column shell while the control root remains blocked.
