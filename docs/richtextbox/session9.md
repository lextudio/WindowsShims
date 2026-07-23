### Session 9 - Selection Replacement Input

Status: complete.

Scope:

- Add DevFlow coverage for replacing an existing selection through the
  `TextEditorTyping.OnTextInput(...)` path.

DevFlow additions:

- `richtextbox.probe.replace-selection-text-input-event`

Verified behavior:

- The probe focuses the RichTextBox, selects all existing document text, sends
  composed character input through `OnTextInput(...)`, and verifies the old text
  is removed while the replacement text is present.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj
```

Result:

```text
Passed: 13/13
```

Next session:

- Add observable selection metadata to the DevFlow snapshot, then cover basic
  formatting commands such as ToggleBold/ToggleItalic on selected text.
