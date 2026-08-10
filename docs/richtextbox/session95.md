### Session 95 - RTF Round-Trip Coverage for LineBreaks and Tab Characters

Status: completed.

Scope:

- Sessions 81-94 added RTF round-trips for text, inline/paragraph formatting,
  lists, hyperlinks, tables (cells, columns, nesting), super/subscript,
  inline language, paragraph borders, and nested tables. This session adds
  regression coverage confirming `LineBreak` and tab characters survive RTF
  save/load.

Findings:

- **Both already round-trip with no shim changes.** `XamlToRtfWriter` emits
  `\line ` for a `LineBreak` inline and `\tab` for tab characters; the
  `RtfToXamlReader` reconstructs `<LineBreak />` (the shim's `ParseInline`
  already handles it) and restores tabs in run text.
- This leaves **embedded images (`\pict`)** as the only unround-tripped RTF
  feature — a large one: `TextRangeSerialization.WriteEmbeddedObject` is
  `#if !HAS_UNO` (under Uno, embedded objects are replaced by a space), and a
  full round-trip would additionally need image serialization, `\pict`
  binary output/parsing, and `XamlReader` `<Image>` support.

Changes:

- No shim or submodule changes; only tests.

Tests:

- Integration (2 new, 230/230 total):
  - `SaveLoad_Rtf_RoundTripsLineBreak` — `a<LineBreak/>b` reloads with the
    `LineBreak` inline intact and text `a\nb`.
  - `SaveLoad_Rtf_RoundTripsTabCharacters` — `a\tb\tc` round-trips unchanged.
- Model tests: 234/234 (`LeXtudio.Windows.Tests`).

Result:

- 230/230 RichTextBox integration tests pass; 234/234 model tests pass.
- Line breaks and tab characters are verified to round-trip through RTF
  save/load; embedded images remain the one known unround-tripped RTF
  feature (deferred, `WriteEmbeddedObject` excluded under `HAS_UNO`).
