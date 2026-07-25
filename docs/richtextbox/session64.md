### Session 64 - Text Search (Find/FindNext)

Status: completed.

Scope:

- WPF's `TextRange.Find` searches for text strings within a `TextRange`.
  The implementation uses `TextContainer` navigation and `TextPointer`
  comparison — both of which are linked upstream and should work on the
  shim. Verify that basic text search works for:
  - Single-paragraph documents.
  - Multi-paragraph documents (search across paragraph boundaries).
  - Case-sensitive and case-insensitive modes.
  - Wrapped around the document end (`Find` with `FindFlags.Wrap`).
- The `TextRange.Find` method is in the linked upstream source and compiled.
  No production-code changes expected unless a bug is found.

Implementation:

- Use the existing `TextRange.Find(string, TextPointer, FindFlags)` API
  via reflection from a probe (since it's a static method or instance
  method on `TextRange`).
- Add a `text-range-find` probe that searches for text and returns the
  result positions.
- If `Find` doesn't work (throws or returns no results), investigate the
  `TextPointer` comparison path in `TextPointerBase` or the `TextRange`
  constructor.

Tests:

- `Find_PlainText_LocatesMatch`: search for existing word, verify found.
- `Find_PlainText_NotFound_ReturnsNull`: search for non-existent word.
- `Find_AcrossParagraphBoundary_FindsMatch`: search spanning two paragraphs.
- `Find_CaseInsensitive_FindsDifferentCase`: search with wrong case, verify
  found with `None` flag and not found with `MatchCase`.

Files modified:

- `tests/RichTextBox.IntegrationTests/RichTextBoxIntegrationTests.cs` — new
  tests.
- `tests/RichTextBox.IntegrationTestHost/MainPage.cs` — add
  `text-range-find` probe if `TextRange.Find` is accessible.

Regression sweep:

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
dotnet test tests/RichTextBox.IntegrationTests/
```

Next session:

- Catalog refresh and consumer gap prioritization.
