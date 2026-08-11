### Session 97 - Visual Rendering for Embedded Images

Status: completed.

Scope:

- Session 96 made embedded images round-trip through RTF (`\pict`) at the
  document-model level, but noted the shim `Image` was a plain
  `FrameworkElement` with no visual. This session makes images actually
  render in `FlowDocumentView`.

Findings:

- **The rendering path already parented the child into a Canvas** — the
  Florence line visual builder places `run.EmbeddedElement` into the line
  `Canvas` and positions it at `run.X` (session 67/75 infrastructure). The
  problem was the shim `Image` itself: a bare `FrameworkElement` with no
  visual.
- **`WriteableBitmap` is unavailable on this Uno Skia build** — the Uno.UI
  assembly reports
  `Is_Microsoft_UI_Xaml_Media_Imaging_WriteableBitmap_Available = false`, so
  that route is out.
- **WinUI `BitmapImage` from a memory stream works.** A feasibility probe
  (non-blocking — blocking on `SetSourceAsync` on the dispatcher thread
  deadlocks the DevFlow host) confirmed `ImageOpened` fires and the image
  measures with the correct aspect ratio.
- The shim `Image` now derives from `Microsoft.UI.Xaml.Controls.Image` and,
  when its `Source` (a shim `BitmapSource`) is set, re-encodes the pixels to
  PNG and feeds them into a WinUI `BitmapImage` (`DataWriter` +
  `SetSourceAsync`). The hidden `Source` is typed as the base `ImageSource`
  (like WPF) so upstream casts (e.g. `WpfPayload.GetBitmapSourceFromImage`'s
  `(DrawingImage)image.Source`) keep compiling.

Changes:

- `Image.cs`: shim `Image` now derives from the WinUI `Image` control;
  `Source`/`Width`/`Height` hide the base members, and `Source` setter
  synchronizes a WinUI `BitmapImage` for rendering.
- `MainPage.cs`: snapshot gained `firstInlineImageRendered` (whether the
  first inline image's WinUI `Image.Source` DP holds a `BitmapImage`).

Tests:

- Integration (no new test; extended):
  - `SaveLoad_Rtf_RoundTripsInlineImage` now also asserts
    `firstInlineImageRendered` — after the RTF round-trip the reloaded image
    has a renderable WinUI source wired up. 232/232 total.
- Model tests: 234/234 (`LeXtudio.Windows.Tests`).

Result:

- 232/232 RichTextBox integration tests pass; 234/234 model tests pass.
- Embedded images now render in `FlowDocumentView` (the WinUI `Image` control
  displays the decoded `\pict` pixels).
