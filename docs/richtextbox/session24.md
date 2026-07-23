### Session 24 - Inline FlowDirection Through Structural Inline Formatting

Status: complete.

Scope:

- Add DevFlow coverage for `ApplyInlineFlowDirectionLTR` and
  `ApplyInlineFlowDirectionRTL`.
- Keep the command path on upstream WPF structural inline formatting, without a
  RichTextBox command fallback.

Product fix:

- `TextRangeEdit.ApplyStructuralInlinePropertyAcrossParagraphs(...)` now has a
  `HAS_UNO` path for selections whose boundaries sit at the `FlowDocument`
  level. This maps matching document paragraphs to
  `paragraph.ContentStart`/`paragraph.ContentEnd` before re-entering WPF's
  structural inline formatter.
- This avoids the Uno `TextPointer` case where a full-document selection enters
  the paragraph-crossing branch with `start.Paragraph == null`, while still
  applying `Inline.FlowDirectionProperty` through
  `SetStructuralInlineProperty(...)`.

DevFlow additions:

- `richtextbox.probe.apply-inline-flow-direction-ltr-selection-command`
- `richtextbox.probe.apply-inline-flow-direction-rtl-selection-command`
- Snapshot now reports `firstInlineFlowDirection`,
  `firstRunFlowDirection`, and includes `fd=...` in `inlineTree`.
- `InlineFlowDirectionCommand_AppliesFlowDirectionToSelectedText` verifies both
  `LeftToRight` and `RightToLeft` on the first leaf `Run`.

Debugging note:

- A temporary `/tmp/richtextbox-inline-flowdirection.log` probe confirmed the
  fixed path applies the structural property directly to the selected existing
  `Run`. The temporary file logging was removed before verification.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --logger "console;verbosity=minimal"
```

Result:

```text
Passed: 44/44
```

Next session:

- Continue auditing remaining caret/selection paths that still read
  `FrameworkElement.FlowDirectionProperty` from text positions.
