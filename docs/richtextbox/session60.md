### Session 60 - TextPointer Offsets Across Mixed Document Structures

Status: completed.

Scope:

- Session 42 verified caret hit-test round-trips for single-paragraph
  documents. `TextPointer` offsets (`CharOffset`, `GetOffsetToPosition`,
  `GetPositionAtOffset`) across multi-paragraph, list, and table documents
  have not been systematically validated.
- The `TextContainer`'s splay tree tracks symbol-level positions. The
  `TextPointer` implementation (upstream WPF source) computes offsets from
  tree walks. Offsets must be consistent:
  - `GetOffsetToPosition(start, end) == end.CharOffset - start.CharOffset`
  - `start.GetPositionAtOffset(n).GetOffsetToPosition(start) == n`
  - Across paragraph boundaries (the invisible paragraph-marker slot).
  - Inside `List`/`ListItem`/`TableCell` nested structures.
- Inconsistencies here would break caret positioning, selection, and
  clipboard operations in mixed-content documents.

Implementation:

- Use the existing `caret-hit-test-round-trip` probe pattern but extended
  to test documents with multiple paragraphs, lists, and tables.
- If offset inconsistencies are found, fix them in the `TextPointer`
  implementation or the `TextContainer` splay tree (`TextContainer.uno.cs`
  or `TextPointer.cs` in the shim).

Tests:

- `TextPointerOffset_RoundTripsAcrossParagraphBoundary`: document with two
  paragraphs, verify `GetOffsetToPosition` and `GetPositionAtOffset` are
  consistent at the boundary.
- `TextPointerOffset_RoundTripsInsideList`: list with two items, verify
  offsets inside each ListItem are consistent.
- `TextPointerOffset_RoundTripsInsideTable`: table with cells, verify
  offsets inside each TableCell are consistent.
- `TextPointerOffset_CharOffsetMatchesTextLength`: for a full document,
  verify `ContentEnd.CharOffset - ContentStart.CharOffset` equals the
  `TextRange` text length.

Files modified:

- `src/LeXtudio.Windows/.../Documents/TextPointer.cs` or
  `TextContainer.uno.cs` — fix offset inconsistencies if found.
- `tests/RichTextBox.IntegrationTests/RichTextBoxIntegrationTests.cs` — new
  tests.
- `tests/RichTextBox.IntegrationTestHost/MainPage.cs` — add offset
  validation probes if needed.

Regression sweep:

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
dotnet test tests/RichTextBox.IntegrationTests/
```

Next session:

- TBD — re-evaluate priorities against consumer feedback.
