# DataGrid Port - Session 84

Date: 2026-06-13

## Goal

Continue reducing local shims and `#if !HAS_UNO` guards in key DataGrid
header code, building on session 83.

## Changes

### Guards removed in `DataGridColumnHeader.cs`

Removed guards around:
- `PrepareColumnHeader(object item, DataGridColumn column)`
- `DisplayIndexPropertyKey` / `DisplayIndexProperty` / `DisplayIndex`
- `SortDirectionPropertyKey` / `SortDirectionProperty` / `SortDirection`

`OnDisplayIndexChanged` is now compiled on Uno, but its gripper-specific body
remains under `#if !HAS_UNO` until resize gripper support is enabled.

### Shim changes

#### `ContentControl.cs`

Added WPF-compatible `ContentStringFormatProperty` and `ContentStringFormat`.
This lets upstream header preparation and transfer-property code compile
without a local header-specific workaround.

#### `ButtonBase.cs`

Added `OnVisualStatePropertyChanged` no-op shim. `DataGridColumnHeader` inherits
from the local `ButtonBase` shim rather than the local `Control` shim, so the
upstream sort-direction metadata needed the same callback surface here.

#### `DataGridColumnHeader.cs` shim partial

Removed the local `SortDirection => Column?.SortDirection` forwarding property.
The upstream read-only `SortDirection` dependency property is now compiled.

## Guard counts after session 84

| File | `#if` count |
|---|---:|
| `DataGridColumnHeader.cs` | 13 |
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
