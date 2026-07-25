### Session 57 - Nested Inline Formatting Edge Cases

Status: completed.

Scope:

- Bold/Italic/Underline toggle, apply-font-size, and apply-foreground all
  work on simple selections. Edge cases with nested/overlapping formatting
  are not fully covered:
  - `Bold` inside `Italic` inside `Span` with partial-selection toggling.
  - Mixed `TextDecorations` inheritance (`Underline` + `Hyperlink`).
  - `ToggleBold`/`ToggleItalic`/`ToggleUnderline` on a selection spanning
    differently-formatted inline elements.
  - `ClearAllFormatting` on a selection with nested inlines.
- `FlorenceEngine.CollectSpans` already handles recursive inline walking,
  but the formatting command path's interaction with `TextRange`-based
  property application may not correctly handle element-boundary crossing.

Implementation:

- Investigate: when a formatting toggle command operates on a selection that
  spans part of a Bold element and part of an adjacent plain Run, does WPF
  split the Bold element at the selection boundary? The upstream
  `TextRangeEdit` / `TextEditor*.cs` code handles this via element splitting
  (`TextRangeEdit.SplitElement`). Verify this path works on the shim.
- Fix any gaps found in element-splitting or formatting-property application
  across element boundaries. Likely areas: `TextRangeEdit.SetPropertyValue`,
  `TextEditorFormatting`, or the `SetValue` path on `TextElement` children
  that are not `Run` (e.g. `Span`, `Bold`, `Italic`).

Tests:

- `ToggleBoldOnPartiallyBoldSelection_SplitsAndAppliesCorrectly`: select
  across bold/plain boundary, toggle bold, verify both segments are bold
  (element split occurred) or both are toggled correctly.
- `ToggleBoldOnMixedBoldItalicSelection_DoesNotCrash`: selection spanning
  Bold inside Italic, toggle bold, verify no crash and correct partial
  application.
- `ClearFormattingOnNestedInlineSelection_FlattensToPlainText`: apply bold
  + italic + underline, then clear all formatting, verify plain text.
- `ApplyFontSizeOnSelectionWithMixedSizes_AppliesUniformSize`: select
  text with mixed font sizes, apply uniform size, verify.

Files modified:

- `src/LeXtudio.Windows/.../Documents/TextRangeEdit.cs` or
  `TextEditorFormatting.uno.cs` — fix element-splitting gaps if found.
- `tests/RichTextBox.IntegrationTests/RichTextBoxIntegrationTests.cs` — new
  tests.

Regression sweep:

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
dotnet test tests/RichTextBox.IntegrationTests/
```

Next session:

- RichTextBox AcceptsReturn visual hidden-state edge case.
