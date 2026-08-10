### Session 89 - RTF Round-Trip for Table Cell Formatting

Status: completed.

Scope:

- Sessions 82-88 covered table structure (text), hyperlinks, lists, and inline/
  paragraph formatting. This session makes the RTF round-trip preserve table
  cell formatting: background, borders (thickness + brush), and row spans. It
  also pins column spans and cell padding as WPF-faithful drops.

Findings:

- **The whole RTF converter stack already handled cell formatting.** The
  `XamlToRtfWriter` emits `\clcbpat` (background), `\clbrdr[lrtb]` (borders),
  and `\clvmgf`/`\clvmrg` (vertical merge) for rows; the `RtfToXamlReader`
  parses all of these back and emits `Background`, `BorderThickness`,
  `BorderBrush`, and `RowSpan` attributes on `<TableCell>`. Two shim-side gaps
  dropped the formatting before it ever reached RTF:
  1. `TextSchema._tableCellProperties` excluded `ColumnSpan`, `RowSpan`,
     `Padding`, `BorderThickness`, `BorderBrush` under a `#if !HAS_UNO` guard
     (a Uno-port hack), so the intermediate XAML never carried them.
  2. `XamlReader.ParseTable` read no attributes at all, so even attributes that
     did reach XAML (e.g. `Background`) were dropped on load.
- **Column spans and cell padding cannot round-trip through RTF by design.**
  The converter never writes `\clgridspan`, and `WriteCellPadding` is empty —
  both match upstream WPF exactly (the empty method is present in the original
  import). These are WPF-faithful drops, not port gaps.
- `Background` was already serialized (it is outside the guard); it only needed
  the `ParseTable` fix.

Changes:

- `TextSchema.cs`: removed the `#if !HAS_UNO` guard from `_tableCellProperties`,
  restoring the full WPF list (ColumnSpan, RowSpan, Padding, BorderThickness,
  BorderBrush) so `WriteXaml` serializes them when non-default.
- `XamlReader.cs` (`ParseTable`): now reads attributes on `<TableCell>`
  (ColumnSpan, RowSpan, Padding, BorderThickness, BorderBrush, Background),
  `<TableRow>`/`<TableRowGroup>` (Background), and `<Table>` (Background).
- `MainPage.cs`: snapshot gained `firstTableCellBackground`,
  `firstTableCellBorderThickness`, `firstTableCellBorderBrush`,
  `firstTableCellPadding`, `firstTableCellRowSpan`, `firstTableCellColumnSpan`
  (first cell in the first table block) plus a `FirstTableCell` walker.

Tests:

- Integration (4 new, 222/222 total):
  - `SaveLoad_Rtf_RoundTripsTableCellBackground` — `#FFFF0000` survives.
  - `SaveLoad_Rtf_RoundTripsTableCellBorders` — `BorderThickness` (reported as
    the shim's `[Thickness: 1-1-1-1]`) and `BorderBrush` `#FF000000` survive.
  - `SaveLoad_Rtf_RoundTripsTableCellRowSpan` — a `RowSpan="2"` cell survives
    `\clvmgf`/`\clvmrg` round-trip.
  - `SaveLoad_Rtf_DropsTableCellColumnSpanAndPaddingLikeWpf` — pins the
    WPF-faithful drop: `ColumnSpan` reverts to 1 and cell `Padding` is lost.
- Model tests: 234/234 (`LeXtudio.Windows.Tests`).

Result:

- 222/222 RichTextBox integration tests pass; 234/234 model tests pass.
- Table cell background, borders, and row spans now round-trip through RTF;
  column spans and cell padding are documented as WPF-faithful drops.
