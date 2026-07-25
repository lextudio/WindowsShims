### Session 73 - TextWrapping / TextTrimming Property Wiring

Status: completed.

Scope:

- `RichTextBox.TextWrapping` and `TextBoxBase.TextTrimming` control how
  text behaves when it exceeds the available width. WPF's `TextWrapping`
  values (`Wrap`, `NoWrap`, `WrapWithOverflow`) affect the layout engine's
  line-breaking behavior.
- Currently `FlorenceLayoutEngine` always wraps at `availWidth`. There is
  no `NoWrap` support — content wraps even when the consumer sets
  `TextWrapping="NoWrap"`.
- `TextTrimming` (`None`, `CharacterEllipsis`, `WordEllipsis`) controls
  how overflow text is truncated. Currently not implemented.

Implementation:

- Read `RichTextBox.TextWrapping` and pipe it to `FlorenceLayoutEngine.Format`
  as a parameter.
- When `TextWrapping.NoWrap`: set availWidth to `double.PositiveInfinity`
  (or a very large value) to prevent line breaks.
- When `TextWrapping.Wrap`: current behavior (break at availWidth).
- `TextTrimming`: pass the mode through `FlorenceLine`/`FlorenceRun` so
  `FlowDocumentView.BuildLineVisual` can set `TextTrimming` on each
  `TextBlock`.
- Test that `NoWrap` produces a single line regardless of content width.

Tests:

- `TextWrapping_NoWrap_ProducesSingleLine`: set `TextWrapping=NoWrap`, input
  long text, verify `FlorencePage.Lines.Count == 1` (single line).
- `TextWrapping_Wrap_BreaksLines`: set `TextWrapping=Wrap`, verify multiple
  lines.
- `TextTrimming_CharacterEllipsis_Truncates`: set `TextTrimming`, verify
  `TextBlock.TextTrimming` is set.

Files modified:

- `FlorenceEngine.cs` — accept `TextWrapping` parameter.
- `RichTextBox.uno.cs` — forward `TextWrapping`/`TextTrimming` to layout.
- `FlowDocumentView.uno.cs` — apply `TextTrimming` to `TextBlock` visuals.
