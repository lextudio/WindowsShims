### Session 90 - RTF Round-Trip for Superscript/Subscript (Typography.Variants)

Status: completed.

Scope:

- Sessions 81-89 covered RTF round-trips for text, inline/paragraph formatting,
  lists, hyperlinks, and table cells. This session makes
  `Typography.Variants="Superscript|Subscript"` survive RTF save/load as
  `\super`/`\sub`.

Findings:

- **The upstream RTF stack already handled superscript/subscript.** The
  `XamlToRtfWriter` has an `XATypographyVariants` attribute case that emits
  `\super`/`\sub`, and the `RtfToXamlReader` parses them back and emits
  `<Span Typography.Variants="Superscript|Subscript">`. Two shim-side gaps
  dropped the value before it ever reached RTF:
  1. `Typography.VariantsProperty` was registered with a 4-arg
     `RegisterAttached` overload that defaulted `validateValueCallback` to a
     WPF `ValidateValueCallback` type; the Uno property-system shim's binding
     of that callback type threw at DP registration, before a `Run` could be
     created. The fix adds the explicit 5-arg overload with `null` (matching
     the pattern used by every other property in `Typography.cs`).
  2. `XamlToRtfWriter`'s `#if HAS_UNO` owner-strip hack (added for the WinRT
     shim's `TextElement.FontSize`-style serialization) reduces every dotted
     attribute to its short name before the `AttributeTable` lookup, so the
     qualified `"Typography.Variants"` entry never matched and the attribute
     was dropped. Adding a bare `"Variants"` entry (same `XATypographyVariants`
     mapping) makes the lookup robust to either form.
- The shim `XamlReader` already strips the owner from `<Span
  Typography.Variants>` on load (the same `StripQualifier` path), so a new
  `case "Variants"` carries the value onto the element via the
  `VariantsProperty` — this lets `WriteXaml` serialize it back to `\super`/`\sub`.

Changes:

- `ext/wpf` submodule: `Typography.cs` — registered `VariantsProperty` with the
  5-arg `RegisterAttached(..., null)` overload; `XamlToRtfWriter.cs` — added the
  bare `"Variants"` entry to `XamlParserHelper.AttributeTable`.
- `XamlReader.cs` (`ApplyLocalPropertyValue`): new `case "Variants"` that parses
  the value into `FontVariants` and stores it on `Typography.VariantsProperty`.
- `MainPage.cs`: snapshot gained `firstInlineVariants` (the first inline's
  `Typography.Variants` value).

Tests:

- Integration (2 new, 224/224 total):
  - `SaveLoad_Rtf_RoundTripsSuperscript` — `Typography.Variants="Superscript"`
    survives (raw RTF contains `\super`, value reloads as `Superscript`).
  - `SaveLoad_Rtf_RoundTripsSubscript` — `Typography.Variants="Subscript"`
    survives (raw RTF contains `\sub`, value reloads as `Subscript`).
- Model tests: 234/234 (`LeXtudio.Windows.Tests`).

Result:

- 224/224 RichTextBox integration tests pass; 234/234 model tests pass.
- Superscript and subscript now round-trip through RTF save/load.
