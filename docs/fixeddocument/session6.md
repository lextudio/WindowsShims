### Session 6 - The Object Model (R6)

Status: completed. FixedDocument (XPS) migration essentially done — see the summary
at the end of this file.

Scope:

- R6: clear the remaining compile tail from session 5 (72 errors, excluding
  `RubberbandSelector.cs`) and un-gate the whole fixed-document object model —
  `FixedDocument`, `FixedPage`, `PageContent`, `FixedDocumentSequence`,
  `DocumentReference`, `FixedTextContainer`, `FixedTextPointer`, `Glyphs`,
  the `FixedSOM*` family, `XPSS0ValidatingLoader` — in the default build.

Findings:

- Measured surface: **93 errors at the start of this session, 0 in the default
  build by the end.** The tail cleared in roughly a dozen small batches, each
  re-measured with `-p:FixedDocumentRung=all` before moving to the next:
  `GetCompatibleCulture` (XmlLanguage culture fallback), `BitmapImage.UriSource`/
  `ImageSource.Width/Height`, `PathGeometry.CreateFromGeometry`/`AddGeometry`,
  `Geometry.Combine`/`GetArea`, element-level members (`Initialized`,
  `InheritanceBehavior`, `VerifyAccess`, `ChangeLogicalParent`,
  `VisualContentBounds`, `InvalidateVisual`, `InheritanceParent`,
  `DependencyObjectType`), `DocumentAutomationPeer`/`FixedPageAutomationPeer`,
  the annotation-highlight and `HighlightVisual` call sites in `FixedDocument`,
  `IFrameworkInputElement`, the `DependencyObject()` instantiation in
  `FixedTextPointer`, `DispatcherOperation`/`Dispatcher.BeginInvoke` returning a
  real awaitable, `XmlCompatibilityReader` (linked, +24 `SR` constants),
  `TransformToAncestor`/`TransformToVisual` type mismatches, `IInputElement` on
  `FixedPage`, `IScrollInfo` (linked) and `ScrollContentPresenter.
  ComputeScrollOffsetWithMinimalScroll` (ported), and finally
  `XamlReader.Load`'s five-argument overload plus `XamlParseMode`.

- **The gating flag changed shape.** Un-gating incrementally with
  `-p:FixedDocumentRung=all` throughout the session, then flipping the default
  once the tail cleared, meant the project file's structure inverted: instead of
  a rung flag that adds the object model on top of a gated default, the object
  model is now the default and a new `FixedDocumentR8` flag adds back only
  `RubberbandSelector.cs` (the one file R8 still covers). This reads better going
  forward — the default build is the real one, not a stripped-down one.

- **`RubberbandSelector.cs`'s scope grew once fully measured.** Session 1 gated
  it for one reason (its bitmap-snapshot rendering needs `ContainerVisual`/
  `DrawingVisual`, guarded in that session). Un-gating everything else in this
  session exposed the rest of its surface: `CommandBindings`, mouse routed
  events (`MouseLeftButtonDown/Up`, `MouseMove`, `QueryCursor`) on
  `FrameworkElement`/`UIElement`, `Cursor`, and `DocumentGrid` (a WPF FlowDocument
  paginated-viewer control this port does not carry). R8 now covers all of it,
  not just the rendering path.

- Two field/property declarations of type `RubberbandSelector` in `FixedDocument`
  and `FixedDocumentSequence` (a lazily-constructed selection helper) needed
  something to bind to even with the real file gated. A one-line stub
  (`internal sealed class RubberbandSelector { }`) fills that gap and is itself
  removed once `-p:FixedDocumentR8=all` brings in the real type.

- `XamlReader.Load`'s full five-argument overload (parser context, parse mode,
  restrictive-reader flag, safe-type allowlist) is WPF's XAML security
  infrastructure — BAML permission sets and async parsing that this port does
  not model. `XPSS0ValidatingLoader` is its only caller; the arguments are
  accepted and ignored, and the reader is parsed the same way the existing
  `Load(reader)` overload already does.

- `ComputeScrollOffsetWithMinimalScroll` looked like a two-line port at first
  glance and turned into three: the ambiguity between the real (linked)
  `System.Windows.Controls.Primitives.IScrollInfo` and a same-named local shim
  wrongly declared in `System.Windows.Controls` had to be resolved first — the
  local one was deleted, since nothing in the port actually implements it
  (`TextEditorSelection`'s only consumer, `TextBoxView`, is not linked, so the
  cast is dead code either way, same as it was before).

- **Model tests cannot construct `FixedPage`/`FixedDocument`.** A first attempt
  wrote unit tests against real instances of these types directly. All ten
  failed with `NullReferenceException` from Uno's `DependencyProperty`/`Brush`
  static constructors, which call `DispatcherQueue.GetForCurrentThread()` —
  unavailable in the headless model-test process. This matches every other
  WinUI-rooted type in the port: none of the existing 318 model tests construct
  one. The object-model tests were moved to the RichTextBox integration host
  instead (see Tests below), which runs a real app with a live dispatcher.

Changes:

- `System.Windows/TextEditorSystemShims.cs`: `XmlLanguage.GetCompatibleCulture()`
  and a `string.GetCompatibleCulture()` extension (WinUI types `Language` as a
  string), both routing through upstream's shorten-and-retry algorithm.
- `System.Windows/Media/Imaging/ImagingShims.cs`: `BitmapImage(Uri)` constructor
  and `UriSource`.
- `System.Windows/Media/ImageSource.cs`: `Width`/`Height` extensions reading a
  `BitmapSource`'s pixel size.
- New `System.Windows/Media/GeometryExtensions.cs`: `PathGeometry.
  CreateFromGeometry`, `PathGeometry.AddGeometry`, `Geometry.Combine`,
  `Geometry.GetArea` — all bounding-box approximations (exact for the
  rectangular geometries the hyperlink-matching code actually produces).
- `System.Windows/FrameworkElement.cs`: `FrameworkContentElement.Initialized`
  event, `FocusableProperty`, `Language`, `AddLogicalChild`/`RemoveLogicalChild`.
- `System.Windows/FrameworkPropertyMetadata.cs`: `CoerceValueCallback` is now
  `init`, not get-only, so upstream's object-initializer construction compiles.
- `System.Windows/DependencyObjectType.cs`: rewritten to carry the wrapped CLR
  type and implement `IsSubclassOf`.
- `System.Windows/WinUIFrameworkElementExtensions.cs`: `BringIntoView()`/
  `BringIntoView(Rect)`, `VisualContentBounds`, `ChangeLogicalParent`,
  `InvalidateVisual`, `InheritanceParent`, `DependencyObjectType`; `IsInitialized`
  changed from `IsLoaded` to always-true (see rationale in the doc comment —
  `IsLoaded` would send the fixed-document page loader down a path that never
  completes).
- New `System.Windows/Media/VisualHitTesting.cs`: `VisualTreeHelper.HitTest`
  and `HitTestResult`, over WinUI's `FindElementsInHostCoordinates`.
- New `System.Windows/FrameworkElementInitialized.cs`: a `ConditionalWeakTable`-
  backed add/remove/raise surface for the `Initialized` event, since nothing in
  Uno's XAML loader raises it (see `IsInitialized`, above).
- New `System.Windows/Automation/Peers/DocumentAutomationPeers.cs`:
  `DocumentAutomationPeer`, `FixedPageAutomationPeer` — honest no-op stubs, like
  the rest of this port's UI Automation surface.
- `System.Windows/Threading/Dispatcher.cs` and `System.Windows/Dispatcher.cs`:
  `DispatcherOperation.Wait()`, `DispatcherOperationStatus`, and
  `Dispatcher.BeginInvoke` overloads that return a real awaitable/waitable
  operation instead of `void`.
- New `System.Windows/Markup/XamlReader.cs` overload (parser context/parse
  mode/safe-types, ignored) and a new `XamlParseMode` enum.
- `System.Windows/Controls/ContextMenuShims.cs`,
  `System.Windows/Controls/ScrollShims.cs`: the wrong-namespace local
  `IScrollInfo` deleted in favor of the linked upstream one.
- `System.Windows/Controls/PanelShims.cs`: `ScrollContentPresenter.
  ComputeScrollOffsetWithMinimalScroll`, ported from upstream.
- New `System.Windows/Documents/FixedPageInputElement.cs`: explicit
  `IInputElement` on `FixedPage`, forwarding to the existing
  `WinUIDependencyObjectExtensions` members.
- New `System.Windows/Documents/RubberbandSelectorStub.cs`: the one-line
  placeholder described above.
- Linked `IScrollInfo.cs` and `XmlCompatibilityReader.cs` from upstream (the
  latter needed a `HAS_UNO` arm in its assembly-selector `#if`, same pattern as
  `MimeTypeMapper`/`BindUriHelper` in earlier sessions); 24 more `SR` constants
  with real resx text.
- `ext/wpf`: roughly twenty small guarded patches across `FixedDocument.cs`,
  `DocumentSequence.cs`, `PageContent.cs`, `PageContentAsyncResult.cs`,
  `FixedTextBuilder.cs`, `FixedTextPointer.cs`, `FixedElement.cs`,
  `FixedSOMTextRun.cs`, `FixedSOMImage.cs`, `FixedSOMPageConstructor.cs`,
  `FixedPage.cs`, `XPSS0ValidatingLoader.cs` — each `#if HAS_UNO`/`#if !HAS_UNO`
  with a one-line reason, all listed above under Findings.
- `LeXtudio.Windows.csproj`: the gating restructure described above.

Tests:

- New `src/LeXtudio.Windows.Tests/AppModelShimTests.cs`… (carried from session 4).
- Two new integration tests in `RichTextBoxIntegrationTests.cs`, backed by a new
  `fixeddocument.probe.build-and-paginate` DevFlow action in
  `RichTextBox.IntegrationTestHost/MainPage.cs`:
  `FixedDocument_WithThreePages_PaginatesToTheRealFixedPageInstances` and
  `FixedDocument_WithOnePage_PaginatesCorrectly`. Each builds a `FixedDocument`
  entirely in memory (`PageContent.Child`, not a `Source` uri), drives the real
  `DynamicDocumentPaginator` returned by
  `IDocumentPaginatorSource.DocumentPaginator`, and asserts: page count and
  `IsPageCountValid`; each `DocumentPage.Visual` is reference-equal to the
  actual `FixedPage` instance (not a copy or wrapper); each page's WinUI
  `Panel.Children` collection is the live one (a child added before pagination
  is still there after); a page past the end returns `DocumentPage.Missing`;
  and a negative page number throws `ArgumentOutOfRangeException`.
- This required reusing the RichTextBox app host rather than the model-test
  project, per the Findings note above.

Result:

- `LeXtudio.Windows` builds clean on `net10.0-desktop` — **with the whole
  fixed-document object model included by default**, no build property needed.
- 318/318 model tests, 240/240 RichTextBox integration (was 238; +2 for the
  fixed-document probes), 83/83 DataGrid integration.
- Remaining un-gated surface, measured with `-p:FixedDocumentR8=all`: 21 errors,
  entirely within `RubberbandSelector.cs`.

## Where the migration stands

FixedDocument, FixedPage, PageContent, FixedDocumentSequence, DocumentReference,
the FixedSOM* semantic-object-model family, Glyphs, FixedTextView,
FixedTextBuilder, FixedTextContainer, FixedTextPointer, and the paginator/
content-type/app-model support layers are all linked from upstream WPF and
compiled into `LeXtudio.Windows` by default. A `FixedDocument` can be built
programmatically, paginated, and its pages' real `FixedPage`/WinUI `Panel`
instances retrieved — verified end-to-end via the RichTextBox integration host,
not just by compiling.

What is intentionally still out of scope, tracked as **R8**:

- `RubberbandSelector` — rubber-band text/table selection on a fixed page,
  including its bitmap-clipboard snapshot (needs `ContainerVisual`/
  `DrawingVisual`, which WinUI does not expose) and its `DocumentGrid`
  dependency (a paginated FlowDocument viewer control not carried by this port).
- `HighlightVisual` — the `DrawingVisual`-based highlight rendering layer for
  fixed-document text selection and search-result highlighting.
- Actual on-screen rendering/painting of a `FixedPage`'s content beyond what its
  WinUI `Panel` base class already provides — the document *model* (structure,
  pagination, hyperlink navigation, text extraction) is real; a full visual
  fidelity pass (backgrounds, borders, glyph painting matching WPF pixel-for-
  pixel) was never in scope for this migration and would be its own project.

Two known, documented approximations from rung 7 remain: `GlyphTypeface.
AdvanceHeights` reports line height for every glyph (Skia has no `vmtx` table),
and `FamilyNames` answers one name for every culture (Skia gives one family
name, not WPF's localized name table).
