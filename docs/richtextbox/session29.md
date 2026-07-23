### Session 29 - Line and Document Boundary Navigation Coverage

Status: complete.

Scope:

- Extend RichTextBox keyboard navigation coverage from character movement to
  line/document boundary commands.
- Keep the tests on the real Uno `RichTextBox.OnKeyDown(...)` path, which maps
  to migrated WPF `TextEditorSelection` commands.

Verified behavior:

- `Home` and `End` move the caret to the current line start/end for a single
  first-run paragraph.
- `Ctrl+Home` and `Ctrl+End` move the caret to document start/end.
- `Shift+Home` and `Shift+End` extend selection to line boundaries.
- `Ctrl+Shift+Home` and `Ctrl+Shift+End` extend selection to document
  boundaries. The `Ctrl+Shift+End` assertion preserves WPF symbol-offset
  behavior: selected text includes the paragraph break (`bc\n`) and the first
  run relative end offset reaches the document-end symbol position.

Commands:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --filter "FullyQualifiedName~KeyDown_BoundaryKeys|FullyQualifiedName~KeyDown_ShiftBoundaryKeys"
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj
```

Result:

```text
Passed: 8/8 targeted
Passed: 58/58 full RichTextBox integration suite
```

Next session:

- Cover Ctrl+Left/Ctrl+Right word navigation and Ctrl+Shift word-selection
  expansion. Prefer multiple-word first-run text so word-boundary behavior is
  asserted independently from paragraph boundary handling.
