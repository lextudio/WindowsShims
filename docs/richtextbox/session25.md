### Session 25 - FlowDirection Property Audit for Text Positions

Status: complete.

Scope:

- Audit remaining RichTextBox editing paths that read or apply
  `FrameworkElement.FlowDirectionProperty` through text positions.
- Keep UI-scope `FrameworkElement.FlowDirectionProperty` usage intact while
  moving TextElement/position flow direction access to the migrated content
  properties under `HAS_UNO`.

Product fixes:

- `TextSelection` now reads caret/interim flow direction from
  `Inline.FlowDirectionProperty` under `HAS_UNO`, and excludes the same inline
  property from empty-selection springload formatting.
- `TextRangeEdit.MergeFlowDirection(...)` and
  `GetScopingFlowDirectionInline(...)` now use `Inline.FlowDirectionProperty`
  for inline/run flow comparisons and structural inline property application
  under `HAS_UNO`.
- `TextEditor.OnInputLanguageChanged(...)` now reads/applies
  `Inline.FlowDirectionProperty` for content text ranges under `HAS_UNO`.
- `TextEditorTyping.OnFlowDirectionCommand(...)` now applies
  `Block.FlowDirectionProperty` for rich-content paragraph flow-direction
  commands under `HAS_UNO`.
- `TextEditorSelection` now uses `Block.FlowDirectionProperty` for paragraph
  flow-direction reads from text positions under `HAS_UNO`.

Verified behavior:

- Existing paragraph and inline flow-direction DevFlow coverage still passes.
- The full RichTextBox DevFlow integration suite remains green.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --logger "console;verbosity=minimal"
```

Result:

```text
Passed: 44/44
```

Next session:

- Add direct DevFlow coverage for keyboard flow-direction commands
  (`Ctrl+LeftShift` / `Ctrl+RightShift`) if the current Uno key probe can
  represent those keys reliably.
