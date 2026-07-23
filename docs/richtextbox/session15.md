### Session 15 - ApplyFontSize Formatting

Status: complete.

Scope:

- Add DevFlow observability for selected-text font size.
- Cover the upstream `TextEditorCharacters.OnApplyFontSize(...)` command
  handler for RichTextBox selections.

Product fix:

- `TextEditorCharacters.OnApplyFontSize(...)` now uses the shared `HAS_UNO`
  inline-formatting fallback for non-empty RichTextBox selections.
- This makes the applied absolute font size visible on document inlines while
  `TextSelection.ApplyPropertyValue(...)` is still incomplete.

DevFlow additions:

- Snapshot now reports `firstInlineFontSize`.
- `richtextbox.probe.apply-font-size-selection-command`
- `ApplyFontSizeCommand_AppliesFontSizeToSelectedText`

Verified behavior:

- Selecting all text and invoking ApplyFontSize with `24` sets the first
  inline font size to `24`.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --no-build
```

Result:

```text
Passed: 23/23
```

Next session:

- Cover IncreaseFontSize and DecreaseFontSize by adding relative font-size
  fallback behavior for selected RichTextBox inlines.
- Add foreground/background formatting observability once brush serialization
  is stable enough for DevFlow assertions.
