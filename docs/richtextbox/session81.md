### Session 81 - RTF Clipboard Serialization Round-Trips Text + Formatting

Status: completed.

Scope:

- `TextRange.Save`/`Load` with `DataFormats.Rtf` previously ran without
  crashing but lost all content (the round-tripped document was empty). The
  WPF `XamlRtfConverter` stack (`XamlToRtfWriter` / `RtfToXamlReader`) was
  already linked and produced valid intermediate RTF/XAML — the loss was in the
  shim-side consumers.

Root causes found:

1. **Shim `XamlReader` dropped text-node content.** WPF's RTF→XAML conversion
   emits text as element content — `<Span>rtf bold text</Span>` — not as a
   `<Run Text="..."/>` attribute. The shim's `XamlReader.Parse` only processed
   child *elements*, so the text node was silently discarded and the Span came
   back empty. Fixed by capturing `Text`/`SignificantWhitespace` nodes into a
   `Run` in both `PopulateSpan` and `ParseParagraph`.
2. **Shim `XamlReader` ignored `Span` attributes.** `<Span FontWeight="Bold"
   Foreground="...">` had its formatting dropped because `ParseSpanInline`
   never read attributes. Extracted a shared `ApplyInlineProperty` helper (also
   adding `pt`/`px` font-size unit handling) used by both `Run` and `Span`.
3. **`FontWeightConverter` emitted numeric weights.** `ConvertToInvariantString`
   returned `"700"`, but WPF emits `"Bold"` and the upstream `XamlToRtfWriter`
   only parses `"Bold"`/`"Normal"` — so RTF output had no `\b` and bold was
   lost before Load ever ran. Now emits WPF-faithful named weights.

Notes / decisions:

- After round-trip, RTF wraps formatting on a `<Span>` (as WPF does). The
  shim's property system reports only Default/Local (no inheritance — see
  `System.Windows/PropertySystem.cs`), so a nested `Run`'s `GetValue(FontWeight)`
  does not see the parent `Span`'s bold. The integration test therefore asserts
  `FirstInlineFontWeight` (the Span's own value). Full DP inheritance remains a
  separate, broader parity item.

Tests:

- Integration: `SaveLoad_Rtf_RoundTripsTextAndFormatting` — strengthened from
  "doesn't crash" to asserting the text and bold both survive an RTF
  save/load round-trip.
- Unit: `FontWeightConverterEmitsWpfNamedWeights` (Bold/Normal/ExtraLight/Black).

Files modified:

- `src/LeXtudio.Windows/System.Windows/Markup/XamlReader.cs`
- `src/LeXtudio.Windows/System.Windows/Media/FontWeightConverter.cs`
- `tests/RichTextBox.IntegrationTests/RichTextBoxIntegrationTests.cs`
- `src/LeXtudio.Windows.Tests/RichTextBoxDocumentsTests.cs`

Result:

- 193/193 RichTextBox integration tests pass (1 strengthened, 0 failures).
- 234/234 model tests pass (`LeXtudio.Windows.Tests`).
- RTF clipboard serialization now preserves text and inline formatting.
