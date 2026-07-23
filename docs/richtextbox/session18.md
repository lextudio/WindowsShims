### Session 18 - Font Family Formatting

Status: complete.

Scope:

- Add stable FontFamily observability to the DevFlow RichTextBox snapshot.
- Cover the ApplyFontFamily command handler for selected RichTextBox text.

Product fix:

- `TextEditorCharacters.OnApplyFontFamily(...)` now applies the selected
  `FontFamily` to RichTextBox document inlines under `HAS_UNO`.
- This mirrors the existing selected-text fallback used for font size,
  foreground, and background formatting while selected
  `TextSelection.ApplyPropertyValue(...)` behavior remains incomplete.

DevFlow additions:

- Snapshot now reports `firstInlineFontFamily`.
- `richtextbox.probe.apply-font-family-selection-command`
- `ApplyFontFamilyCommand_AppliesFontFamilyToSelectedText`

Verified behavior:

- Applying `Courier New` sets the first inline font family to `Courier New`.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --no-build
```

Result:

```text
Passed: 28/28
```

Next session:

- Add paragraph-level snapshot fields needed for alignment assertions.
- Cover paragraph alignment commands once paragraph formatting can be observed
  through DevFlow.
