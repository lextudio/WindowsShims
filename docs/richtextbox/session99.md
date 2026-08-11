### Session 99 - Visual Rendering for Table Columns and Cells

Status: completed.

Scope:

- Sessions 89/92 made table cell formatting and column widths round-trip
  through RTF, but `FlorenceEngine.FormatTable` flattened every cell
  paragraph at full width with no column layout and no cell visuals. This
  session makes column widths drive the layout and renders cell
  backgrounds/borders.

Findings:

- `FormatTable` walked the table's text range and formatted every paragraph
  (including cell paragraphs) at `availWidth` with x=0 — no columns, no cell
  boxes.
- The line model is flat (one `FlorenceLine` per text row), so a true
  side-by-side grid (cells' k-th lines merged into shared row bands) would
  break caret/hit-testing. The pragmatic layout: cells flow sequentially,
  but each cell's paragraphs are constrained to the cell's column width and
  positioned at the column's x offset, and each cell with a background or
  border emits a cell box at that position. Column widths come from
  `Table.Columns` (absolute `TableColumn.Width` values, scaled down if they
  overflow the available width) or an equal split of the available width by
  the maximum cells-per-row.

Changes:

- `FlorenceEngine.cs`:
  - `FormatParagraph` gained an `xOffset` parameter (run X positions, line
    emission, and the paragraph border box are shifted); `EmitLine` takes the
    offset too.
  - New `FlorenceCellBox` (bounds, background, border brush/thickness) and
    `FlorencePage.CellBoxes`.
  - `FormatTable` rewritten: `GetColumnWidths` (explicit widths or equal
    split), per-column x starts, cell paragraphs formatted at their column
    width/offset, and a cell box emitted per cell with a background or
    border.
- `FlowDocumentView.uno.cs`:
  - `BuildBorderSides` extracted as a generic helper; `BuildCellVisuals`
    renders a filled rectangle for the cell background plus one rectangle per
    non-zero border side.
  - Cell visuals added to the canvas before the line blocks and arranged at
    their bounds. New `CellVisualRectCount` and `CellBoxLayout`
    ("X:Width" pairs) for tests.
- `MainPage.cs`: snapshot gained `cellVisualRectCount` and `cellBoxLayout`.

Tests:

- Integration (extended 3, 232/232 total):
  - `SaveLoad_Rtf_RoundTripsTableColumnWidths` now asserts
    `cellBoxLayout == "0:100,100:200"` — the cell boxes sit at the columns'
    x positions with their widths.
  - `SaveLoad_Rtf_RoundTripsTableCellBackground` asserts the background
    renders as one filled rectangle.
  - `SaveLoad_Rtf_RoundTripsTableCellBorders` asserts the border renders as
    four side rectangles.
- Model tests: 234/234 (`LeXtudio.Windows.Tests`).

Result:

- 232/232 RichTextBox integration tests pass; 234/234 model tests pass.
- Table column widths drive the Florence layout, and cell backgrounds and
  borders render in `FlowDocumentView` after RTF save/load. Known limitation
  (documented): cells flow vertically rather than as a true side-by-side
  grid; `ColumnSpan`/`RowSpan` are not honored by the layout.
