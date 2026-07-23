### Session 17 - Foreground and Background Formatting

Status: complete.

Scope:

- Add stable brush observability to the DevFlow RichTextBox snapshot.
- Cover ApplyForeground and ApplyBackground command handlers for selected
  RichTextBox text.

Product fix:

- `TextEditorCharacters.OnApplyForeground(...)` now applies the selected brush
  to RichTextBox document inlines under `HAS_UNO`.
- `TextEditorCharacters.OnApplyBackground(...)` uses the same inline fallback
  for selected RichTextBox text.
- This keeps foreground/background formatting observable while selected
  `TextSelection.ApplyPropertyValue(...)` behavior remains incomplete.

DevFlow additions:

- Snapshot now reports `firstInlineForeground` and `firstInlineBackground` as
  `#AARRGGBB` for solid brushes.
- `richtextbox.probe.apply-foreground-selection-command`
- `richtextbox.probe.apply-background-selection-command`
- `ApplyForegroundCommand_AppliesForegroundToSelectedText`
- `ApplyBackgroundCommand_AppliesBackgroundToSelectedText`

Verified behavior:

- Applying foreground with `LightGreen` sets the first inline foreground to
  `#FF90EE90`.
- Applying background with `LightPink` sets the first inline background to
  `#FFFFB6C1`.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --no-build
```

Result:

```text
Passed: 27/27
```

Next session:

- Add ApplyFontFamily coverage and stable FontFamily snapshot serialization.
- Then evaluate paragraph formatting commands such as alignment once
  paragraph-level observability is in place.
