### Session 94 - RTF Round-Trip Coverage for Nested Tables

Status: completed.

Scope:

- Sessions 81-93 added RTF round-trips for text, inline/paragraph formatting,
  lists, hyperlinks, table cells/columns, super/subscript, inline language,
  and paragraph borders. This session adds regression coverage confirming
  nested tables (a table inside a table cell) survive RTF save/load intact.

Findings:

- **Nested tables already round-trip with no shim changes.** The RTF writer
  emits `\nesttableprops`/`\nestrow`/`\nestcell` for a table inside a cell,
  and `RtfToXamlReader` reconstructs the inner `<Table>` block. On the shim
  side, `ParseTable` parses cell content via `ParseBlock`, which already
  dispatches nested `<Table>` elements — so the load path needed no work.
- Other remaining candidates were probed and confirmed WPF-faithful drops:
  `MarkerOffset` has an attribute-table entry but no writer case, and
  `TableColumn.Background` is serialized to XAML but the RTF reader re-emits
  only column widths (RTF encodes cell backgrounds, not column backgrounds).

Changes:

- `MainPage.cs`: snapshot gained `firstTableCellHasNestedTable` (whether the
  first cell of the first table contains a nested `Table` block).

Tests:

- Integration (1 new, 228/228 total):
  - `SaveLoad_Rtf_RoundTripsNestedTable` — an outer 1x1 table whose cell
    contains a paragraph plus an inner 1x1 table reloads with both tables
    intact and all text present.
- Model tests: 234/234 (`LeXtudio.Windows.Tests`).

Result:

- 228/228 RichTextBox integration tests pass; 234/234 model tests pass.
- Nested tables are verified to round-trip through RTF save/load.
