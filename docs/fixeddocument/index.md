# FixedDocument (XPS) Port

**Status: migrated.** `FixedDocument`, `FixedPage`, `PageContent`,
`FixedDocumentSequence`, `DocumentReference`, the `FixedSOM*` semantic object
model, `Glyphs`, `FixedTextView`/`FixedTextBuilder`, and the paginator/
app-model support layers are linked from upstream WPF and compiled into
`LeXtudio.Windows` by default — no build property needed. See "Where the
migration stands" at the end of `session6.md` for what remains out of scope
(tracked as R8: `RubberbandSelector`, `HighlightVisual`, full visual-fidelity
rendering) and why.

This folder tracks the `System.Windows.Documents` fixed-document family —
`FixedDocument`, `FixedPage`, `PageContent`, `FixedDocumentSequence`, the
`FixedSOM*` semantic object model, and the paginator surface they sit on.

The rest of the repo's conventions apply: `docs/PLAN.md` is the original
source-first playbook, and `src/LeXtudio.Windows/LeXtudio.Windows.csproj` is the
source of truth for what is actually compiled.

## Why this family is hard

Every other ported control (ToolBar, RichTextBox, DataGrid) rebases WPF logic
onto WinUI's layout model. The fixed-document family additionally depends on
WPF's **retained-mode drawing layer**, which WinUI does not expose at all:

- `GlyphRun` and `GlyphTypeface` — solved in rung 5 by reimplementing over
  SkiaSharp rather than linking. Two known gaps remain: `AdvanceHeights` reports
  the line height for every glyph because Skia exposes no `vmtx` table, and
  `FamilyNames` answers one name for every culture.
- `DrawingContext` / `OnRender` / `DrawingVisual` / `ContainerVisual` /
  `StreamGeometry` / `CapacityStreamGeometryContext`.
- `FormattedText`'s 7-argument constructor and `BuildHighlightGeometry`.

Nothing in the fixed-document object model renders without those, so the port
is staged: the paginator/content-type spine first, the object model second, the
text/visual layer last.

## Current State

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
Build succeeded: 0 errors
```

Tests: 318/318 model (`LeXtudio.Windows.Tests`), 240/240 RichTextBox
integration (was 238; +2 for fixed-document object-model probes), 83/83
DataGrid integration.

The object model is included by default. `RubberbandSelector.cs` (R8) is the
one file still gated; un-gate it for measurement without editing the project
file:

```bash
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj \
  -f net10.0-desktop -p:FixedDocumentR8=all
```

R8 error count: **21**, entirely within `RubberbandSelector.cs`. The
whole-family count was 602 at session 1, 585 at the start of session 2, and 0
in the default build as of session 6.

### Landed (rungs 1-7; R6 completes the migration)

Linked from upstream, replacing hand-written local shims:

| Upstream file | Was |
| --- | --- |
| `PresentationCore/System/Windows/Documents/DocumentPage.cs` | local stub with a null `Visual` |
| `PresentationCore/System/Windows/Documents/DocumentPaginator.cs` | local stub, no page-number validation |
| `PresentationCore/System/Windows/Documents/DynamicDocumentPaginator.cs` | absent |
| `PresentationCore/System/Windows/Documents/ContentPosition.cs` | empty stub in the wrong namespace (`System.Windows`) |
| `PresentationCore/System/Windows/Documents/GetPageCompletedEventArgs.cs` | local stub |
| `PresentationCore/System/Windows/Documents/GetPageNumberCompletedEventArgs.cs` | absent |
| `PresentationCore/System/Windows/Documents/PaginationProgressEventArgs.cs` | absent |
| `PresentationCore/System/Windows/Documents/PagesChangedEventArgs.cs` | local stub |
| `PresentationCore/System/Windows/Documents/IDocumentPaginatorSource.cs` | local stub |
| `WindowsBase/MS/Internal/ContentType.cs` | inaccessible (internal to another assembly) |
| `Shared/System/Windows/Markup/XmlWrappingReader.cs` | inaccessible |
| `PresentationCore/MS/internal/IO/Packaging/PreloadedPackages.cs` | absent |
| `WindowsBase/MS/Internal/IO/Packaging/PackagingExtensions.cs` | absent |
| `PresentationFramework/System/Windows/Navigation/FragmentNavigationEventArgs.cs` | absent |

Local files that remain, with the reason each cannot be linked:

- `System.Windows/Navigation/NavigationService.cs` — upstream is 4400+ lines of
  app-model code (journal, `PageFunction`, WebBrowser hosting). Only the
  attached `NavigationServiceProperty`, the `FragmentNavigation` event and
  `Content`/`CurrentSource` are provided.
- `System.Windows/Documents/FixedDocumentSupport.cs` — a no-op `MS.Utility.EventTrace`.
  Upstream tracing is ETW/Win32 interop (`Shared/MS/Utility/TraceProvider.cs`).

`TextPointer` now derives from the real `System.Windows.Documents.ContentPosition`
rather than an empty same-named stub in `System.Windows`.

Rungs 2 and 3 added the shared value-type surface — `RectExtensions` and
`WinUITransformExtensions` in `System.Windows/Media/RectAndTransformExtensions.cs`,
`Matrix.IsIdentity`/`operator *`, `DpiScale.PixelsPerDip`,
`LogicalTreeHelper.GetChildren`/`FindLogicalNode`, ten named `Brushes`, and a
one-member `System.Windows.Markup.TypeConverterHelper` — plus linked
`DocumentsTrace` and `ReflectionUtils`. Details in `session2.md`.

Rung 4 added the element surface — `BringIntoView`, `GetDpi`, `IsInitialized`,
`InheritanceBehavior`, `AutomationProperties.Name`/`HelpText`,
`Rect.Contains(Rect)`, and an implicit `string` to `XmlLanguage` conversion —
along with the 50 missing `SR` constants (carrying their real resx text) and the
`Image`/`Path` using-aliases. Details in `session3.md`.

Rung 5 brought up the app-model loading layer. `MS/Internal/Utility/BindUriHelper.cs`
and its `Shared/MS/Utility` half are now linked and compile in the normal build —
the first fixed-document support file off the gate list. `BaseUriHelper`,
`MimeTypeMapper`, `WpfWebRequestHelper` and one `XamlReaderHelper` constant are
local, in `System.Windows/Navigation/AppModelShims.cs`, because upstream drags in
the pack:// deployment stack. Details in `session4.md`.

Rung 7 replaced the `GlyphTypeface` and `GlyphRun` stubs with real implementations
in `System.Windows/Media/`. `GlyphTypeface` is backed by SkiaSharp; `GlyphRun`'s
caret arithmetic is ported from upstream line for line. Guarding WPF's
`DrawDebugVisual` code behind `#if DEBUG && !HAS_UNO` cleared a third of what
looked like a GlyphRun problem before any shim was written. Details in `session5.md`.

Rung 6 cleared the remaining ~72-error tail and un-gated the whole object model
in the default build — the gating flag inverted from "add the object model on
top of a gated default" to "add back only `RubberbandSelector.cs` (now `R8`)
on top of an un-gated default". Verified with two new integration tests that
build a `FixedDocument` in memory and paginate it through a real app host, not
just by compiling. Details in `session6.md`.

### Gated off

The whole object model is still excluded in the project file, with the block
listing each family: `Fixed*.cs`, `PageContent*.cs`, `DocumentSequence*.cs`,
`DocumentReference*.cs`, `ChildDocumentBlock.cs`, `Glyphs.cs`,
`HighlightVisual.cs`, `RubberbandSelector.cs`, `XPSS0ValidatingLoader.cs`,
`FlowNode.cs`, `FlowPosition.cs`, `DocumentStructures/*`, plus
`MS/Internal/documents/{FixedDocumentPaginator,FixedDocumentSequencePaginator,TextViewBase}.cs`
and `MS/Internal/documents/TextViewBase.cs`.

Turning the whole family on at once was measured: **602 compile errors across
44 files** at session 1, **389** after session 2. The taxonomy is in `session1.md`
and `session2.md`.

## Rung Plan

Each rung must leave the library building and all three suites green.

- **R1 — paginator/content-type spine.** Done, session 1.
- **R2 — shared value-type helpers.** `Rect.Offset/Inflate/TopLeft/TopRight/IntersectsWith`
  and the one-argument constructor, `Transform.Value/Identity`, `MatrixTransform(Matrix)`,
  `GeneralTransform.AffineTransform`, `PathGeometry.CreateFromGeometry`, the
  missing `Brushes` colors, `LogicalTreeHelper.GetChildren`,
  `System.Windows.Markup.TypeConverterHelper`, `DpiScale.PixelsPerDip`.
  Done, session 2. These are repo-wide gaps, not fixed-document specific.
- **R3 — `MS.Internal.Documents.DocumentsTrace`.** Done, session 2: linked
  upstream along with `ReflectionUtils`.
- **R4 — element-level gaps.** Done, session 3, along with the `Image`/`Path`
  aliases and the `SR` constants. Still open in this area and folded into R6:
  `IScrollInfo` and `ScrollContentPresenter.ComputeScrollOffsetWithMinimalScroll`
  in `TextViewBase`, `TransformToAncestor` returning WinUI's `GeneralTransform`
  where the shim type is expected, and `IFrameworkInputElement`.
- **R5 — app-model loading.** Done, session 4.
- **R6 — the object model.** Done, session 6. `FixedDocument`, `PageContent`,
  `DocumentSequence`, `DocumentReference`, `FixedTextContainer`,
  `FixedTextPointer` and everything else are un-gated in the default build.
  Verified live (pagination, page identity, hyperlink plumbing) via a RichTextBox
  integration-host probe, not compile-only as originally scoped — rendering
  fidelity is still R8's job, not R6's.
- **R7 — GlyphRun surface.** Done, session 5, over SkiaSharp. Taken before R6
  because it was the gate on everything else. Either a Uno/Skia-backed
  `GlyphRun` shim or a rewrite of `Glyphs`/`FixedSOMTextRun` onto Uno's text
  stack. Decide before starting R6, because `FixedTextView` straddles both.
- **R8 — rendering and hit-testing (remaining scope).** `RubberbandSelector`
  (rubber-band selection, its bitmap-clipboard snapshot, and its `DocumentGrid`
  dependency), `HighlightVisual` (the `DrawingVisual`-based highlight layer),
  `FixedPage.OnRender`, the `GeometryWalker` fast path, and full pixel-fidelity
  rendering of fixed-page content. All need the retained-mode visual layer
  (`ContainerVisual`/`DrawingVisual`) that WinUI does not expose.

## Working Rules

1. Keep `LeXtudio.Windows` building after every rung.
2. Link upstream `.cs` files; do not hand-port. Write a local file only when the
   upstream one drags in an app-model or Win32 subsystem, and say so in a comment.
3. Guard what cannot compile with `#if !HAS_UNO` in `ext/wpf`, with a
   `// HAS_UNO:` note giving the reason.
4. Delete the local shim whenever its upstream replacement lands.
5. Run all three suites before calling a rung done; the integration suites share
   an app instance per collection, so a single-test run is not proof.
