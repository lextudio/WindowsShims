### Session 13 - Formatting Toggle-Off Coverage

Status: complete.

Scope:

- Cover repeated selected-text formatting commands so Bold, Italic, and
  Underline are verified in both directions.
- Remove order-dependent behavior from underline text decoration toggling.

Product fix:

- `TextDecorationCollection.TryRemove(...)` now builds and returns a new
  collection instead of mutating the source collection in place.
- This prevents repeated ToggleUnderline from modifying shared static
  collections such as `TextDecorations.Underline`, which previously made later
  underline operations order-dependent.

DevFlow additions:

- `ToggleBoldCommand_WhenInvokedTwice_RestoresNormalWeight`
- `ToggleItalicCommand_WhenInvokedTwice_RestoresNormalStyle`
- `ToggleUnderlineCommand_WhenInvokedTwice_RemovesUnderline`

Verified behavior:

- ToggleBold applies `700` on the first invocation and a second invocation
  restores a non-bold inline weight.
- ToggleItalic applies `Italic` on the first invocation and a second invocation
  restores a non-italic inline style.
- ToggleUnderline applies underline on the first invocation and a second
  invocation removes underline without corrupting shared underline state.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --no-build
```

Result:

```text
Passed: 19/19
```

Next session:

- Validate whether `KeyRoutedEventArgs` modifier injection can drive
  Ctrl+B/Ctrl+I/Ctrl+U through the RichTextBox key path.
- If modifier routing is not available yet, continue with command-level
  coverage for font size and foreground/background formatting.
