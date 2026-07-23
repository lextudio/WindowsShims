### Session 6 - Delete Command Coverage

Status: complete.

Scope:

- Add DevFlow coverage for the upstream Delete command handler.
- Keep the test on the command path for now, matching the Backspace coverage
  from Session 5, while leaving full Uno key-event synthesis as a separate
  follow-up.

DevFlow additions:

- `richtextbox.probe.delete-selection-command`

Verified behavior:

- The probe focuses the current RichTextBox, calls the public `SelectAll()`
  API, invokes `TextEditorTyping.OnDelete(...)`, and verifies the selected
  document text is removed through `TextRange` readback.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj
```

Result:

```text
Passed: 9/9
```

Next session:

- Investigate whether `RichTextBox.OnKeyDown` can be reached through a stable
  Uno `KeyRoutedEventArgs` construction path in the desktop integration host.
- If key-event construction remains unsuitable, add more command-handler probes
  for selection replacement, Return/paragraph insertion, and clipboard-free
  editing cases, and document the event-synthesis limitation explicitly.
