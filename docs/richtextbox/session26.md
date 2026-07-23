### Session 26 - Keyboard FlowDirection KeyUp Path

Status: complete.

Scope:

- Cover WPF's two-stage keyboard flow-direction command path for RichTextBox.
- Keep the implementation routed through upstream `TextEditorTyping.OnKeyDown`
  / `OnKeyUp` instead of adding a command fallback.

Product fixes:

- `RichTextBox.uno.cs` now forwards Uno `OnKeyUp(KeyRoutedEventArgs)` into
  WPF `TextEditorTyping.OnKeyUp(...)`.
- `MapVirtualKey(...)` now maps `Shift`, `LeftShift`, `RightShift`,
  `Control`, `LeftControl`, and `RightControl` to the WPF `Key` shim values.
- This lets WPF `TextEditorTyping.OnKeyDown(...)` set `PureControlShift`, then
  `TextEditorTyping.OnKeyUp(...)` schedule and execute
  `OnFlowDirectionCommand(...)`.

DevFlow additions:

- `richtextbox.probe.key-down-up-select-all-modifiers`
- `KeyDownUp_ControlLeftShift_AppliesParagraphFlowDirectionLeftToRight`

Verified behavior:

- The test first applies paragraph RTL through the existing WPF paragraph
  command path, then sends `Ctrl+LeftShift` key-down/key-up and verifies the
  selected paragraph returns to `LeftToRight`.
- `Ctrl+RightShift` is not asserted yet because upstream WPF only executes that
  path when a bidi input language is installed, which is environment-dependent.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --logger "console;verbosity=minimal"
```

Result:

```text
Passed: 45/45
```

Next session:

- Continue RichTextBox migration with paste/clipboard or selection/caret
  behavior, preferring DevFlow coverage that exercises WPF editor internals.
