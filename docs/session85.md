# DataGrid Port - Session 85

Date: 2026-06-13

## Goal

Continue reducing header-local shims after sessions 83-84 by moving column
association onto the upstream WPF header path.

## Changes

### Guard removed in `DataGridColumnHeader.cs`

Removed the guard around the upstream `Column` property.

### Shim reduction

#### `DataGrid.cs`

`BuildHeaderRow()` now calls upstream
`DataGridColumnHeader.PrepareColumnHeader(column.Header, column)` instead of
assigning a shim-only `Column` setter.

The existing shim render path still reapplies `HeaderContent(column)` after
preparation so the live sort glyph behavior remains unchanged.

#### `DataGridColumnHeader.cs` shim partial

Removed the local duplicate `Column` property/setter. Header column association
now flows through upstream `_column` and `PrepareColumnHeader`.

## Guard counts after session 85

| File | `#if` count |
|---|---:|
| `DataGridColumnHeader.cs` | 12 |
| `DataGridRow.cs` | 19 |
| `DataGridCell.cs` | 20 |

## Verification

```
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj
dotnet test src/LeXtudio.Windows.Tests/
dotnet run --project src/LeXtudio.Windows.Sample/ --framework net10.0-desktop -- --probe
```

Results:
- Build: 0 errors
- Tests: Passed 136 / Failed 0
- Probe: `DONE failures=0`
