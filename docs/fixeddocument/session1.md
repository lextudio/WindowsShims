### Session 1 - Paginator / Content-Type Spine

Status: completed.

Scope:

- Bring the fixed-document (XPS) family into the port. The family was excluded
  wholesale by four `<Compile Remove>` globs in `LeXtudio.Windows.csproj`
  (`Fixed*.cs`, `PageContent*.cs`, `DocumentSequence*.cs`,
  `XPSS0ValidatingLoader.cs`) even though every file was already listed as a
  `<Compile Include>` above them.

Findings:

- Un-gating the family produced **602 compile errors across 44 files**. The
  distribution, after the first two rounds of support work:

  | Bucket | Errors | Nature |
  | --- | ---: | --- |
  | `DocumentsTrace` missing | 68 | one file, mechanical |
  | `GlyphRun` members | ~60 | no WinUI equivalent — real blocker |
  | `Image`/`Path` ambiguity | 39 | per-file using-aliases |
  | `Rect` helpers and 1-arg ctor | ~40 | shared value-type gap |
  | `SR` string constants | ~50 | mechanical |
  | `Transform.Value`/`Identity`, `MatrixTransform` | ~22 | shared shim gap |
  | `Brushes` named colors | ~20 | mechanical |
  | `FormattedText` 7-arg ctor, `BuildHighlightGeometry` | ~18 | text layer |
  | app-model (`MimeTypeMapper`, `WpfWebRequestHelper`, `XamlReader.LoadBaml`) | ~15 | needs shims |
  | element gaps (`VerifyAccess`, `InheritanceBehavior`, `IsInitialized`, `BringIntoView`) | ~15 | shim gap |

  The full family is therefore a multi-rung port, not a single change. The rung
  plan is in `index.md`.

- The build error count is not monotonic while iterating: Uno's source
  generators run before the C# compile, so an early failure hides the real
  error set. Two intermediate rounds reported 1-2 errors that turned into 602
  once those cleared.

- `System.Windows.ContentPosition` existed as an empty abstract stub in
  `EarlyBatchEditorShims.cs` — the wrong namespace (WPF's is
  `System.Windows.Documents.ContentPosition`). `TextPointer` and `FlowDocument`
  were binding to it through enclosing-namespace lookup. Linking the real one
  and deleting the stub re-points `TextPointer`'s base class at upstream, with
  no test movement.

- `MS.Internal.ContentType` reported CS0122 rather than CS0246: the type is
  present in a referenced assembly but internal to it. Linking WindowsBase's
  copy makes the accessible one win.

- `MS.Internal.Utility.BindUriHelper` is gated `#if PRESENTATIONFRAMEWORK`
  upstream. The guard was widened to `PRESENTATIONFRAMEWORK || HAS_UNO`, but the
  file still needs `INavigator`, `MimeTypeMapper` and `BaseUriHelper.PackAppBaseUri`,
  so it stays out of the compile until R5.

Changes:

- `LeXtudio.Windows.csproj`:
  - Links the upstream paginator spine from PresentationCore: `DocumentPage`,
    `DocumentPaginator`, `DynamicDocumentPaginator`, `ContentPosition`,
    `GetPageCompletedEventArgs`, `GetPageNumberCompletedEventArgs`,
    `PaginationProgressEventArgs`, `PagesChangedEventArgs`,
    `IDocumentPaginatorSource`.
  - Links `WindowsBase/MS/Internal/ContentType.cs`,
    `Shared/System/Windows/Markup/XmlWrappingReader.cs`,
    `PresentationCore/MS/internal/IO/Packaging/PreloadedPackages.cs`,
    `WindowsBase/MS/Internal/IO/Packaging/PackagingExtensions.cs`,
    `PresentationFramework/System/Windows/Navigation/FragmentNavigationEventArgs.cs`.
  - Replaces the four blanket removes with one documented block naming every
    still-gated family and the reason.

- Deleted `System.Windows/Documents/DocumentPaginatorShims.cs` (the local
  paginator stubs) and the `System.Windows.ContentPosition` stub.

- New local files, each carrying a comment saying why it is not a link:
  - `System.Windows/Navigation/NavigationService.cs` — attached
    `NavigationServiceProperty`, `FragmentNavigation`, `Content`, `CurrentSource`.
  - `System.Windows/Documents/FixedDocumentSupport.cs` — no-op `MS.Utility.EventTrace`.

- `MS.Internal/Florence/FlorenceEngine.cs`: `GetPage` now uses the four-argument
  `DocumentPage` constructor with an explicit page rect for bleed and content box.

- `ext/wpf` patches (all `#if !HAS_UNO` with a `// HAS_UNO:` reason, inert until
  the object model is un-gated):
  - `FixedPage.cs` — derives from the WPF `Panel` shim under HAS_UNO so children
    live in a real WinUI panel; `OnRender`, `LogicalChildren`, `VisualChildrenCount`,
    `GetVisualChild`, `OnVisualParentChanged`, `OnPreviewMouseWheel`, the private
    `Children`/`CreateUIElementCollection` pair and `DebugVisualAdorner.OnRender`
    are guarded.
  - `FixedElement.cs` — `partial` (Uno's DependencyObject generator emits one).
  - `FixedSOMPageConstructor.cs` — the `GeometryWalker`/`StreamGeometry` fast path
    is guarded; the existing `PathGeometry` fallback covers the same line detection.
  - `RubberbandSelector.cs` — the bitmap-snapshot path is guarded; text still
    reaches the clipboard.
  - `Glyphs.cs`, `PageContent.cs` — `OnRender` / `LogicalChildren` guarded.
  - `DocumentPage.cs` — `VisualTreeHelper.GetContentBounds` falls back to
    `DesiredSize`; `new Rect(Size)` becomes `new Rect(new Point(), Size)`.
  - `DocumentPaginator.cs`, `DynamicDocumentPaginator.cs`, `PreloadedPackages.cs` —
    `using MS.Internal.PresentationCore` guarded (SR is root-namespace here).
  - `FixedDocument.cs` — `using MS.Internal.Annotations.Component` guarded.
  - Per-file `using` aliases for `Matrix`, `Image`, `Path`, `Package`.
  - `BindUriHelper.cs` — `#if PRESENTATIONFRAMEWORK || HAS_UNO`.

- `SR.cs`: 11 new constants for `ContentType` and `DocumentPaginator`.

Tests:

- New `src/LeXtudio.Windows.Tests/FixedDocumentSpineTests.cs` (12 tests):
  `DocumentPage.Missing` semantics, bleed/content box defaulting to the page rect
  and being preserved when explicit, `DocumentPaginator` negative-page-number
  rejection, `GetPageCompleted`/`GetPageNumberCompleted` event plumbing,
  `DynamicDocumentPaginator` rejecting `ContentPosition.Missing`, and
  `ContentType` parsing/comparison.
- The `ContentType` comparison test initially asserted that parameters are
  ignored. Upstream returns false when either side carries a parameter unless
  the caller passes `allowParameterValuePairs: true`; the test now pins both.

Result:

- `LeXtudio.Windows` builds clean on `net10.0-desktop`.
- 247/247 model tests, 238/238 RichTextBox integration, 83/83 DataGrid integration.
- The `net10.0-windows10.0.19041.0` target was not built (macOS host).

Next: R2 (shared `Rect`/`Transform`/`Brushes` helpers) and R3 (`DocumentsTrace`),
which together clear roughly half the remaining error count without touching the
GlyphRun question.
