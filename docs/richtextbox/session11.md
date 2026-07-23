### Session 11 - ToggleItalic Formatting

Status: complete.

Scope:

- Extend selected-text formatting coverage from ToggleBold to ToggleItalic.
- Generalize the `HAS_UNO` inline-formatting fallback.

Product fix:

- The ToggleBold fallback is now a shared
  `ApplyPropertyToDocumentInlines(...)` helper.
- `TextEditorCharacters.OnToggleItalic(...)` uses the same fallback for
  selected RichTextBox text because Uno `TextSelection.ApplyPropertyValue(...)`
  does not yet update inline properties reliably.

DevFlow additions:

- Snapshot now reports `firstInlineFontStyle`.
- `richtextbox.probe.toggle-italic-selection-command`

Verified behavior:

- Selecting all text and invoking ToggleItalic sets the first inline font style
  to `Italic`.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj
```

Result:

```text
Passed: 15/15
```

Next session:

- Add key-path Ctrl+B/Ctrl+I coverage if modifier injection can be added to
  the `KeyRoutedEventArgs` probe.
- Or cover ToggleUnderline with inline text decoration observability and the
  same formatting fallback.
