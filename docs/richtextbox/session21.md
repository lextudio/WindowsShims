### Session 21 - Undo and Redo Key Coverage

Status: complete.

Scope:

- Add DevFlow coverage for the public WPF `TextBoxBase.Undo()` and `Redo()`
  APIs on RichTextBox.
- Add real `RichTextBox.OnKeyDown(...)` coverage for Ctrl+Z and Ctrl+Y.

DevFlow additions:

- `richtextbox.probe.undo`
- `richtextbox.probe.redo`
- `richtextbox.probe.state` now reports `canUndo` and `canRedo`.

Product fix:

- `RichTextBox.uno.cs` now maps Ctrl+Z to `ApplicationCommands.Undo`,
  Ctrl+Shift+Z to `ApplicationCommands.Redo`, and Ctrl+Y to
  `ApplicationCommands.Redo` before falling through to character formatting
  shortcuts.

Verified behavior:

- Text inserted through `TextEditorTyping.OnTextInput(...)` is removed by
  public `Undo()` and restored by public `Redo()`.
- The same mutation is removed/restored through the Uno `OnKeyDown` path for
  Ctrl+Z/Ctrl+Y.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --filter "FullyQualifiedName~UndoRedo_RestoresTextInputMutation|FullyQualifiedName~KeyDown_ControlZAndControlY_InvokeUndoRedo"
```

Result:

```text
Passed: 2/2
```

Next session:

- Add partial-selection formatting tests to prove selected ranges split
  precisely and unselected runs keep original values.
- Then cover line spacing commands through `TextRangeEdit` paragraph handling.
