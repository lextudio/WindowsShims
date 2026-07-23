### Session 28 - Caret and Shift Selection Navigation Coverage

Status: complete.

Scope:

- Add DevFlow state needed to assert logical selection/caret positions.
- Cover RichTextBox keyboard navigation through the real Uno `OnKeyDown(...)`
  forwarding path and migrated WPF `TextEditorSelection` commands.

DevFlow additions:

- `richtextbox.probe.set-caret-run-offset`
- `richtextbox.probe.key-down-modifiers`
- `richtextbox.probe.state` now reports:
  - `selectionStartRunOffset`
  - `selectionEndRunOffset`

Verified behavior:

- With the caret explicitly placed inside the first `Run`, Right moves the caret
  one character forward and Left moves it one character backward.
- Shift+Right extends the selection by one character via
  `EditingCommands.SelectRightByCharacter`.

Commands:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --filter "FullyQualifiedName~KeyDown_LeftRight|FullyQualifiedName~KeyDown_ShiftRight"
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj
```

Result:

```text
Passed: 2/2 targeted
Passed: 50/50 full RichTextBox integration suite
```

Next session:

- Expand navigation coverage to document/line boundary commands
  (`Home`, `End`, `Ctrl+Home`, `Ctrl+End`, and Shift variants), keeping the
  assertions on explicit first-run or document-relative offsets.
