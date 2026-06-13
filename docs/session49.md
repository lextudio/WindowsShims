# DataGrid Port - Session 49

Date: 2026-06-12

## Goal

Row headers — render the left-edge row-header column (honoring
`HeadersVisibility`) with a current-row / editing / error glyph.

## What Changed

- `DataGrid.AreRowHeadersVisible` (`HeadersVisibility` has the `Row` bit) and
  `RowHeaderShimWidth` (`RowHeaderWidth` or 24).
- The row template gains a separate `PART_RowHeader` `ContentControl` before
  `PART_CellsHost`, so the row header does not shift the column-indexed cells.
- `DataGridRow.BuildRowHeader`: when row headers are visible, fills
  `PART_RowHeader` with a `DataGridRowHeader` (width = `RowHeaderShimWidth`);
  otherwise collapses it. `RefreshRowHeaderGlyph` sets the glyph by priority:
  validation error `⚠` > editing `✎` > current/selected `▶` > none. It is
  refreshed from selection, edit-state (`IsEditing` is now a backing-field
  property), and row-error changes.
- `BuildHeaderRow` prepends a top-left corner placeholder (width
  `RowHeaderShimWidth`) when row headers are visible, so column headers stay
  aligned with the row-header-indented cells. `_headerCells` still tracks only
  the column headers (Auto-width pass unaffected).
- Probe: default `HeadersVisibility=Column` keeps earlier steps stable; a
  dedicated step flips to `All`, selects row 0, and asserts its `PART_RowHeader`
  holds a `DataGridRowHeader` showing `▶` while cells remain column-indexed.
  Test guards for `AreRowHeadersVisible` / `BuildRowHeader`. 121 tests; 30
  probe steps; failures=0.

## Verification

Build succeeded; 121 passed/0 failed; probe `DONE failures=0` —
"row0 header glyph = '▶', cell0 col matches = True".

## Notes / honest limits

- Glyph-only row header (no row-header content/template, no `RowHeaderStyle`,
  no drag-to-select via the header, no resize).
- The corner header is an empty placeholder (no corner content/template).
- `HeadersVisibility` is read at build time; switching it requires a rebuild
  (the probe calls `BuildShimVisualTree`).
- Column-header row still uses plain `DataGridColumnHeader`s; row/column header
  styling is minimal.

## Next Session

1. Row-header pointer select / drag-select (click the row header to select the
   row).
2. `RowDetails` (`RowDetailsTemplate` + `RowDetailsVisibilityMode`) expandable
   panel under rows.
3. Alternating row backgrounds (`AlternatingRowBackground`/`AlternationCount`).
