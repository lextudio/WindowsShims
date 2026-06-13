# DataGrid Port - Session 74

Date: 2026-06-13

## Goal

Take the next larger reuse step after the style-notification batches by turning
the already-linked `GridLinesVisibility` notification path into visible shim
behavior.

## What Changed

- `DataGridCell` now has a `HasShimGridLine` marker.
- `DataGridCell.ApplyShimGridLines()` maps `DataGrid.GridLinesVisibility` to
  lightweight cell borders:
  - `None` clears the ordinary grid-line border.
  - `Horizontal` draws the bottom border.
  - `Vertical` draws the right border.
  - `All` draws right and bottom borders.
- Grid-line brushes are read from linked WPF properties:
  `HorizontalGridLinesBrush` and `VerticalGridLinesBrush`.
- `DataGridRow.BuildCells()` applies grid-line state during cell realization.

This reuses upstream WPF `OnNotifyGridLinePropertyChanged`, which regenerates
containers through `OnItemTemplateChanged(null, null)`. The shim render path
already rebuilds through that route, so no new upstream fork patch was needed.

Current/validation borders remain higher priority: `ApplyShimGridLines()` does
not override a validation error or current-cell focus border. When current
focus moves away from a cell, it now returns to the ordinary grid-line border
instead of an empty border.

## Probe

Replaced the old session-70 no-crash grid-line probe with visible rendering
coverage:

- Sets custom horizontal and vertical grid-line brushes.
- Verifies `None` clears realized cell borders.
- Verifies `Horizontal` sets a bottom border using the horizontal brush.
- Verifies `Vertical` sets a right border using the vertical brush.
- Verifies `All` sets both right and bottom borders.
- Restores the previous grid-line settings.

## Verification

```
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj --framework net10.0-desktop --no-restore
dotnet build src/LeXtudio.Windows.Sample/LeXtudio.Windows.Sample.csproj --framework net10.0-desktop --no-restore
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --no-restore
dotnet run --project src/LeXtudio.Windows.Sample/LeXtudio.Windows.Sample.csproj --framework net10.0-desktop -- --probe
```

Results:

- Builds: 0 errors, existing full-build warning set.
- Tests: 136 passed, 0 failed.
- Probe: `DONE failures=0` (48 steps; one existing grid-line step now asserts
  visible behavior instead of just safe notification).

## Still Deferred

- Header grid-line rendering.
- Row-header grid-line rendering.
- Exact WPF grid-line thickness / clipping behavior.
- DataGridCellsPanel-based layout.

## Next Batch

The next large reuse target should be either:

1. Header/row-header grid-line visuals, completing the visible grid-line
   surface.
2. `RowStyleSelector` / `ItemContainerStyleSelector` substrate if style
   selectors need to be prioritized before layout work.
