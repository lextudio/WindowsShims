### Session 83 - RTF Round-Trip for FontSize / FontFamily / Foreground / Strikethrough

Status: completed.

Scope:

- Sessions 81/82 made RTF save/load round-trip text + Bold/Italic/Underline and
  mixed/nested/structural content. This session closes the remaining inline
  formatting gaps so FontSize (mixed and whole-paragraph), FontFamily,
  Foreground color, Strikethrough, and combined `Underline, Strikethrough` all
  survive a `DataFormats.Rtf` save/load round-trip.

Root causes found:

1. **`XamlToRtfWriter` ignored dotted owner-qualified attribute names.** The
   shim serializes inheritable text properties as `Owner.Property`
   (e.g. `TextElement.FontSize="14"`, `TextElement.FontFamily="..."`), but the
   writer's `AttributeTable` only knows the short names (`FontSize`,
   `FontFamily`, ...). As a result the document root's `FontSize="14"` was
   never parsed, the writer kept the RTF default `_fs = 24`, and mixed sizes
   merged into a single `\fs24` — so a `16px` run no longer differed from its
   siblings and the round-trip flattened the size. Fixed in `HandleAttributes`
   (`#if HAS_UNO`) by stripping the `Owner.` prefix before the attribute lookup;
   `xml:lang`/`xml:space` carry no dot and are unaffected.
2. **Empty/default `FontFamily` serialized as its type name.** Without a shim
   converter, `GetStringValue` fell through to the default `TypeConverter`,
   which returned `FontFamily.ToString()` = `"Microsoft.UI.Xaml.Media.FontFamily"`.
   Once the dotted names were parsed this would have polluted the RTF font table
   with a bogus entry. Fixed in `DPTypeDescriptorContext.GetStringValue`
   (`#if HAS_UNO`) to serialize `FontFamily` via its `Source`; an empty Source
   yields `FontFamily=""`, which `XamlParserHelper.ConvertToFont` already
   rejects (`Length == 0` → false), leaving the default font intact.
3. **Combined `TextDecorations` were dropped at serialization.** The
   `TextDecorationsFixup` special case only matched the single-declaration
   singletons (Underline, Strikethrough, OverLine, Baseline); a combined
   collection (`Underline + Strikethrough`) matched nothing and returned null,
   routing it to the complex-properties path that the RTF writer ignores.
   Fixed by falling back to a comma-separated list built from each
   `TextDecoration.Location` (`"Underline, Strikethrough"`), which the RTF
   writer's `ConvertToDecoration` (uses `Contains`) and the shim `XamlReader`'s
   `ParseTextDecorations` both already understand.
4. **Two tests asserted the wrong inline.** `FirstInlineForeground` /
   `FirstInlineFontFamily` descend to the *first* inline; the original tests put
   the plain run first, so they read the default black / Segoe UI. Reordered the
   formatted run to the front of the paragraph (and fixed the joined-text
   assertion, `arialplain` / `greenplain`).

Notes / decisions:

- The FontFamily fix lives in `DPTypeDescriptorContext` next to the existing
  Foreground/Background/FontWeight/FontStyle `#if HAS_UNO` branches, keeping the
  upstream non-Uno code path untouched.
- The `ParseParagraph` attribute application and `ParseTextDecorations` from
  session 82's tail are what re-apply the RTF converter's paragraph-level
  (`FontSize="12pt"`) and span-level (`TextDecorations="Underline, Strikethrough"`,
  `FontFamily="Arial"`, `Foreground="#FF00AA00"`) output on load.
- The temporary `rtf-intermediate-tmp` probe in `MainPage.cs` (used to confirm
  the green `\cf2`/`{\colortbl ...\red0\green170\blue0;}` survived RTF but was
  lost at the test's assertion point) and the `Assert.Fail` diagnostic in the
  Foreground test were removed once the round-trip was verified.

Tests:

- Integration (5 new in this session, 205/205 total): `SaveLoad_Rtf_RoundTripsFontSize`
  (mixed sizes → `<Span FontSize="12pt">`, `z=16`), `SaveLoad_Rtf_RoundTripsFontSizeOnUniformParagraph`
  (whole-paragraph size → `<Paragraph FontSize="12pt">`, `FirstParagraphFontSize == 16`),
  `SaveLoad_Rtf_RoundTripsFontFamily` (`FirstInlineFontFamily` contains `Arial`),
  `SaveLoad_Rtf_RoundTripsForegroundColor` (`FirstInlineForeground == "#FF00AA00"`),
  `SaveLoad_Rtf_RoundTripsCombinedUnderlineAndStrikethrough` (`d=U` and `st=S` in the
  `InlineTree` encoding).
- Model tests: 234/234 (`LeXtudio.Windows.Tests`).

Files modified:

- `ext/wpf/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Documents/XamlToRtfWriter.cs` (dotted-owner-name stripping in `HandleAttributes`)
- `ext/wpf/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Documents/DPTypeDescriptorContext.cs` (`FontFamily` Source serialization; combined `TextDecorations` fixup)
- `src/LeXtudio.Windows/System.Windows/Markup/XamlReader.cs` (session 82 tail: `ParseParagraph` attribute application, `ParseTextDecorations`, `ApplyInlineProperty` FontFamily)
- `tests/RichTextBox.IntegrationTestHost/MainPage.cs` (`st=` strikethrough encoding, `firstParagraphFontSize` snapshot; temporary probe removed)
- `tests/RichTextBox.IntegrationTests/RichTextBoxIntegrationTests.cs` (5 new RTF tests)
- `docs/richtextbox/index.md`, `docs/richtextbox/RICHTEXTBOX-PORT-CATALOG.md` (counts/status)

Result:

- 205/205 RichTextBox integration tests pass.
- 234/234 model tests pass.
- RTF save/load now round-trips FontSize (mixed + uniform), FontFamily,
  Foreground color, Strikethrough, and combined `Underline, Strikethrough`.
