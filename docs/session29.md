# DataGrid Port - Session 29

Date: 2026-06-12

## Goal

Column fidelity (session-28 next step #2): use real `DataGridColumnHeader`
controls for the header row and honor explicit column `Width` instead of the
flat 120px fallback.

## Outcome

- The header row is now built from `DataGridColumnHeader` controls (each with
  its `Column` set and `Content` = the column header), not plain
  `TextBlock`s.
- Explicit pixel column widths are honored. Probe: the Age column with
  `Width = new DataGridLength(60)` renders a 60px-wide header, and the grid's
  `DesiredSize` width drops 386 → 326 (one column 60px narrower than the
  previous flat 120).

## What Changed

### DataGrid render path

- `BuildHeaderRow` now creates `DataGridColumnHeader` instances (Column,
  Content, Width, SemiBold) instead of `TextBlock`s.
- `ShimColumnWidth` honors `DataGridLength.IsAbsolute` (uses `Value`);
  `ActualWidth` still wins if ever set; `Auto`/`SizeToCells`/`SizeToHeader`/
  `Star` still fall back to 120 (no width computation yet). Cells already use
  `ShimColumnWidth`, so headers and cells stay aligned.

### Sample probe + tests

- The Age column now carries `Width = DataGridLength(60)`; a probe step
  asserts the first header is a `DataGridColumnHeader` and the Age header's
  width is 60.
- Test `ColumnWidthResolverExists` pins `ShimColumnWidth`. 113 tests total.

## Verification

```bash
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
cd WindowsShims/src/LeXtudio.Windows.Sample && dotnet run --framework net10.0-desktop -- --probe
```

Result: build succeeded; 113 tests passed, 0 failed; probe `DONE failures=0`
— "Age header width = 60", `DesiredSize=326,119.5`.

## Notes / honest limits

- Only absolute (pixel) widths are honored. `Auto`/`SizeToCells`/
  `SizeToHeader` would need a measure pass to size to content; `Star` needs
  total-width distribution. All still fall back to 120px, so mixed-unit grids
  won't size as WPF does.
- `DataGridColumnHeader` renders via the default WinUI `ContentControl`
  template (from the merged `XamlControlsResources`); it is not styled like a
  WPF column header (no sort glyph, separators, or hover/press states), and
  header click does not sort.
- No column reordering / resizing by the user; widths are read once at render.
- Selection, headers, and rows still do not scroll-into-view; no editing.

## Next Session

1. Honor `Auto` column width: measure header + cell content and apply a
   uniform per-column width so columns size to content and stay aligned.
2. Cell-level selection with hit-testing (which `DataGridCell` was pressed),
   honoring `SelectionUnit`, reflecting `DataGridCell.IsSelected` distinctly.
3. Header click → sort (toggle `SortDirection`, reorder items) as a first
   header interaction.
