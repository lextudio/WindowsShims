### Session 84 - RTF Round-Trip for Background + Overline/Baseline Behavior

Status: completed.

Scope:

- Sessions 81-83 made RTF save/load round-trip text plus Bold/Italic/Underline,
  Strikethrough, FontSize, FontFamily, Foreground, and combined
  `Underline, Strikethrough`. This session closes character-level coverage with
  the background/highlight color round-trip, and pins down the behavior of the
  two `TextDecorations` kinds RTF cannot encode (OverLine, Baseline).

Findings:

1. **Background round-trips.** The RTF writer encodes a run's `Background` as a
   run-level `\highlightN` (WPF style: `N` indexes the shared `colortbl`), and
   `RtfToXamlReader` restores it as a `<Span Background="#FF...">` attribute
   (`AppendInlineXamlPrefix`, `fsThis.CB != fsParent.CB`) which the shim
   `XamlReader`'s `ApplyInlineProperty` "Background" case parses back into a
   `SolidColorBrush`. No code change was needed — the whole path already worked;
   it just had no test.
2. **OverLine/Baseline are dropped at save, WPF-faithfully.** RTF has only
   `\ul` and `\strike`; there is no encoding for overline or baseline
   decorations. `XamlParserHelper.ConvertToDecoration` only matches
   "Underline"/"Strikethrough", so WPF's `XamlToRtfWriter` silently drops
   OverLine/Baseline. The shim matches this exactly. The test asserts the
   predictable outcome (text survives, no `d=U`/`st=S` decoration is produced)
   rather than attempting a non-standard RTF extension.

Notes / decisions:

- No production code changed this session — the background path was already
  complete and just needed coverage; the OverLine/Baseline behavior is
  intentionally WPF-faithful.
- Background colors that are not in RTF's 16-color highlight palette are not an
  issue here: the shim (like WPF) writes `\highlightN` as an index into the
  shared colortbl, so arbitrary colors round-trip within the WPF-to-WPF chain.

Tests:

- Integration (2 new, 207/207 total): `SaveLoad_Rtf_RoundTripsBackground`
  (run-level `Background="#FFFFFF00"` survives as `FirstInlineBackground ==
  "#FFFFFF00"`), `SaveLoad_Rtf_DropsOverlineAndBaselineLikeWpf` (text survives,
  decorations dropped).

Files modified:

- `tests/RichTextBox.IntegrationTests/RichTextBoxIntegrationTests.cs` (2 new tests)
- `docs/richtextbox/index.md`, `docs/richtextbox/RICHTEXTBOX-PORT-CATALOG.md` (counts/status)

Result:

- 207/207 RichTextBox integration tests pass.
- 234/234 model tests pass.
- All character-level formatting now has RTF round-trip coverage or a documented
  WPF-faithful drop.
