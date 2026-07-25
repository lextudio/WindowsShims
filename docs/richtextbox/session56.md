### Session 56 - Multi-Paragraph Clipboard Paste

Status: completed.

Scope:

- `TextRangeSerialization` for `DataFormats.Text` works and is tested for
  single-line content (session 27). Multi-paragraph paste through the
  clipboard command path (`Ctrl+V`, `EditingCommands.Paste`) has not been
  verified end-to-end.
- Key question: when pasting text containing `\n` paragraph breaks, does the
  WPF editing pipeline correctly split the content into multiple `Paragraph`
  blocks, or does it insert literal newline characters inside a single
  paragraph?
- Undo fidelity: does `Ctrl+Z` after a multi-paragraph paste restore the
  exact pre-paste document state (block count, caret position, text content)?

Implementation:

- `Paste` in `TextEditorCopyPaste` uses `TextRangeSerialization` to convert
  clipboard text into document content. For `DataFormats.Text`, the serializer
  produces a `TextRange` with embedded `\n` characters. The `TextEditor`
  handles `\n` during text insertion by splitting paragraphs via
  `TextEditorParagraphs.InsertParagraphBreak`. Verify this path works
  correctly on the shim.
- No production-code changes expected unless a bug is found — this session is
  primarily test coverage.

Tests:

- `PasteCommand_MultiParagraphText_CreatesCorrectParagraphs`: copy
  multi-paragraph text to clipboard, paste into RichTextBox, verify
  `blockCount` and text content.
- `PasteCommand_MultiParagraphText_UndoRestoresOriginalDocument`: paste then
  Ctrl+Z, verify document matches pre-paste state.
- `PasteCommand_IntoNonEmptySelection_ReplacesSelection`: select existing
  text, paste multi-paragraph content, verify replacement and paragraph count.

Files modified:

- `tests/RichTextBox.IntegrationTests/RichTextBoxIntegrationTests.cs` — new
  tests.
- Possibly `clipboard-set-text` probe in `MainPage.cs` if multi-paragraph
  clipboard content isn't already settable.

Regression sweep:

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
dotnet test tests/RichTextBox.IntegrationTests/ --filter-method "*PasteCommand*"
```

Next session:

- Nested inline formatting edge cases.
