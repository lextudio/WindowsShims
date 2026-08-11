### Session 104 - IME Composition Mapping Through Tables

Status: completed.

Scope:

- Session 43 verified the IME plain-text offset mapping for multi-paragraph
  documents but tables were untested. This session verifies and fixes IME
  composition inside table documents.

Findings:

- `GetPlainTextOffset` used `TextRange(ContentStart, position).Text`, which
  runs `NormalizeRange` — for a range ending inside a table, the end is
  expanded to the containing cell's boundary, so every offset inside a cell
  collapsed to the cell end and `GetPositionAtPlainTextOffset` always
  resolved to the document start/end.
- `TextRange` construction also clamps positions inside a table cell to the
  cell's boundaries (WPF's table-aligned range semantics), so
  `range.Text = value` in `OnImeTextUpdating` inserted at the wrong place
  when composing inside a cell.
- Boundary positions at table cell edges are not insertion positions, and the
  shim's insertion machinery moves them backward (into the previous cell).

Changes:

- `RichTextBox.Ime.uno.cs`:
  - `GetPlainTextOffset` now uses `TextRangeBase.GetTextInternal` directly
    (bypasses `NormalizeRange`'s table expansion).
  - `GetPositionAtPlainTextOffset` steps forward into the first text content
    at/after a resolved boundary position so composition lands inside the
    intended cell.
  - `OnImeTextUpdating` inserts via `DeleteContentInternal` +
    `InsertTextInRun` instead of `TextRange.Text` (which clamps in-cell
    ranges), and places the caret at the plain-text offset
    `startOffset + newText.Length`.

Tests:

- Integration (1 new, 236/236 total):
  - `SimulateImeTextUpdating_OnTableDocument_InsertsIntoCorrectCell` —
    composing at the plain-text offset of the second cell's first character
    lands inside that cell (the XAML contains the composed text followed by
    the cell's text).
- Model tests: 234/234 (`LeXtudio.Windows.Tests`).

Result:

- 236/236 RichTextBox integration tests pass; 234/234 model tests pass.
- IME composition works inside table documents, including at cell boundaries.
