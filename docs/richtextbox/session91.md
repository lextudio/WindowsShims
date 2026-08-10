### Session 91 - RTF Round-Trip for Inline Language (Language / \langN / xml:lang)

Status: completed.

Scope:

- Sessions 81-90 covered RTF round-trips for text, inline/paragraph formatting,
  lists, hyperlinks, table cells, and super/subscript. This session makes the
  inline `Language` property survive RTF save/load as `\langN` (LCID).

Findings:

- **The upstream RTF stack already handled language on both sides.** The
  `XamlToRtfWriter` has an `XALang` attribute case that converts a culture name
  to an LCID and emits `\langN`, and the `RtfToXamlReader` processes `\langN`
  into both `Lang` and `LangCur` and re-emits `xml:lang="<culture>"` per-run
  (WPF does not write lang at paragraph level by design). The shim-side gaps
  were on the XAML boundaries:
  1. `TextRangeSerialization.WriteXaml` serializes the shim's inheritable
     `Microsoft.UI.Xaml.FrameworkElement.LanguageProperty` as a plain
     `Language="<culture>"` attribute (the `xml:lang` special-case in
     `WriteInheritablePropertiesForFlowDocument` only matches WPF's
     `FrameworkContentElement.LanguageProperty`, which is not the property in
     the shim's inheritable list). The RTF writer's `AttributeTable` only knew
     `xml:lang`, so the `Language` attribute was dropped.
  2. The shim `XamlReader` had no case for `Language`/`xml:lang`, so neither
     the user-facing `Language` attribute nor the reader's `xml:lang` re-emit
     was applied on load.
- Note: the shim's `ReadLocalValue` returns effective values for the
  inheritable list, so the document root always carries a `Language="en-US"`
  attribute and every RTF save now emits a leading `\lang1033` (culture-
  dependent) — harmless, since the reader only re-emits `xml:lang` when the
  value differs from the parent context, and it matches how the shim
  serializes the property.
- `FontStretch` was probed as an alternative candidate and confirmed a
  WPF-faithful drop: `XamlToRtfWriter` maps it to character expansion
  (`formatState.Expand`), but `RtfToXamlReader` refuses to convert RTF
  expansion back to stretch ("Avalon does not support the RTF notion of
  Expand", line 5404). Same category as LineHeight (`\sl` written, `\sl`
  re-emit commented out) — documented drops, not port gaps.

Changes:

- `ext/wpf` submodule: `XamlToRtfWriter.cs` — added the bare `"Language"`
  entry to `XamlParserHelper.AttributeTable` (mapping to the existing
  `XALang`), since the shim's intermediate XAML uses `Language` rather than
  `xml:lang`.
- `XamlReader.cs` (`ApplyInlineProperty` and `ParseParagraph`): new
  `Language`/`xml:lang`/`lang` cases that set
  `Microsoft.UI.Xaml.FrameworkElement.LanguageProperty` on the element.
  (`XmlReader.LocalName` already drops the `xml:` prefix, hence the `lang` arm.)
- `MainPage.cs`: snapshot gained `firstInlineLanguage` (the first inline's
  WinUI `FrameworkElement.LanguageProperty` value).

Tests:

- Integration (1 new, 225/225 total):
  - `SaveLoad_Rtf_RoundTripsInlineLanguage` — `Language="de-DE"` on a Run
    survives: raw RTF contains `\lang1031` and the value reloads as `de-DE`.
- Model tests: 234/234 (`LeXtudio.Windows.Tests`).

Result:

- 225/225 RichTextBox integration tests pass; 234/234 model tests pass.
- Inline language now round-trips through RTF save/load.
