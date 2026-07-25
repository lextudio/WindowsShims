### Session 74 - TextChanged Event Verification

Status: completed.

Scope:

- `RichTextBox.TextChanged` is the primary event consumers use to react to
  document modifications. The event should fire on typing, paste, undo/redo,
  and any programmatic document change.
- The upstream WPF `TextContainer.Changed` event fires on text changes.
  `RichTextBox` hooks it via `TextContainer.Changed += OnTextContainerChanged`.
  Verify the event fires correctly and test it via a probe.

Implementation:

- Add a `text-changed-count` probe that attaches a handler to
  `RichTextBox.TextChanged`, performs an action, and reports how many
  times the event fired.
- Test various operations: typing, Enter, paste, undo, format toggle.

Tests:

- `TextChanged_FiresOnTextInput`: type a character, verify event fires.
- `TextChanged_FiresOnPaste`: paste text, verify event fires.
- `TextChanged_FiresOnUndo`: type, undo, verify event fires both times.
- `TextChanged_FiresOnFormatToggle`: toggle bold, verify event fires.

Files modified:

- `MainPage.cs` — `text-changed-count` probe.
- `RichTextBoxIntegrationTests.cs` — new tests.
