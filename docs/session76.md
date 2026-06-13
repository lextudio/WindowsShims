# DataGrid Port - Session 76

Date: 2026-06-13

## Goal

Take the next style-reuse step by moving `RowStyleSelector` onto the linked WPF
selector surface and making realized rows honor the WPF row-style precedence.

## What Changed

- Linked upstream WPF `StyleSelector.cs`.
- Added `ItemsControl.ItemContainerStyle` and
  `ItemsControl.ItemContainerStyleSelector` CLR accessors over the existing
  dependency properties.
- Added a `HAS_UNO` row notification to linked WPF
  `DataGrid.OnRowStyleSelectorChanged`, matching the session-72 `RowStyle`
  notification bridge.
- `DataGridRow.ApplyShimRowStyle()` now computes effective row style as:
  `RowStyle ?? ItemContainerStyle ?? (RowStyleSelector ??
  ItemContainerStyleSelector)?.SelectStyle(item, row)`.
- Realized rows refresh their marker when `RowStyleSelector` changes.

This keeps setter application deferred, but removes another local-only styling
shim by using the WPF selector contract directly.

## Probe

Added a runtime probe step that:

- Assigns a custom WPF-style `StyleSelector`.
- Verifies the realized first row receives the selected style.
- Verifies explicit `RowStyle` overrides the selector.
- Clears `RowStyle` and verifies selector fallback reapplies.

## Verification

```
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj --framework net10.0-desktop --no-restore
dotnet build src/LeXtudio.Windows.Sample/LeXtudio.Windows.Sample.csproj --framework net10.0-desktop --no-restore
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --no-restore
dotnet run --project src/LeXtudio.Windows.Sample/LeXtudio.Windows.Sample.csproj --framework net10.0-desktop -- --probe
```

Results:

- Library build: 0 errors, existing upstream warning set.
- Sample build: 0 errors, 0 warnings.
- Tests: 136 passed, 0 failed.
- Probe: `DONE failures=0`.

## Still Deferred

- Actual application of style setters to row visuals.
- Live notifications for base `ItemContainerStyle` /
  `ItemContainerStyleSelector` changes.
- Full WPF item-container generator style assignment path.

## Next Batch

The next large reuse step should target header interaction substrate:
column-header resize/reorder gripper plumbing can reuse more linked WPF
header notification and input paths than another local visual marker batch.
