### Session 5 - Scheduled Text Input and Backspace Command

Status: complete.

Scope:

- Fix the Uno-specific `TextEditorTyping.ScheduleInput` dispatcher mismatch
  found in Session 4.
- Add DevFlow coverage for `TextEditorTyping.OnTextInput(...)` itself.
- Add the first deletion command probe through the upstream Backspace command
  handler.

Product fix:

- `TextEditorTyping.ScheduleInput(...)` now processes input synchronously on
  `HAS_UNO`. Uno's dispatcher can run the background callback before the current
  item is appended to `PendingInputItems`; synchronous processing preserves the
  editor ordering contract until WPF-style dispatcher batching is modeled.

DevFlow additions:

- `richtextbox.probe.text-input-event`
- `richtextbox.probe.backspace-command`

Verified behavior:

- `richtextbox.probe.text-input-event` drives
  `TextEditorTyping.OnTextInput(...)` and inserts text into the document.
- `richtextbox.probe.backspace-command` invokes the upstream Backspace command
  handler and removes the previous character after text insertion.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj
```

Result:

```text
Passed: 8/8
```

Next session:

- Add Delete command coverage.
- Add a probe that goes through `RichTextBox.OnKeyDown` / Uno key mapping if a
  constructible `KeyRoutedEventArgs` path is available; otherwise keep command
  handler probes explicit and document the platform-event limitation.
