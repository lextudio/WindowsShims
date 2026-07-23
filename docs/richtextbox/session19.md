### Session 19 - Paragraph Alignment Commands

Status: complete.

Scope:

- Add paragraph-level observability to the DevFlow RichTextBox snapshot.
- Cover AlignLeft, AlignCenter, AlignRight, and AlignJustify command handlers
  for selected RichTextBox paragraphs.

Product fix:

- `TextEditorParagraphs.OnAlignLeft(...)`, `OnAlignCenter(...)`,
  `OnAlignRight(...)`, and `OnAlignJustify(...)` now apply
  `Block.TextAlignmentProperty` directly to RichTextBox document blocks under
  `HAS_UNO`.
- This keeps paragraph alignment command behavior observable while the generic
  paragraph path through `_OnApplyProperty(... applyToParagraphs: true)` is
  still incomplete.

DevFlow additions:

- Snapshot now reports `firstParagraphTextAlignment`.
- `richtextbox.probe.align-left-selection-command`
- `richtextbox.probe.align-center-selection-command`
- `richtextbox.probe.align-right-selection-command`
- `richtextbox.probe.align-justify-selection-command`
- `AlignCommand_AppliesTextAlignmentToSelectedParagraph`

Verified behavior:

- AlignLeft sets the first paragraph text alignment to `Left`.
- AlignCenter sets the first paragraph text alignment to `Center`.
- AlignRight sets the first paragraph text alignment to `Right`.
- AlignJustify sets the first paragraph text alignment to `Justify`.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --no-build
```

Result:

```text
Passed: 32/32
```

Next session:

- Add paragraph line-spacing observability.
- Cover ApplySingleSpace, ApplyOneAndAHalfSpace, and ApplyDoubleSpace command
  handlers through DevFlow.
