### Session 86 - RTF Round-Trip for Inline FlowDirection

Status: completed.

Scope:

- Sessions 81-85 closed character- and paragraph-level RTF round-trips. This
  session closes the last standard formatting gap: a run whose `\rtlch`/`\ltrch`
  direction differs from its paragraph now survives a `DataFormats.Rtf`
  save/load round-trip.

Root cause found:

- The RTF chain already handled inline direction on both sides —
  `XamlToRtfWriter` maps a `<Span FlowDirection>` attribute to `\rtlch`/`\ltrch`
  on the run (`XAFlowDirection` → `formatState.DirChar`), and `RtfToXamlReader`
  wraps runs whose direction differs from the parent in
  `<Span FlowDirection="LeftToRight|RightToLeft">`. The only gap was the shim's
  `XamlReader`: `ApplyInlineProperty` (used by `ParseSpanInline`) had no
  `FlowDirection` case, so the attribute was silently dropped when the RTF's
  XAML was parsed back.
- Note: `FlowDirection` lives on `Inline` (not `TextElement`) — `Inline.cs`
  registers `Inline.FlowDirectionProperty` while `TextElement` has none — so the
  new case applies the value via an `is Inline` pattern, mirroring how
  `TextDecorations` is applied via `Inline.TextDecorationsProperty`.

Notes / decisions:

- WPF is not the fidelity concern here: since `Inline` carries a real
  `FlowDirectionProperty`, WPF round-trips the value too. The fix is
  WPF-faithful, not an extension.
- A temporary `richtextbox.probe.rtf-intermediate-tmp` probe was added to dump
  the intermediate RTF and confirmed the writer emits `{\ltrch ltr }{\rtlch rtl}`
  for a mixed-direction paragraph; it was removed again once the round-trip was
  verified.
- Test assertions use the span as the first inline: the snapshot's
  `firstInlineFlowDirection`/`firstInlineType` describe the first inline in the
  first paragraph, so a `<Span FlowDirection>` must precede the plain run for
  the assertions to observe it. The `inlineTree` encoding (`fd=`) is asserted
  for the non-default `RightToLeft` value.

Tests:

- Integration (2 new, 214/214 total):
  - `SaveLoad_Rtf_RoundTripsInlineFlowDirection` — a leading
    `<Span FlowDirection="RightToLeft">` survives reload as a `Span` with
    `FlowDirection == RightToLeft` (and `fd=RightToLeft` in the inline tree).
  - `SaveLoad_Rtf_RoundTripsMixedDirectionRuns` — a `LeftToRight` span inside a
    `RightToLeft` paragraph keeps its own direction while the paragraph
    direction is preserved.
- Model tests: 234/234 (`LeXtudio.Windows.Tests`).

Files modified:

- `src/LeXtudio.Windows/System.Windows/Markup/XamlReader.cs` (`ApplyInlineProperty` applies `FlowDirection` to `Inline`)
- `tests/RichTextBox.IntegrationTests/RichTextBoxIntegrationTests.cs` (2 new tests + `FirstInlineType` helper)
- `docs/richtextbox/index.md`, `docs/richtextbox/RICHTEXTBOX-PORT-CATALOG.md` (counts/status)

Result:

- 214/214 RichTextBox integration tests pass.
- 234/234 model tests pass.
- RTF save/load now round-trips inline FlowDirection, completing the standard
  character-, paragraph-, and inline-level formatting surface.
