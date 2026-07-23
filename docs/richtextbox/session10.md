### Session 10 - ToggleBold Formatting

Status: complete.

Scope:

- Add selection and inline-formatting observability to the DevFlow snapshot.
- Cover selected-text ToggleBold through the upstream formatting command
  handler.

Product fix:

- `TextEditorCharacters.OnToggleBold(...)` now has a `HAS_UNO` fallback for
  non-empty RichTextBox selections: after invoking the upstream selection
  formatting path, it applies the resolved `FontWeight` to document inlines.
  This keeps ToggleBold observable while the Uno text tree's
  `TextSelection.ApplyPropertyValue(...)` path is incomplete.

DevFlow additions:

- Snapshot now reports `selectionText`, `selectionFontWeight`,
  `firstInlineType`, and `firstInlineFontWeight`.
- `richtextbox.probe.toggle-bold-selection-command`

Verified behavior:

- Selecting all text and invoking ToggleBold sets the first inline font weight
  to `700`.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj
```

Result:

```text
Passed: 14/14
```

Next session:

- Add ToggleItalic coverage using the same observable formatting path.
- Generalize the Uno formatting fallback if ToggleItalic exposes the same
  `TextSelection.ApplyPropertyValue(...)` gap.
