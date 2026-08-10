### Session 93 - RTF Round-Trip for Paragraph Borders

Status: completed.

Scope:

- Sessions 81-92 covered RTF round-trips for text, inline/paragraph formatting,
  lists, hyperlinks, table cells/columns, super/subscript, and inline language.
  This session makes `Paragraph.BorderThickness`/`BorderBrush` survive RTF
  save/load (`\brdr*` controls ↔ `BorderThickness="l,t,r,b"` + `BorderBrush`).

Findings:

- **The upstream RTF stack already handled paragraph borders on both sides.**
  `XamlToRtfWriter`'s `XABorderThickness`/`XABorderBrush` cases fill
  `FormatState.ParaBorder`, `WriteParagraph` appends
  `ParaBorder.RTFEncoding` (`\brdrt\brdrw...` etc.), and `RtfToXamlReader`
  parses the controls back and re-emits `BorderThickness` (uniform single
  value or `l,t,r,b`) plus `BorderBrush` on `<Paragraph>`. Two shim-side gaps
  blocked the round-trip:
  1. The shim `XamlReader.ParseParagraph` had no `BorderThickness`/
     `BorderBrush` cases, and `TryParseThickness` rejected the uniform
     single-value form the RTF reader emits for even borders.
  2. `DPTypeDescriptorContext.GetStringValue` had no case for
     `Block.BorderBrushProperty`/`ListItem.BorderBrushProperty`, so the brush
     went to complex properties and was never serialized as an attribute —
     the writer then defaulted the border color to black.

Changes:

- `ext/wpf` submodule: `DPTypeDescriptorContext.cs` (`GetStringValue`) — new
  `Block.BorderBrushProperty`/`ListItem.BorderBrushProperty` case formatting
  `SolidColorBrush` values as `#AARRGGBB`.
- `XamlReader.cs`:
  - `ParseParagraph`: new `BorderThickness` (via `TryParseThickness`) and
    `BorderBrush` (via `TryParseColor`) cases.
  - `TryParseThickness`: now accepts the uniform single-value form (`"1"`)
    like WPF's `ThicknessConverter`, not only `l,t,r,b`.
- `MainPage.cs`: snapshot gained `firstParagraphBorderThickness` and
  `firstParagraphBorderBrush`.

Tests:

- Integration (1 new, 227/227 total):
  - `SaveLoad_Rtf_RoundTripsParagraphBorder` — `BorderThickness="1,2,3,4"`
    reloads as `[Thickness: 1-2-3-4]` and `BorderBrush="#FFFF0000"` survives.
- Model tests: 234/234 (`LeXtudio.Windows.Tests`).

Result:

- 227/227 RichTextBox integration tests pass; 234/234 model tests pass.
- Paragraph borders now round-trip through RTF save/load.
