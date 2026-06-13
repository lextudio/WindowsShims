# DataGrid Port - Session 33

Date: 2026-06-12

## Goal

Keyboard navigation: Up/Down arrows move the selection between rows.

## What Changed

- `DataGrid.OnKeyDown` (WinUI `KeyRoutedEventArgs`) handles Up/Down and marks
  the event handled.
- `MoveSelectionByOffset(delta)`: finds the currently selected row among the
  generated containers, clamps `current + delta` to range (or picks the
  first/last when nothing is selected), and routes through
  `HandleShimRowClicked` so the existing single-select + highlight +
  `SelectedItem` path is reused.
- Probe step "keyboard navigation moves selection (Down/Up)" and test
  `KeyboardNavigationSurfaceExists`. 116 tests; 17 probe steps; failures=0.

## Verification

```bash
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
cd WindowsShims/src/LeXtudio.Windows.Sample && dotnet run --framework net10.0-desktop -- --probe
```

Build succeeded; 116 passed/0 failed; probe `DONE failures=0`.

## Notes / honest limits

- Up/Down only (no Home/End/PageUp/PageDown, no cell-level focus movement).
- Requires the grid to have keyboard focus to fire `OnKeyDown`; the probe
  drives `MoveSelectionByOffset` directly.
- No `BringIntoView` on navigation yet (selected row may be offscreen).
- Still shim-driven single-select; WPF `Selector` pipeline inert.

## Next Session

1. `BringIntoView` on selection/navigation (scroll the row into the template
   `ScrollViewer`); add Home/End/PageUp/PageDown.
2. `Auto` column width (measure content); cell-level selection with
   hit-testing.
3. Route shim selection/sort through the WPF `Selector`/`SortDescriptions`
   pipelines.
