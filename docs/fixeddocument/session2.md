### Session 2 - Shared Value-Type Helpers and DocumentsTrace (R2 + R3)

Status: completed.

Scope:

- R2: the `Rect`/`Transform`/`Brushes` surface WPF code assumes and
  `Windows.Foundation` / WinUI do not carry.
- R3: `MS.Internal.Documents.DocumentsTrace`.

Neither rung un-gates any fixed-document file; both clear compile surface that
the family (and the rest of the port) needs.

Findings:

- The gate is now switchable. `LeXtudio.Windows.csproj` reads
  `$(FixedDocumentRung)`, so the whole family can be un-gated for measurement:

  ```bash
  dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj \
    -f net10.0-desktop -p:FixedDocumentRung=all
  ```

  The local `FixedPage` hyperlink shim moved out of `HyperlinkSupport.cs` into
  its own file so it can be removed under that condition — otherwise upstream
  `FixedPage.cs` collides with it during the generator pass and hides every
  other error.

- Measured surface: **585 errors before this session, 389 after.**

- `Windows.Foundation.Rect` is a struct, so WPF's mutating members had to become
  extension members on a `ref` receiver (`extension(ref Rect rect)`). That keeps
  the upstream call shape `rect.Offset(dx, dy)` working with WPF's in-place
  semantics rather than forcing a rewrite to an assignment.

- Three constructor shapes cannot be reached by any extension and were patched at
  the call site behind `#if HAS_UNO`: `new Rect(Size)` (5 sites, now
  `RectExtensions.FromSize`), `new RectangleGeometry(Rect)` (3 sites, now an
  object initialiser) and `new MatrixTransform(Matrix)` (1 site).

- `DocumentsTrace` is safe to link despite its fields being null in Release:
  every method is `[Conditional("DEBUG")]`, so the call is elided rather than
  dereferencing null. Its only dependency is `Shared/MS/Internal/ReflectionUtils.cs`,
  also linked.

- `Shared/System/Windows/Markup/TypeConverterHelper.cs` was tried as a link and
  rejected: its converter registry needs `ReflectionHelper`, `TypeTypeConverter`,
  `DateTimeConverter2` and `System.Xaml`, none of which exist on Uno. The fixed
  document sources only read `InvariantEnglishUS`, so a 20-line local file with
  that one member replaced it, with the reason in the header.

- First attempt at `GeneralTransform.AffineTransform` returned the shim
  `GeneralTransform` and produced five new conversion errors: WPF's property is
  typed `Transform`. It now samples the receiver's point mapping at the origin
  and the two unit vectors and returns a WinUI `MatrixTransform`.

Changes:

- New `System.Windows/Media/RectAndTransformExtensions.cs`:
  - `RectExtensions` — `Offset`, `Inflate`, `Transform`, `Scale` on a `ref`
    receiver; `TopLeft`/`TopRight`/`BottomLeft`/`BottomRight`, `IntersectsWith`;
    the static `TransformBounds` and `FromSize` helpers.
  - `WinUITransformExtensions` — `Transform.Identity`, `Transform.Value`,
    `GeneralTransform.AffineTransform`, and `Matrix` conversion both ways.
    `Value` reads a `MatrixTransform`'s matrix directly and falls back to
    sampling the point mapping for any other transform, which is exact for the
    affine transforms WPF's hierarchy produces.
- `System.Windows/Media/Matrix.cs`: `IsIdentity`, `Multiply` and `operator *`.
- `System.Windows/Media/Colors.cs`: `DarkViolet`, `CadetBlue`.
- `System.Windows/Media/Brushes.cs`: 10 named brushes.
- `System.Windows/Controls/Control.cs`: `DpiScale.PixelsPerDip` (the vertical
  scale factor, as in WPF).
- `System.Windows/LogicalTreeHelper.cs`: `GetChildren` (Panel children,
  ContentControl content, Border child) and a depth-first `FindLogicalNode`.
- New `System.Windows/Markup/TypeConverterHelper.cs` (`InvariantEnglishUS` only).
- Linked `Shared/MS/Internal/ReflectionUtils.cs` and
  `PresentationFramework/MS/Internal/documents/DocumentsTrace.cs`.
- `ext/wpf`: the seven constructor call sites above, each `#if HAS_UNO`.

Tests:

- New `src/LeXtudio.Windows.Tests/RectAndTransformExtensionsTests.cs` (14 tests):
  offset and inflate in place, inflate-past-zero collapsing to empty, the four
  corners, `IntersectsWith` including the touching-corner and empty-rect cases,
  bounding box under a 90-degree rotation, negative-factor scale normalising,
  `FromSize`, matrix multiply ordering, and `IsIdentity`.
- `Transform.Value`, `Transform.Identity` and `AffineTransform` are deliberately
  not covered here: constructing a WinUI `MatrixTransform` needs a
  `DispatcherQueue`, which the model test project has no app to supply. They
  need an integration test.

Result:

- `LeXtudio.Windows` builds clean on `net10.0-desktop`.
- 261/261 model tests, 238/238 RichTextBox integration, 83/83 DataGrid integration.
- Un-gated surface: 389 errors, down from 585.

Remaining buckets, largest first: `GlyphRun` members (~41), `Image`/`Path`
ambiguity (39), `FormattedText` 7-arg constructor and `BuildHighlightGeometry`
(18), `SR` constants (~30), app-model loading (`MimeTypeMapper`,
`WpfWebRequestHelper`, `BaseUriHelper.GetBaseUri`, ~15), element-level gaps
(`BringIntoView`, `InheritanceBehavior`, `IsInitialized`, `GetDpi`,
`TransformToAncestor` returning the WinUI `GeneralTransform`, ~18).

Next: R4 (element-level gaps) is the last rung before the GlyphRun decision.
