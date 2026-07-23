### Session 23 - Paragraph FlowDirection Through WPF Editing Path

Status: complete.

Scope:

- Fix `FlowDirection` on migrated document content without adding command
  fallbacks.
- Add DevFlow coverage for `ApplyParagraphFlowDirectionLTR` and
  `ApplyParagraphFlowDirectionRTL`.

Product fixes:

- `Block.FlowDirectionProperty` and `Inline.FlowDirectionProperty` now register
  Uno-native dependency properties under `HAS_UNO` instead of reusing WinUI
  `FrameworkElement.FlowDirectionProperty` through `AddOwner`.
- `TextSchema` now classifies the Uno `Block.FlowDirectionProperty` as an
  inheritable paragraph property and the Uno `Inline.FlowDirectionProperty` as
  an inheritable/structural character property.
- `TextEditorParagraphs.OnApplyParagraphFlowDirectionLTR/RTL` uses
  `Block.FlowDirectionProperty` under `HAS_UNO`, while preserving upstream WPF's
  `FrameworkElement.FlowDirectionProperty` path for non-Uno builds.
- The paragraph property worker keeps applying through
  `SetPropertyOnParagraphOrBlockUIContainer` and the normal WPF
  `ClearValue`/`SetValue` path; no RichTextBox-specific fallback was added.
- Plain paragraphs no longer return early from list splitting when the
  selection is not inside a list.

DevFlow additions:

- `richtextbox.probe.apply-paragraph-flow-direction-ltr-selection-command`
- `richtextbox.probe.apply-paragraph-flow-direction-rtl-selection-command`
- `ParagraphFlowDirectionCommand_AppliesFlowDirectionToSelectedParagraph`
  verifies both `LeftToRight` and `RightToLeft`.

Debugging note:

- A temporary `/tmp/richtextbox-flowdirection.log` probe confirmed that the
  command reached `TextRangeEdit.SetParagraphPropertyWorker` and set the local
  `Paragraph` `FlowDirection` value. The temporary file logging was removed
  before verification.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --logger "console;verbosity=minimal"
```

Result:

```text
Passed: 42/42
```

Next session:

- Continue with inline flow direction command coverage and any remaining
  caret/selection behavior that still references
  `FrameworkElement.FlowDirectionProperty`.
