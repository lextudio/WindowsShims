### Session 59 - Mixed FlowDirection Edge Cases

Status: completed.

Scope:

- Sessions 23-26 added `Paragraph.FlowDirection` and `Inline.FlowDirection`
  coverage through the WPF editing command path and keyboard shortcuts.
  Uncovered: selection spanning paragraphs with **different** flow directions,
  and mixed `FlowDirection` at the inline level within a single paragraph.
- When `FlowDirection` differs across a selection, `ToggleFlowDirection`
  should apply the new direction uniformly (overriding the mixed state).
  Verify this works and doesn't crash.
- Inline `FlowDirection` on `Run`/`Span` inside a paragraph with a different
  paragraph-level `FlowDirection` — the inline should take precedence for
  its content. Verify the `FlorenceLayoutEngine` renders correctly (the
  engine currently doesn't read `FlowDirection` at all, so both LTR and RTL
  text are left-aligned — document this limitation).
- Verify that `FlowDirection` round-trips through `Text` save/load.

Implementation:

- No production-code changes expected unless a crash or incorrect behavior
  is found. The `FlowDirection` property is already wired through WPF's
  `TextRangeEdit.SetParagraphProperty` path.
- Document the `FlorenceLayoutEngine` limitation: `FlowDirection` affects
  text extraction and caret semantics but not visual alignment.

Tests:

- `ToggleFlowDirectionOnMixedSelection_AppliesUniformDirection`: create two
  paragraphs with LTR/RTL, select both, toggle flow direction, verify both
  now have the same direction.
- `InlineFlowDirection_OverridesParagraphDirection`: create a paragraph with
  RTL direction, set a Run to LTR, verify the inline's FlowDirection is
  independent of the paragraph's direction.
- `FlowDirection_RoundTripsThroughTextSaveLoad`: create paragraph with RTL
  direction, save to Text (DataFormats.Text), reload, verify direction
  preserved (or document that Text format strips direction).

Files modified:

- `tests/RichTextBox.IntegrationTests/RichTextBoxIntegrationTests.cs` — new
  tests.

Regression sweep:

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
dotnet test tests/RichTextBox.IntegrationTests/
```

Next session:

- TextPointer/TextRange offset validation across mixed document structures.
