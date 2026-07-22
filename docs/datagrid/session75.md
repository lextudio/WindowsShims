# DataGrid Port - Session 75

Date: 2026-06-13

## Goal

Complete the visible grid-line surface started in session 74 by extending the
same linked `GridLinesVisibility` rebuild path to column headers and row
headers.

## What Changed

- `DataGridColumnHeader` now exposes `HasShimGridLine` and
  `ApplyShimGridLines()`.
- `DataGridRowHeader` now exposes `HasShimGridLine` and
  `ApplyShimGridLines()`.
- `DataGrid.BuildHeaderRow()` applies grid-line state to realized column
  headers.
- `DataGridRow.BuildRowHeader()` applies grid-line state to realized row
  headers.

The mapping matches the cell behavior from session 74:

- `None`: no ordinary grid-line border.
- `Horizontal`: bottom border using `HorizontalGridLinesBrush`.
- `Vertical`: right border using `VerticalGridLinesBrush`.
- `All`: right and bottom borders.

This continues to reuse upstream WPF `OnNotifyGridLinePropertyChanged`, which
regenerates containers through `OnItemTemplateChanged(null, null)`. No new
upstream fork patch was needed.

## Probe

Expanded the grid-line probe to cover all realized surfaces:

- Data cell.
- Column header.
- Row header, with `HeadersVisibility.All` enabled for the probe scope.

The probe verifies `None`, `Horizontal`, `Vertical`, and `All`, including brush
identity for horizontal and vertical modes.

## Verification

```
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj --framework net10.0-desktop --no-restore
dotnet build src/LeXtudio.Windows.Sample/LeXtudio.Windows.Sample.csproj --framework net10.0-desktop --no-restore
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --no-restore
dotnet run --project src/LeXtudio.Windows.Sample/LeXtudio.Windows.Sample.csproj --framework net10.0-desktop -- --probe
```

Results:

- Final incremental library build: 0 errors, 0 warnings.
- Tests: 136 passed, 0 failed.
- Probe: `DONE failures=0`.
- Full sample/library builds still show the existing upstream warning set.

## Still Deferred

- Corner header grid-line rendering.
- Exact WPF grid-line thickness and clipping behavior.
- DataGridCellsPanel-based layout and virtualization.
- Style setter application.

## Next Batch

The next larger code-reuse step should move back toward WPF control behavior:

1. `RowStyleSelector` / `ItemContainerStyleSelector` substrate if styling
   behavior is the priority.
2. Header resize/reorder gripper groundwork if column interaction is the
   priority.
