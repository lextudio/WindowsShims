### Session 82 - RTF Round-Trip for Mixed/Nested Inline Formatting

Status: completed.

Scope:

- Session 81 made RTF save/load round-trip text + a single bold run. This
  session fixes the remaining gaps so that mixed formatting (`Bold` +
  `Italic` + `Underline`), nested formatting (`<Bold>` inside `<Italic>`),
  and multi-paragraph/list/table/unicode/hyperlink content all survive a
  `DataFormats.Rtf` save/load round-trip.

Root causes found:

1. **Reader-position bug in the shim `XamlReader` flattened sibling inlines.**
   `ParseRun` used `ReadElementContentAsString()`, which leaves the `XmlReader`
   positioned on the *next sibling* element; the parent loop's following
   `reader.Read()` then jumped into that sibling's subtree, so any inline that
   immediately followed a `<Run>` was flattened to a bare `Run` (its children
   promoted). This is why Bold and Underline (which came after a leading
   Italic/Run) were lost. Fixed by consuming the Run's text manually
   (Text/SignificantWhitespace nodes, advancing to the `</Run>` end tag) so the
   caller's next `Read()` reaches the true sibling. Self-closing
   `<Run Text="..."/>` still leaves the reader on the element. The same
   over-skip affected `ParseInline`'s `LineBreak`/default case and
   `ParseBlock`'s default case (`reader.Skip()` replaced with a
   `ConsumeUnknownElement` helper that leaves the reader on the element's end
   tag or on the element when empty).
2. **Bold/Italic/Underline carried no serializable local value.**
   `WriteXaml`'s `TextSchema.GetStandardElementType` reduces these marker
   elements to a bare `<Span>`, so `WriteInheritableProperties` had nothing to
   serialize and `XamlToRtfWriter` emitted plain `\ltrch` runs. Fixed by setting
   local values in `ParseBold`/`ParseItalic`/`ParseUnderline`:
   `FontWeight=Bold`, `FontStyle=Italic`,
   `TextDecorations=Underline` respectively. Rendering is unaffected — it is
   type-based (`FlorenceEngine` `CollectSpans`/`GetTextDecorations`), so there
   is no double-formatting.
3. **No DP inheritance → redundant `\b0`/`FontWeight="Normal"` on inner Runs.**
   WPF's `WriteInheritableProperties` only writes a property when its value
   differs between inner/outer scope; a Run nested in a Bold span compares
   equal in real WPF because DP inheritance makes the Run report Bold. The
   shim's property system reports only Default/Local (`PropertySystem.cs`), so
   the inner Run serialized `FontWeight="Normal"` and the RTF contained
   `{\b \b0 bold}`. Fixed with a targeted inheritance simulation in
   `ITextPointer.GetValue` (`ext/wpf/.../TextPointer.cs`): for the inheritable
   text properties (rebuilt from `TextSchema.GetInheritableProperties`), walk
   the `TextElement.Parent` chain from the scoping element and return the
   nearest local value. Only inheritable properties participate —
   `TextDecorations`, `Background`, etc. keep local-only semantics. This is
   scoped to `ITextPointer.GetValue`, so direct `element.GetValue(...)` reads
   (snapshot `firstRunFontWeight`, rendering) are unchanged.

Notes / decisions:

- The inheritance simulation lives in the upstream `TextPointer.cs` behind
  `#if HAS_UNO`, next to the existing Uno bridge for `Language`/`FlowDirection`
  defaults. It deliberately does not touch the shim property system, so all
  non-`ITextPointer` consumers keep the documented Default/Local behavior.
- The RTF chain for the mixed doc is now:
  `<Bold>` → tree `Bold(Run)` → XAML `<Span FontWeight="Bold">` → RTF `{\b ...}`,
  with no `\b0` override for the inner Run.
- Two reader probes were used to isolate the flattening before the fix:
  `parse-xaml-inline-tree` (through the shim `XamlReader`) vs
  `construct-inline-tree` (direct tree construction); the former dropped
  following inlines, the latter preserved all wrappers. The temporary probes
  in `MainPage.cs` and the `Debug_DumpRtfTree` test were removed once the fixes
  landed.

Tests:

- Integration (7 new): mixed Bold/Italic/Underline, nested Bold-inside-Italic,
  multiple paragraphs, unicode, hyperlink, list, table cell — each asserting
  the round-tripped document text/formatting via the `InlineTree` encoding
  (`w=700`, `s=Italic`, `d=U`) and the `Xaml`/`SetAndRtfRoundTrip` helpers.
- Integration (existing): `SaveLoad_Rtf_RoundTripsTextAndFormatting`.

Files modified:

- `ext/wpf/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Documents/TextPointer.cs` (inheritance walk)
- `src/LeXtudio.Windows/System.Windows/Markup/XamlReader.cs` (reader-position fix, local-formatting setters)
- `tests/RichTextBox.IntegrationTestHost/MainPage.cs` (debug probes)
- `tests/RichTextBox.IntegrationTests/RichTextBoxIntegrationTests.cs` (7 new tests)
- `docs/richtextbox/index.md` (counts)

Result:

- 199/199 RichTextBox integration tests pass (7 new; the temporary
  `Debug_DumpRtfTree` debug test was removed once the fixes landed).
- 234/234 model tests pass (`LeXtudio.Windows.Tests`).
- RTF save/load now round-trips mixed, nested, and structural formatting.
