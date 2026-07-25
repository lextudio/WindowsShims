### Session 72 - IsReadOnly / IsEnabled Visual States

Status: completed.

Scope:

- WPF's `RichTextBox.IsReadOnly` and `IsEnabled` change the control's visual
  appearance: greyed background, disabled caret, no selection highlights.
  The current shim ignores these states — the caret still blinks and text
  can appear editable even when `IsReadOnly=true`.
- `TextBoxBase.IsReadOnly` gates editing commands in upstream WPF (the
  `CanExecute` handlers check it), so editing is already blocked. But the
  **visual feedback** is missing.

Implementation:

- In `FlowDocumentView`, check the `RichTextBox.IsReadOnly` and `IsEnabled`
  properties via reflection (or by passing them down when RenderScope is set).
- When `IsReadOnly` or `!IsEnabled`: hide the caret, desaturate/grey the
  text background, and don't draw selection highlights.
- Add a `read-only-state` probe to test visual properties.

Tests:

- `ReadOnly_HidesCaret`: set `IsReadOnly=true`, verify caret invisible.
- `ReadOnly_DoesNotAffectTextContent`: set `IsReadOnly=true`, verify text
  unchanged and typing does not modify document.
- `IsEnabledFalse_GreysOut`: set `IsEnabled=false`, verify caret hidden.

Files modified:

- `FlowDocumentView.uno.cs` — check read-only/enabled state.
- `RichTextBox.uno.cs` — wire state changes to FlowDocumentView.
- `MainPage.cs` — read-only probes.
- `RichTextBoxIntegrationTests.cs` — new tests.
