### Session 98 - Visual Rendering for Paragraph Borders

Status: completed.

Scope:

- Session 93 made `Paragraph.BorderThickness`/`BorderBrush` round-trip through
  RTF (`\brdr*`), but `FlowDocumentView`'s Florence renderer drew no border.
  This session makes paragraph borders render in the editor.

Findings:

- The Florence layout engine emits lines per paragraph but had no notion of a
  paragraph's enclosing box, and the view built one canvas per line with no
  border layer.
- WPF draws paragraph borders as a box around the paragraph's content lines,
  honoring each side's thickness.

Changes:

- `FlorenceEngine.cs`:
  - New `FlorenceParagraphBorder` (x, y, width, height, brush, thickness)
    and `FlorencePage.ParagraphBorders`.
  - `FormatParagraph` reads `BorderThickness`/`BorderBrush` and, when set,
    records a border box spanning the paragraph's laid-out lines (width =
    widest run, height = sum of line heights).
- `FlowDocumentView.uno.cs`:
  - `BuildParagraphBorderSides` turns a border box into one `Rectangle` per
    non-zero side (top/bottom full width, left/right full height), honoring
    asymmetric thicknesses.
  - `RebuildLineBlocks` adds the border rectangles to the canvas before the
    line blocks (behind the text); `ArrangeOverride` positions them at the
    recorded bounds. New `ParagraphBorderRectCount` for tests.
- `MainPage.cs`: snapshot gained `paragraphBorderRectCount` (number of
  paragraph border side rectangles in `FlowDocumentView`).

Tests:

- Integration (extended): `SaveLoad_Rtf_RoundTripsParagraphBorder` now also
  asserts `paragraphBorderRectCount == "4"` — after the RTF round-trip the
  border renders as four side rectangles. 232/232 total.
- Model tests: 234/234 (`LeXtudio.Windows.Tests`).

Result:

- 232/232 RichTextBox integration tests pass; 234/234 model tests pass.
- Paragraph borders now render in `FlowDocumentView` after RTF save/load.
