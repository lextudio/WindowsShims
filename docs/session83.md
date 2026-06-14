# DataGrid Port - Session 83

Date: 2026-06-13

## Goal

Continue reducing local `#if !HAS_UNO` guards in key DataGrid types by enabling
more upstream WPF visual-state code on the Uno path.

## Changes

### Guard removed in `DataGridColumnHeader.cs`

Removed the `#if !HAS_UNO` wrapper around
`DataGridColumnHeader.ChangeVisualState(bool)`.

This enables the upstream WPF header VSM transitions on Uno:
- common states: `Pressed`, `MouseOver`, `Normal`
- sort states: `SortAscending`, `SortDescending`, `Unsorted`
- validation state hook via `ChangeValidationVisualState`

### Shim additions

#### `DataGridHelperStubs.cs` - `VisualStates`

Added missing header visual-state constants:
- `StatePressed`
- `StateUnsorted`
- `StateSortAscending`
- `StateSortDescending`

#### `ButtonBase.cs`

Added minimal state surface needed by the upstream header VSM:
- `IsPressed`
- pointer press/release/capture-lost updates that call `UpdateVisualState()`
- `ChangeValidationVisualState(bool)` shim that currently transitions to
  `Valid`

`Validation.cs` is present in upstream WPF but is not compiled into the shim
target yet, so full invalid-state routing remains future work.

#### `DataGridColumnHeader.cs` shim partial

Added `SortDirection` forwarding to `Column?.SortDirection`, matching the
effective value returned by upstream coercion for realized headers.

## Guard counts after session 83

| File | `#if` count |
|---|---:|
| `DataGridColumnHeader.cs` | 17 |
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
