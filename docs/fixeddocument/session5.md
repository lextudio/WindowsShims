### Session 5 - The GlyphRun Surface (R7)

Status: completed.

Scope:

- R7: `GlyphTypeface` and `GlyphRun`, the font-metrics and caret layer that
  `Glyphs`, `FixedSOMTextRun`, `FixedTextView` and `FixedPageStructure` are
  written directly against. This was the blocker the previous four sessions kept
  deferring.

Findings:

- Measured surface: **195 errors before this session, 95 after.** Excluding the
  three files that stay gated for R8 (`RubberbandSelector.cs` and its
  `DocumentGrid`/`HighlightVisual` dependencies), **72**.

- **The decision: reimplement over SkiaSharp, do not link.** Upstream
  `GlyphTypeface` is a managed wrapper over DirectWrite
  (`MS.Internal.Text.TextInterface.Font`, `GetGlyphMetrics`, the WPF font cache)
  and upstream `GlyphRun` is 2500 lines wired to those metrics plus the
  retained-mode drawing layer. Neither can be linked off Windows. SkiaSharp is
  already a dependency of this project and is the font stack Uno's Skia targets
  render with, so it is the natural backend.

- **A third of the apparent GlyphRun problem was not a GlyphRun problem.** All 15
  `FormattedText` seven-argument errors and a large share of the `GlyphRun` and
  `Brushes` ones came from WPF's `DrawDebugVisual` code, which sits inside
  `#if DEBUG` and only compiled because this is a Debug build. Guarding those
  regions with `#if DEBUG && !HAS_UNO` across the `FixedSOM*` family,
  `FixedPageStructure` and `FixedPage.DebugVisualAdorner` took the count from 195
  to 172 before a line of the shim was written. The remaining GlyphRun surface
  was the real text-measurement layer.

- The caret methods needed no font at all. `GetDistanceFromCaretCharacterHit`,
  `GetCaretCharacterHitFromDistance`, `GetNextCaretCharacterHit`,
  `GetPreviousCaretCharacterHit` and `FindNearestCaretStop` are pure index maths
  over `ClusterMap`, `AdvanceWidths` and `CaretStops`, so they are ported from
  upstream line for line. Only `ComputeAlignmentBox` and `ComputeInkBoundingBox`
  reach the font.

- WinUI already defines `Microsoft.UI.Xaml.Media.StyleSimulations` with the same
  members. Declaring a WPF-named one produced a CS0104 ambiguity; aliasing to
  WinUI's is what makes upstream `Glyphs.StyleSimulations` bind.

- SkiaSharp 3.119.2 has no `SKFont.GetGlyphBounds`. Glyph ink bounds come from
  `SKFont.GetGlyphPath(glyph).Bounds` instead, which is exact rather than an
  approximation.

Changes:

- New `System.Windows/Media/GlyphTypeface.cs` (SkiaSharp-backed), replacing a
  stub whose only member was `Symbol => false`. Metrics are in em units as WPF
  reports them: the backing `SKFont` is sized at the em square and every value
  divided by `UnitsPerEm`.
  - `FamilyNames`, `Style`, `Weight`, `Stretch`, `XHeight`, `Baseline`, `Height`,
    `DesignEmHeight`, `GlyphCount`, `AdvanceWidths`, `AdvanceHeights`,
    `CharacterToGlyphMap`, and an internal `GetGlyphInkBounds`.
  - The dictionaries are computed lookups, not materialised: a font's glyph set
    is large and no caller wants to enumerate it, so `Count`/`Keys`/`Values` and
    enumeration throw `NotSupportedException` rather than quietly reporting
    nothing. The indexer, `TryGetValue` and `ContainsKey` are the contract.
  - Known gap: Skia exposes no `vmtx` table, so `AdvanceHeights` reports the line
    height for every glyph. That is what a font without `vmtx` falls back to, but
    it is not the real per-glyph vertical advance for a font that has one. Only
    sideways runs read it.
  - Known gap: Skia gives one family name rather than the font's localized name
    table, so `FamilyNames` answers the same string for every culture.

- New `System.Windows/Media/GlyphRun.cs`, replacing an empty stub: the 14-argument
  constructor and every property upstream exposes, `ComputeAlignmentBox`,
  `ComputeInkBoundingBox`, the four caret methods, and `IsHitTestable`.
  The absent-`CaretStops` and absent-`ClusterMap` defaults are computed lists, as
  upstream's `DefaultCaretStopList`/`DefaultClusterMap` are.

- `ext/wpf`: `#if DEBUG` → `#if DEBUG && !HAS_UNO` in `FixedSOMSemanticBox`,
  `FixedSOMGroup`, `FixedSOMImage`, `FixedSOMFixedBlock`, `FixedSOMPage`,
  `FixedSOMTableRow`, `FixedSOMTable`, `FixedSOMTableCell`, `FixedSOMTextRun`,
  `FixedPageStructure`, and the `DebugVisualAdorner` region of `FixedPage`.

Tests:

- New `GlyphTypefaceTests.cs` (14 tests) on the real default face: the x-height
  lands inside the em square, a space advances less than a capital M, glyph
  lookup succeeds for a letter and reports a miss with glyph 0 for an unmapped
  code point, an out-of-range glyph is unfound rather than throwing, enumerating
  a computed lookup is refused, and a missing font file falls back to the default
  face.
- New `GlyphRunTests.cs` (16 tests): distance accumulation, cluster width shared
  proportionally across its code points, caret stops suppressing positions, a run
  with no stops being non-hit-testable, forward and backward caret walks stopping
  at the ends, the alignment box for left-to-right and right-to-left, and ink
  bounds staying inside the alignment box.

- The suite had to be made serial. `[assembly: CollectionBehavior(DisableTestParallelization = true)]`
  is now in `TestParallelization.cs`. Uno's `DependencyProperty.RegisterAttached`
  goes through `Uno.Collections.HashtableEx`, whose rehash throws
  `IndexOutOfRangeException` when two test classes first touch different
  DP-owning types at the same moment. The suite had been getting away with it;
  adding two classes changed the timing and `BaseUriHelper`'s static constructor
  started failing. Real apps register on the UI thread, so this is a harness
  problem, but it made the suite flaky. Verified stable over three consecutive runs.

  Guarding by `#if DEBUG` region needs care: two of `FixedPageStructure`'s DEBUG
  blocks hold the `FixedTextBuilder`/`FixedNodes` data members, which non-DEBUG
  code in `FixedTextBuilder.cs` reads. Guarding those broke two call sites; only
  the rendering regions carry the `&& !HAS_UNO`.

Result:

- `LeXtudio.Windows` builds clean on `net10.0-desktop`.
- 318/318 model tests, 238/238 RichTextBox integration, 83/83 DataGrid integration.
- Un-gated surface: 93 errors, 72 excluding the R8 files.

What is left is a long tail of one-offs rather than a subsystem: `PathGeometry.CreateFromGeometry`/`AddGeometry`,
`IFrameworkInputElement`, `IScrollInfo` in `TextViewBase`, `CommandBindings`,
`BitmapImage.UriSource`, `Geometry.GetArea`, `FrameworkElement.Initialized`,
`DocumentAutomationPeer`, `AnnotationHighlightLayer`, and
`string.GetCompatibleCulture`. Largest remaining files are `FixedTextBuilder` (16),
`FixedDocument` (13) and `DocumentSequence` (7).

Next: R6 — clear the tail and un-gate the object model. R8 (rendering and
hit-testing) stays last.
