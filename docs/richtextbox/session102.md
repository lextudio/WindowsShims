### Session 102 - Table Layout Honors ColumnSpan and RowSpan

Status: completed.

Scope:

- Session 101 laid out table cells side by side but each cell occupied exactly
  one column slot. This session makes the layout honor `ColumnSpan`
  (a cell's box spans the summed widths of its columns) and `RowSpan` (a
  cell's box extends over the spanned rows' heights).

Findings:

- `ColumnSpan` does not round-trip through RTF (WPF-faithful drop, pinned in
  session 89 — the converter never writes `\clgridspan`), so the layout test
  builds the document directly via `set-xaml-document` (which applies
  `ColumnSpan` since session 89). `RowSpan` does round-trip
  (`\clvmgf`/`\clvmrg`), so that test uses the RTF round-trip.
- The flat line model constrains the row-span approach: a row-spanned cell's
  text stays in its first row's band (WPF vertically centers it over the
  spanned height), but its background/border box extends over the spanned
  rows once all row heights are known.

Changes:

- `FlorenceEngine.cs` (`FormatTable`):
  - Cells advance the column cursor by `ColumnSpan`; a spanning cell's width
    is the sum of its columns' widths, and following cells start after the
    spanned columns.
  - `GetColumnWidths`'s equal-split branch counts effective columns
    (spanned cells widen the grid) instead of raw cell counts.
  - Row Y positions and row-spanned cell boxes are recorded during layout;
    after all rows are laid out, each row-spanned box's height is extended
    over the spanned rows (the box is mutable now).
- `FlowDocumentView.uno.cs`: new `CellBoxHeightLayout` (comma-joined box
  heights) for tests.
- `MainPage.cs`: snapshot gained `cellBoxHeightLayout`.

Tests:

- Integration (2 new/extended, 234/234 total):
  - `SaveLoad_Rtf_RoundTripsTableCellRowSpan` (extended) — the row-spanned
    cell's box height equals the sum of both rows' heights.
  - `TableColumnSpan_LayoutSpansColumns` — a 3-column table (100px each) with
    a span-2 cell renders boxes at `0:200` and `200:100`.
- Model tests: 234/234 (`LeXtudio.Windows.Tests`).

Result:

- 234/234 RichTextBox integration tests pass; 234/234 model tests pass.
- The Florence table layout now honors both `ColumnSpan` and `RowSpan`
  (row-span text remains top-aligned in its first row's band).
