### Session 31 - Word Deletion Coverage

Status: complete.

Scope:

- Add DevFlow-driven integration coverage for `Ctrl+Delete` and
  `Ctrl+Backspace` through the real Uno `RichTextBox.OnKeyDown(...)` path.
- Exercise migrated WPF `TextEditorTyping.OnDeleteNextWord` and
  `OnDeletePreviousWord` via `EditingCommands.DeleteNextWord` and
  `DeletePreviousWord`.

Verified behavior:

- From offset 5 in `one two three`, `Ctrl+Delete` deletes the WPF forward
  word-boundary range `wo ` and leaves `one tthree`.
- From the same offset, `Ctrl+Backspace` deletes the WPF backward
  word-boundary range `t` and leaves `one wo three`.
- The resulting selection is empty and the caret remains at the WPF deletion
  boundary.

Commands:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --filter "FullyQualifiedName~KeyDown_ControlBackspaceDelete"
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj
```

Result:

```text
Passed: 2/2 targeted
Passed: 64/64 full RichTextBox integration suite
```

Next session:

- Cover non-empty selection behavior for word deletion commands: WPF should
  delete the selected content directly instead of expanding to the nearest word
  boundary.
