### Session 96 - RTF Round-Trip for Embedded Images (\pict)

Status: completed.

Scope:

- Sessions 81-95 completed RTF round-trips for text, inline/paragraph
  formatting, lists, hyperlinks, tables, super/subscript, language, borders,
  line breaks, and tabs — leaving embedded images as the last unround-tripped
  RTF feature (`WriteEmbeddedObject` was `#if !HAS_UNO`, so images became a
  space in the intermediate XAML). This session makes images
  (`InlineUIContainer`/`BlockUIContainer` + `Image`) survive RTF save/load.

Findings:

- **The upstream writer and reader already knew how to emit and parse
  `\pict`** — `WriteShapeImage` writes `{\*\shppict{\pict\picwgoal...\pngblip`
  + hex data, and `ProcessImage` parses it back. The blockers were all on the
  shim/XAML boundaries:
  1. `WriteEmbeddedObject` was compiled out under `HAS_UNO` (images became a
     space), and the shim's `InlineUIContainer.Child` is stored outside the
     `TextContainer` (WPF's `InsertUIElement` isn't wired up), so the
     `TextPointer` walk never emitted the embedded element at all.
  2. `BitmapSource` didn't retain pixel data (`GetPixelData` returned zeros),
     and `BitmapFrame.Create(Stream)` returned an empty frame, so the RTF
     writer had no natural image size.
  3. The shim `XamlReader` had no `InlineUIContainer`/`BlockUIContainer`/
     `Image` parsing, and the shim `Image` was a stub.
- **Design: self-contained data-URI sources under HAS_UNO.** WPF's image path
  threads an OPC package (`WpfPayload`) through XAML and the RTF converter,
  but the shim's `XamlReader` is a pure string parser with no package
  concept. Instead, the intermediate XAML under `HAS_UNO` carries
  `Source="data:image/png;base64,..."`:
  - Save: `WriteEmbeddedObject` encodes the `BitmapSource` pixels to PNG
    (SkiaSharp) and emits `<Image Width Height Source="data:...">`; the RTF
    writer's `WriteShapeImageFromDataUri` decodes it into `\pict\pngblip` +
    hex. `WriteStartXamlElement` emits the image child explicitly (since it
    is not in the `TextContainer`), keyed on the original element type so it
    works with the RTF save's `reduceElement=true` path.
  - Load: `ProcessImage` under `HAS_UNO` captures the `\pict` bytes and
    re-emits `<Image Width Height Stretch Source="data:...">`; the shim
    `XamlReader` decodes the data URI into a `BitmapSource` (via
    `BitmapSource.Decode`, SkiaSharp) and wraps it in an
    `InlineUIContainer`/`BlockUIContainer`.
- `BitmapFrame.Create(Stream)` now decodes real dimensions (SkiaSharp) so
  `\picwgoal`/`\pichgoal` come out correct, and `BitmapSource.Create` retains
  the pixel array (both were needed by the encode/decode round-trip).
- Note: RTF has no block-level image concept, so a `BlockUIContainer` image
  reloads as an inline image (WPF-faithful). Visual rendering of the image in
  `FlowDocumentView` is covered in session 97.

Changes:

- `ext/wpf` submodule:
  - `TextRangeSerialization.cs`: HAS_UNO branch of `WriteEmbeddedObject`
    emits a data-URI `<Image>`; `WriteStartXamlElement` serializes an `Image`
    child of `InlineUIContainer`/`BlockUIContainer` explicitly.
  - `XamlToRtfWriter.cs`: `WriteImage` handles `data:image/...` sources via
    `WriteShapeImageFromDataUri` (no package needed).
  - `RtfToXamlReader.cs`: `ProcessImage` under `HAS_UNO` captures the `\pict`
    bytes and emits a self-contained data-URI `<Image>`.
- `ImagingShims.cs`: `BitmapSource.Create` retains the pixel array;
  `GetPixelData` returns it; new `BitmapSource.Decode(byte[])` (SkiaSharp).
- `ExtraImagingShims.cs`: `BitmapFrame.Create(Stream)` decodes real
  dimensions.
- `Image.cs`: shim `Image` now holds a real `Source` (`BitmapSource`) and
  `Width`/`Height`.
- `XamlReader.cs`: `InlineUIContainer`/`BlockUIContainer`/`Image` parsing
  (data-URI decode), plus bare-`<Image>` cases (the RTF reader emits a bare
  `<Image>` element) wrapped in the appropriate container.
- `MainPage.cs`: snapshot gained `firstInlineImageDims` and
  `firstBlockImageDims` (first image's `PixelWidth x PixelHeight`).

Tests:

- Integration (2 new, 232/232 total):
  - `SaveLoad_Rtf_RoundTripsInlineImage` — a 40x20 PNG in an
    `InlineUIContainer` reloads as an image with the same pixel dimensions.
  - `SaveLoad_Rtf_RoundTripsBlockImage` — a `BlockUIContainer` image reloads
    with the same dimensions (as an inline image, WPF-faithfully).
- Model tests: 234/234 (`LeXtudio.Windows.Tests`).

Result:

- 232/232 RichTextBox integration tests pass; 234/234 model tests pass.
- Embedded images now round-trip through RTF save/load as `\pict\pngblip`/
  `\jpegblip`; the RTF round-trip surface is now complete.
