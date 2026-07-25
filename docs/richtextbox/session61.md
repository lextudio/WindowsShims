### Session 61 - XAML Serialization (DataFormats.Xaml)

Status: completed.

Scope:

- `TextRangeBase.Save`/`Load` only recognizes `DataFormats.Text`. `Xaml`,
  `Rtf`, and `XamlPackage` throw `ArgumentException` (session 34). Enabling
  `DataFormats.Xaml` would unlock formatted clipboard copy/paste and document
  persistence with formatting (bold, italic, colors, font size).
- The WPF XAML serializer (`XamlSerializer` in `TextRangeSerialization`)
  depends on `XamlReader`/`XamlWriter` from `System.Xaml`. Under Uno/desktop,
  these may be partially available — investigate what works and what doesn't.
- If full `XamlReader`/`XamlWriter` is not available on the target platform,
  implement a local shim serializer that produces XAML for the subset of
  document structures the shim supports (Paragraph, Run, Bold, Italic,
  Underline, Span, Hyperlink, List, Table).

Implementation:

- Remove or modify the `#if HAS_UNO` guard in `TextRangeBase.cs` that gates
  `CanSave`/`CanLoad`/`Save`/`Load` for `DataFormats.Xaml`.
- Try the existing WPF `XamlSerializer` path. If it works for simple
  documents, this session is test coverage only.
- If the WPF path crashes, write a minimal `RichTextXamlSerializer` in
  `LeXtudio.Windows` that handles the known document model types and
  registers it in `TextRangeSerialization`'s format table.
- Tests verify round-trip: create a formatted paragraph → save to XAML →
  load into a new document → verify formatting and text match.

Tests:

- `XamlSaveLoad_RoundTripsPlainText`: save/load plain text via XAML.
- `XamlSaveLoad_RoundTripsBoldItalicUnderline`: save/load formatted text.
- `XamlSaveLoad_RoundTripsHyperlink`: save/load document with hyperlink.
- `XamlSaveLoad_InvalidDocument_ThrowsPredictably`: malformed XAML, verify
  exception.

Files modified:

- `src/LeXtudio.Windows/.../Documents/TextRangeBase.cs` — remove
  `#if HAS_UNO` guard for `DataFormats.Xaml`.
- Possibly new `XamlSerializer.uno.cs` if WPF path doesn't work.
- `tests/RichTextBox.IntegrationTests/RichTextBoxIntegrationTests.cs` — new
  tests.

Regression sweep:

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
dotnet test tests/RichTextBox.IntegrationTests/
```

Next session:

- Formatted clipboard copy/paste round-trip.
