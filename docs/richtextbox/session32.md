### Session 32 - Non-Empty Selection Word Deletion Coverage

Status: complete.

Scope:

- Add DevFlow coverage proving `Ctrl+Delete`/`Ctrl+Backspace` delete a
  non-empty selection directly, without expanding it to the nearest word
  boundary.

Product notes:

- No product fix was needed. Upstream `TextEditorTyping.OnDeleteNextWord(...)`
  and `OnDeletePreviousWord(...)` already special-case
  `This.Selection.IsEmpty`: when the selection is non-empty they delete exactly
  the selected range and only call `MoveToNextWordBoundary(...)` for the empty
  (caret-only) case. This behavior was already migrated and required only test
  coverage.

DevFlow additions:

- `richtextbox.probe.select-run-range`, a general non-zero-length selection
  probe inside the first `Run`, reusing the existing `SelectFirstRunTextRange`
  helper (previously only exposed with implicit zero-length callers).

Verified behavior:

- With `one two three` and the selection set to `"two "` (offsets 4-8),
  `Ctrl+Delete` and `Ctrl+Backspace` both remove exactly the selected text,
  leaving `one three` with the caret at offset 4, instead of the word-boundary
  expansion seen for empty-selection deletion in Session 31.

Commands:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --filter "FullyQualifiedName~KeyDown_ControlBackspaceDelete_WithNonEmptySelection"
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj
```

Result:

```text
Passed: 2/2 targeted
Passed: 66/66 full RichTextBox integration suite
```

Next session:

- Continue selection/editing coverage: consider selection replacement via the
  real key path (typing over a selection through `OnKeyDown`/character input
  rather than `OnTextInput` directly), or begin auditing
  clipboard/serialization format coverage (M4) now that the editing spine has
  broad key-path coverage.
