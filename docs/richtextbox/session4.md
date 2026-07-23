### Session 4 - TextEditor Typing Path

Status: complete.

Scope:

- Add DevFlow coverage for text insertion through the WPF `TextEditorTyping`
  editing path rather than direct `AppendText`.
- Fix the first runtime editing-spine gap found by that probe.

DevFlow additions:

- `richtextbox.probe.text-input`

Implementation notes:

- Constructing/platform-synthesizing Uno `CharacterReceivedRoutedEventArgs` is
  not used in this session.
- The probe reflects into upstream `TextEditorTyping.DoTextInput(...)`, which is
  the actual insertion worker called after `OnTextInput` schedules input.
- A first attempt to call `TextEditorTyping.OnTextInput(...)` directly exposed a
  dispatcher mismatch: `ScheduleInput` queued a background callback and the Uno
  dispatcher path cleared `PendingInputItems` before the current call appended
  its item. The session bypasses that scheduling layer and tests the insertion
  worker directly.

Product fix:

- `TextPointer.ITextPointer.GetValue(...)` now supplies WPF-compatible defaults
  for `FrameworkElement.LanguageProperty` (`en-US`) and
  `FrameworkElement.FlowDirectionProperty` (`LeftToRight`) when the Uno
  DP owner/default bridge returns `null` / `UnsetValue`.
- `FrameworkElement.LanguageProperty` also now has a non-null default
  `XmlLanguage` metadata value.

Verified behavior:

- `richtextbox.probe.text-input` can insert `abc` into a focused RichTextBox.
- Readback through `TextRange(document.ContentStart, document.ContentEnd).Text`
  contains the inserted text.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj
```

Result:

```text
Passed: 6/6
```

Next session:

- Decide whether to fix `TextEditorTyping.ScheduleInput` under Uno so
  `OnTextInput(...)` itself can be tested without bypassing scheduling.
- Add the first deletion/editing command probe: insert text, invoke Backspace or
  Delete through the RichTextBox key path, and verify `TextRange` readback.
