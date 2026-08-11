### Session 105 - XAML/XamlPackage Image Round-Trip Fix

Status: completed.

Scope:

- The session 101/102 data-URI image embedding (`WriteEmbeddedObject`'s HAS_UNO
  branch) was only tested through RTF. This session pins the Xaml and
  XamlPackage round trips with images.

Findings:

- `DataFormats.Xaml` save works (data-URI `<Image>` emitted) and the load
  parses it — but with `preserveTextElements=false` (the round-trip's save
  call) the serializer collapses an `InlineUIContainer` into
  `<Run><Image .../></Run>`, and the shim's `ParseRun` ignored the nested
  `<Image>` child, leaving an empty Run. The XamlPackage round trip worked
  because its package XAML part is parsed by the same `Parse` paths but the
  reduced `<Run><Image/></Run>` shape was never produced there.
- The `DataFormats.Xaml` load path is `TextRange.Xml` setter →
  `XamlReader.Load(XmlTextReader, bool)` → `Parse`; the XamlPackage path is
  `WpfPayload.LoadElement` → `XamlReader.Load(stream, ParserContext, bool)` →
  ZIP-magic detection → `LoadFromPackage` → `Load(string)` → `Parse`. Both
  end in the same parser.

Changes:

- `XamlReader.cs` (`ParseRun`): when a `<Run>` element contains a nested
  `<Image>` element (the serializer's reduced-container shape), reconstruct an
  `InlineUIContainer` carrying the Run's formatting properties. `ParseRun`
  now returns `Inline`; its only caller (`ParseInline` case "Run") accepts it.

Tests:

- Integration (2 new, 238/238 total):
  - `SaveLoad_Xaml_RoundTripsInlineImage` — Xaml format round-trips an
    inline image (40x20 data-URI PNG), preserving dims and the rendered
    WinUI source.
  - `SaveLoad_XamlPackage_RoundTripsInlineImage` — XamlPackage (OPC)
    round-trips the same image through its XAML part.
- Model tests: 234/234 (`LeXtudio.Windows.Tests`).

Result:

- 238/238 RichTextBox integration tests pass; 234/234 model tests pass.
- Both Xaml and XamlPackage now round-trip embedded images.
