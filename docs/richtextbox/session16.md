### Session 16 - Relative Font Size Commands

Status: complete.

Scope:

- Cover selected-text IncreaseFontSize and DecreaseFontSize command handlers.
- Add Uno fallback behavior for relative font-size changes.

Product fix:

- `TextEditorCharacters.OnIncreaseFontSize(...)` now uses a `HAS_UNO`
  RichTextBox fallback for non-empty selections that increments inline font
  size by `OneFontPoint`.
- `TextEditorCharacters.OnDecreaseFontSize(...)` uses the same fallback with a
  negative delta.
- The fallback clamps to the upstream min/max font-size bounds and avoids the
  current `TextRange.ApplyPropertyValue(... Increase/Decrease ...)` exception
  path for selected RichTextBox text.

DevFlow additions:

- `richtextbox.probe.increase-font-size-selection-command`
- `richtextbox.probe.decrease-font-size-selection-command`
- `IncreaseFontSizeCommand_IncreasesSelectedTextFontSize`
- `DecreaseFontSizeCommand_DecreasesSelectedTextFontSize`

Verified behavior:

- Applying font size `24` and then IncreaseFontSize sets the first inline font
  size to `24.75`.
- Applying font size `24` and then DecreaseFontSize sets the first inline font
  size to `23.25`.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --no-build
```

Result:

```text
Passed: 25/25
```

Next session:

- Add foreground/background formatting observability using stable brush
  serialization.
- Add command-level coverage for ApplyForeground and ApplyBackground.
