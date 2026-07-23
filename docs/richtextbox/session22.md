### Session 22 - Partial Selection Formatting and WPF Line Spacing No-Op

Status: complete.

Scope:

- Add DevFlow coverage for formatting only a subrange inside the first `Run`.
- Lock `ApplySingleSpace`, `ApplyOneAndAHalfSpace`, and `ApplyDoubleSpace` to
  upstream WPF behavior.

Product notes:

- No command-level formatting fallback was added.
- Upstream WPF `TextEditorParagraphs.OnApplySingleSpace(...)`,
  `OnApplyOneAndAHalfSpace(...)`, and `OnApplyDoubleSpace(...)` are empty
  handlers. The migrated shim should therefore preserve no-op behavior unless
  upstream WPF changes.
- While probing paragraph commands, `ApplyParagraphFlowDirectionRTL` exposed a
  separate Uno compatibility issue: `Block.FlowDirectionProperty` is an
  `AddOwner` of WinUI `FrameworkElement.FlowDirectionProperty`, and setting it
  on document content can trigger WinUI's `FrameworkElement` backing-field
  callback against a non-FrameworkElement target. This needs a focused DP-owner
  compatibility fix, not a RichTextBox command fallback.

DevFlow additions:

- `richtextbox.probe.toggle-bold-run-range-command`
- `richtextbox.probe.toggle-italic-run-range-command`
- `richtextbox.probe.toggle-underline-run-range-command`
- `richtextbox.probe.apply-single-space-selection-command`
- `richtextbox.probe.apply-one-and-a-half-space-selection-command`
- `richtextbox.probe.apply-double-space-selection-command`
- Snapshot now reports paragraph `LineHeight`, `LineStackingStrategy`, and
  `FlowDirection`.
- `inlineTree` now includes per-run text, weight, style, size, and underline
  state.

Verified behavior:

- Formatting `abcdef` range `[2, 4)` splits the first run so `ab` and `ef`
  remain unformatted while `cd` receives Bold, Italic, or Underline.
- WPF line-spacing commands leave paragraph `LineHeight` as `NaN` and
  `LineStackingStrategy` as `MaxHeight`.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --filter "FullyQualifiedName~WithPartialRunSelection|FullyQualifiedName~LineSpacingCommand_MatchesWpfNoOpBehavior"
```

Result:

```text
Passed: 6/6
```

Next session:

- Fix content-element `FlowDirection` DP owner compatibility so upstream
  `ApplyParagraphFlowDirectionLTR/RTL` can run through the WPF property path.
- Add DevFlow coverage for paragraph flow direction once that DP issue is
  fixed.
