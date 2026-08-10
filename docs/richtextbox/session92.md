### Session 92 - RTF Round-Trip for Table Column Widths

Status: completed.

Scope:

- Sessions 81-91 covered RTF round-trips for text, inline/paragraph formatting,
  lists, hyperlinks, table cell formatting, super/subscript, and inline
  language. This session makes `TableColumn.Width` survive RTF save/load
  (`\clwWidth`/`\cellx` twips ↔ `<TableColumn Width>` px).

Findings:

- **The upstream RTF stack already handled column widths on both sides.**
  `XamlToRtfWriter` converts `<TableColumn Width="100">` into per-cell
  `\clftsWidth3\clwWidth1500\cellx1500` (px → twips), and `RtfToXamlReader`
  reconstructs `<Table.Columns><TableColumn Width="<px>"/></Table.Columns>`
  from the `\cellx` values. Three shim-side gaps blocked the round-trip:
  1. `TableColumn.OnEnterParentTree()` NRE'd when a column was added: the
     shim's `TextElement.InsertLogicalChild` only set `Parent` for
     `TextElement` children, and `TableColumn` derives directly from
     `FrameworkContentElement` — so `TableColumn.Table` (`Parent as Table`)
     was null when `Table.InvalidateColumns()` ran. Now any
     `FrameworkContentElement` child gets parented.
  2. `WriteXamlAtomicElement` serialized `TableColumn.Width` through
     `TypeDescriptor.GetConverter(GridLength)`, which under Uno returns the
     WinUI converter emitting `"100px"` — unparseable by
     `Converters.StringToDouble` (plain `double.Parse`), so the RTF writer
     dropped the width. Under `HAS_UNO` the atomic serialization now routes
     through the same `DPTypeDescriptorContext.GetStringValue` shim used for
     inheritable properties, and a `TableColumn.WidthProperty` case emits the
     WPF `GridLengthConverter` forms (`"100"`, `"100*"`, `"Auto"`).
  3. The shim `XamlReader.ParseTable` had no `TableColumn`/`Table.Columns`
     cases, so neither the RTF reader's re-emit nor user XAML with explicit
     column widths was applied on load.

Changes:

- `ext/wpf` submodule:
  - `TextRangeSerialization.cs` (`WriteXamlAtomicElement`): routes property
    serialization through `DPTypeDescriptorContext.GetStringValue` under
    `HAS_UNO` instead of the raw TypeDescriptor path.
  - `DPTypeDescriptorContext.cs` (`GetStringValue`): new
    `TableColumn.WidthProperty` case emitting the WPF `GridLengthConverter`
    string forms.
- `TextElement.uno.cs` (`InsertLogicalChild`): parent any
  `FrameworkContentElement` child, not just `TextElement` (fixes
  `TableColumn.Parent`/`TableColumn.Table`).
- `XamlReader.cs` (`ParseTable`): new `TableColumn` case (parses `Width` as a
  pixel `GridLength` and adds it to `table.Columns`) and a `Table.Columns`
  wrapper case.
- `MainPage.cs`: snapshot gained `firstTableColumnWidths` (comma-joined widths
  of the first table's columns).

Tests:

- Integration (1 new, 226/226 total):
  - `SaveLoad_Rtf_RoundTripsTableColumnWidths` — a 2-cell row with
    `Width="100"`/`Width="200"` columns reloads with `100,200` intact.
- Model tests: 234/234 (`LeXtudio.Windows.Tests`).

Result:

- 226/226 RichTextBox integration tests pass; 234/234 model tests pass.
- Table column widths now round-trip through RTF save/load; the intermediate
  XAML serializes them WPF-faithfully (`Width="100"`, not `"100px"`).
