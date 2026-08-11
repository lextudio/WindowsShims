### Session 100 - Visual Rendering for Inline and Paragraph Backgrounds

Status: completed.

Scope:

- Session 84 made inline/paragraph `Background` round-trip through RTF
  (`\highlightN` → `<Span Background>`/`<Paragraph Background>`), but the
  Florence renderer painted no background. This session renders both.

Findings:

- `FlorenceRun` carried no background, `CollectSpans` dropped the value, and
  the view painted only text blocks.
- WPF semantics: an inline's background flows to its content (a
  `<Span Background>` paints its child runs), and `Paragraph.Background`
  paints the paragraph's content area.

Changes:

- `FlorenceEngine.cs`:
  - `CollectSpans` now threads a `Background` value down the inline tree
    (`inline.Background ?? inherited`), and `SpanInfo`/`FlorenceRun` carry it
    into the layout.
  - `FormatParagraph` emits a `FlorenceFillBox` (bounds + brush) when
    `Paragraph.Background` is set, mirroring the border box.
  - New `FlorenceFillBox` + `FlorencePage.FillBoxes`.
- `FlowDocumentView.uno.cs`:
  - `BuildLineVisual` paints a filled rectangle for each run with a
    background (before the run's text blocks, sized to the line height).
  - `RebuildLineBlocks` adds paragraph background fill rectangles before the
    line blocks and arranges them. New `FillBoxRectCount` and
    `InlineBackgroundRectCount` for tests.
- `MainPage.cs`: snapshot gained `fillBoxRectCount` and
  `inlineBackgroundRectCount`.

Tests:

- Integration (extended): `SaveLoad_Rtf_RoundTripsBackground` now uses an
  inline-yellow paragraph plus a paragraph-green paragraph and asserts the
  inline background paints inside the line canvas and the paragraph
  background paints one fill box. 232/232 total.
- Model tests: 234/234 (`LeXtudio.Windows.Tests`).

Result:

- 232/232 RichTextBox integration tests pass; 234/234 model tests pass.
- Inline and paragraph backgrounds now render in `FlowDocumentView` after RTF
  save/load.
