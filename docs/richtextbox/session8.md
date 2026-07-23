### Session 8 - Delete and Enter Key Coverage

Status: complete.

Scope:

- Add more real `RichTextBox.OnKeyDown(...)` DevFlow coverage.
- Cover Delete through the Uno key path with a deterministic selection.
- Cover Enter/paragraph insertion through the Uno key path.

Product fixes:

- The RichTextBox test scenario now explicitly sets `AcceptsReturn = true` so
  Enter behavior is independent from metadata-bridge defaults.
- `RichTextBox.uno.cs` now handles unmodified `VirtualKey.Enter` on the Uno key
  path by inserting a new paragraph into `Document.Blocks`, moving the editor
  selection to the new paragraph, and refreshing the caret. This avoids the
  current upstream paragraph-split path, which can corrupt the Uno text tree by
  treating a `Paragraph` as an `Inline`.
- `TextEditorTyping.OnEnterBreak(...)` no longer requires WPF
  `UiScope.IsKeyboardFocused` under `HAS_UNO`; Uno focus is already represented
  by the key event entering the control.

DevFlow additions:

- `richtextbox.probe.key-down-select-all`

Verified behavior:

- `richtextbox.probe.key-down` with `Back` removes the previous character.
- `richtextbox.probe.key-down-select-all` with `Delete` removes selected text.
- `richtextbox.probe.key-down` with `Enter`, followed by text input, produces
  at least two document blocks and preserves text before and after the break.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj
```

Result:

```text
Passed: 12/12
```

Next session:

- Add selection replacement coverage through `OnTextInput`: select existing
  text, type replacement text, and verify old content is removed.
- Start covering basic formatting commands such as ToggleBold once selection
  state is observable in the DevFlow snapshot.
