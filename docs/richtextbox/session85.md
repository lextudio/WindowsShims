### Session 85 - RTF Round-Trip for Paragraph TextAlignment / FlowDirection / Margin / TextIndent

Status: completed.

Scope:

- Sessions 81-84 closed character-level RTF round-trips. This session covers
  paragraph-level formatting: `TextAlignment`, `FlowDirection`, `Margin`, and
  `TextIndent` now survive a `DataFormats.Rtf` save/load round-trip.
  `LineHeight` is verified as a WPF-faithful drop.

Root causes found:

1. **`Thickness` serialized as its `ToString()` in XAML.** `WriteNoninheritableProperties`
   calls `DPTypeDescriptorContext.GetStringValue` for `Block.MarginProperty`; the
   shim's `Thickness` struct carries `[TypeConverter(typeof(ThicknessConverter))]`,
   but under Uno `TypeDescriptor.GetConverter` does not pick it up, so the value
   serialized as `Margin="[Thickness: 20-0-0-0]"`. `XamlToRtfWriter.ConvertToThickness`
   cannot parse that, so no `\li` was emitted and the margin came back as zero.
   Fixed in `DPTypeDescriptorContext.GetStringValue` (`#if HAS_UNO`): any
   `Thickness`-typed property value now serializes as `"left,top,right,bottom"`
   (comma-separated, invariant culture), the form the RTF writer's
   `ConvertToThickness` and the shim's `XamlReader.TryParseThickness` both expect.
2. **`ParseParagraph` ignored the other paragraph attributes.** It already applied
   `FontSize`/`FontFamily`/`FontWeight`/`FontStyle`/`Foreground`/`Background`.
   Extended the attribute switch to also apply `TextAlignment` and `FlowDirection`
   (WinUI enums via the global aliases), `Margin` (via a new `TryParseThickness`),
   `TextIndent`, and `LineHeight`. The RTF chain already wrote the corresponding
   control words (`\ql/\qc/\qr`, `\rtlpar`, `\li/\ri/\sb/\sa`, `\fi`) and
   `RtfToXamlReader` already emitted the matching `<Paragraph>` attributes, so the
   load side was the only gap.

Notes / decisions:

- `LineHeight` is intentionally dropped. The RTF writer emits `\sl` from
  `XALineHeight`, but `RtfToXamlReader` deliberately does not read it back
  ("Avalon only supports lineheight exact - we're just not going to output it").
  WPF behaves identically, so the round-trip test asserts the drop rather than
  forcing a non-faithful extension. `ParseParagraph` still applies a local
  `LineHeight` from straight XAML so the drop is exercised by the RTF path.
- The `Thickness` fix is general (any `Thickness`-typed property), matching the
  existing FontWeight/FontStyle/FontFamily/Foreground/Background shim pattern in
  `GetStringValue`.
- The temporary `rtf-intermediate-tmp` probe (re-added for the margin diagnosis,
  which confirmed `xamlSave` carried `[Thickness: ...]` while `xamlBack` carried
  `Margin="20.00,0.00,0.00,0.00"` after the fix) was removed again once the
  round-trip was verified.

Tests:

- Integration (5 new, 212/212 total): `SaveLoad_Rtf_RoundTripsParagraphTextAlignment`
  (`TextAlignment="Center"` survives), `SaveLoad_Rtf_RoundTripsParagraphFlowDirection`
  (`RightToLeft` survives), `SaveLoad_Rtf_RoundTripsParagraphMargin` (`20,0,0,0`
  survives as a `Thickness`), `SaveLoad_Rtf_RoundTripsParagraphTextIndent` (`\fi`
  restored as `TextIndent`), `SaveLoad_Rtf_DropsParagraphLineHeightLikeWpf`
  (text survives, `\sl` not read back).
- Test host snapshots gained `firstParagraphMargin` and `firstParagraphTextIndent`.
- Model tests: 234/234 (`LeXtudio.Windows.Tests`).

Files modified:

- `ext/wpf/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Documents/DPTypeDescriptorContext.cs` (`Thickness` → `"l,t,r,b"` serialization)
- `src/LeXtudio.Windows/System.Windows/Markup/XamlReader.cs` (`ParseParagraph` applies TextAlignment/FlowDirection/Margin/TextIndent/LineHeight; `TryParseThickness` helper)
- `tests/RichTextBox.IntegrationTestHost/MainPage.cs` (snapshot fields)
- `tests/RichTextBox.IntegrationTests/RichTextBoxIntegrationTests.cs` (5 new tests)
- `docs/richtextbox/index.md`, `docs/richtextbox/RICHTEXTBOX-PORT-CATALOG.md` (counts/status)

Result:

- 212/212 RichTextBox integration tests pass.
- 234/234 model tests pass.
- RTF save/load now round-trips paragraph-level TextAlignment, FlowDirection,
  Margin, and TextIndent, and drops LineHeight WPF-faithfully.
