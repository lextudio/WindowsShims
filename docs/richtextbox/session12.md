### Session 12 - ToggleUnderline Formatting

Status: complete.

Scope:

- Extend selected-text formatting coverage from ToggleBold/ToggleItalic to
  ToggleUnderline.
- Add observable text-decoration state to the DevFlow RichTextBox snapshot.

Product fix:

- `TextEditorCharacters.OnToggleUnderline(...)` now normalizes non-collection
  Uno default values for `Inline.TextDecorationsProperty` before casting.
  This avoids an `InvalidCastException` when the value is still represented by
  `FreezableDefaultValueFactory`.
- ToggleUnderline now uses the shared `HAS_UNO` inline-formatting fallback so
  selected RichTextBox text receives the toggled underline decoration while
  `TextSelection.ApplyPropertyValue(...)` remains incomplete.

DevFlow additions:

- Snapshot now reports `firstInlineHasUnderline`.
- `richtextbox.probe.toggle-underline-selection-command`

Verified behavior:

- Selecting all text and invoking ToggleUnderline sets underline decoration on
  the first inline.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --no-build
```

Result:

```text
Passed: 16/16
```

Next session:

- Cover repeated ToggleBold/ToggleItalic/ToggleUnderline calls to verify the
  selected inline formatting toggles back to the normal/default state.
- Then add Ctrl+B/Ctrl+I/Ctrl+U key-path coverage once modifier injection is
  validated through the Uno `KeyRoutedEventArgs` probe.
