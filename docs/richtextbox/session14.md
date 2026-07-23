### Session 14 - Keyboard Formatting Shortcuts

Status: complete.

Scope:

- Cover the RichTextBox `OnKeyDown(...)` entry point for selected-text
  formatting shortcuts.
- Validate Ctrl+B, Ctrl+I, and Ctrl+U against the same inline formatting
  observability used by command-level tests.

Product fix:

- `RichTextBox.OnKeyDown(...)` now recognizes Ctrl+B, Ctrl+I, and Ctrl+U when
  Alt is not held and dispatches `EditingCommands.ToggleBold`,
  `EditingCommands.ToggleItalic`, and `EditingCommands.ToggleUnderline`.
- The command dispatch reuses the migrated upstream command handlers and the
  existing Uno inline-formatting fallback.

DevFlow additions:

- `richtextbox.probe.key-down-select-all-modifiers`
- `KeyDown_ControlB_AppliesBoldToSelectedText`
- `KeyDown_ControlI_AppliesItalicToSelectedText`
- `KeyDown_ControlU_AppliesUnderlineToSelectedText`

Notes:

- The current Uno `KeyRoutedEventArgs` target does not expose a public
  `KeyModifiers` property. The DevFlow probe injects modifiers by temporarily
  setting the internal `Keyboard.ModifiersOverride`, matching the existing
  DataGrid test pattern for programmatic modifier state.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --no-build
```

Result:

```text
Passed: 22/22
```

Next session:

- Add command-level coverage for ApplyFontSize and font size increase/decrease.
- Add foreground/background formatting observability once brush values can be
  serialized consistently through the DevFlow snapshot.
