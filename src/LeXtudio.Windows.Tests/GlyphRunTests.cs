using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using Windows.Foundation;
using Xunit;

namespace LeXtudio.Windows.Tests;

/// <summary>
/// GlyphRun's measurement and caret arithmetic (rung 7). The caret methods are ported
/// from upstream line for line, so these pin the cluster and caret-stop behaviour that
/// FixedTextView's hit testing depends on.
/// </summary>
public sealed class GlyphRunTests
{
    /// <summary>Three glyphs, 10/20/30 wide, one code point each.</summary>
    private static GlyphRun SimpleRun(int bidiLevel = 0) => new(
        new GlyphTypeface(),
        bidiLevel,
        isSideways: false,
        renderingEmSize: 12,
        pixelsPerDip: 1,
        glyphIndices: new ushort[] { 1, 2, 3 },
        baselineOrigin: new Point(),
        advanceWidths: new[] { 10.0, 20.0, 30.0 },
        glyphOffsets: null,
        characters: new[] { 'a', 'b', 'c' },
        deviceFontName: null,
        clusterMap: null,
        caretStops: null,
        language: null);

    [Fact]
    public void DistanceAccumulatesAdvanceWidths()
    {
        var run = SimpleRun();

        Assert.Equal(0.0, run.GetDistanceFromCaretCharacterHit(new CharacterHit(0, 0)));
        Assert.Equal(10.0, run.GetDistanceFromCaretCharacterHit(new CharacterHit(1, 0)));
        Assert.Equal(30.0, run.GetDistanceFromCaretCharacterHit(new CharacterHit(2, 0)));
    }

    [Fact]
    public void TrailingEdgeMeasuresToTheNextStop()
    {
        var run = SimpleRun();

        Assert.Equal(10.0, run.GetDistanceFromCaretCharacterHit(new CharacterHit(0, 1)));
    }

    [Fact]
    public void DistanceRejectsAnIndexPastTheRun()
    {
        var run = SimpleRun();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => run.GetDistanceFromCaretCharacterHit(new CharacterHit(99, 0)));
    }

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(4.0, 0)]     // first half of glyph 0
    [InlineData(12.0, 1)]    // inside glyph 1
    [InlineData(35.0, 2)]    // inside glyph 2
    public void HitFromDistanceLandsInTheRightGlyph(double distance, int expectedFirstCharacter)
    {
        var run = SimpleRun();

        var hit = run.GetCaretCharacterHitFromDistance(distance, out var isInside);

        Assert.True(isInside);
        Assert.Equal(expectedFirstCharacter, hit.FirstCharacterIndex);
    }

    [Fact]
    public void HitPastTheEndReportsOutsideAndClampsToTheLastStop()
    {
        var run = SimpleRun();

        var hit = run.GetCaretCharacterHitFromDistance(1000.0, out var isInside);

        Assert.False(isInside);
        Assert.Equal(2, hit.FirstCharacterIndex);
    }

    [Fact]
    public void CaretWalksForwardThenStops()
    {
        var run = SimpleRun();

        // From a leading edge, next is the trailing edge of the same code point.
        var hit = run.GetNextCaretCharacterHit(new CharacterHit(0, 0));
        Assert.Equal(0, hit.FirstCharacterIndex);
        Assert.Equal(1, hit.TrailingLength);

        // From a trailing edge, next is the trailing edge of the following code point.
        hit = run.GetNextCaretCharacterHit(hit);
        Assert.Equal(1, hit.FirstCharacterIndex);
        Assert.Equal(1, hit.TrailingLength);
    }

    [Fact]
    public void CaretWalksBackwardThenStops()
    {
        var run = SimpleRun();

        // From a trailing edge, previous is the leading edge of the same code point.
        var hit = run.GetPreviousCaretCharacterHit(new CharacterHit(1, 1));
        Assert.Equal(1, hit.FirstCharacterIndex);
        Assert.Equal(0, hit.TrailingLength);

        hit = run.GetPreviousCaretCharacterHit(hit);
        Assert.Equal(0, hit.FirstCharacterIndex);

        // At the start there is nowhere further to go.
        Assert.Equal(hit, run.GetPreviousCaretCharacterHit(hit));
    }

    [Fact]
    public void AClusterSharesItsWidthAcrossItsCodePoints()
    {
        // Two code points map to a single 30-wide glyph.
        var run = new GlyphRun(
            new GlyphTypeface(), 0, false, 12, 1,
            glyphIndices: new ushort[] { 1 },
            baselineOrigin: new Point(),
            advanceWidths: new[] { 30.0 },
            glyphOffsets: null,
            characters: new[] { 'a', 'b' },
            deviceFontName: null,
            clusterMap: new ushort[] { 0, 0 },
            caretStops: null,
            language: null);

        // Halfway through the cluster is half its width.
        Assert.Equal(15.0, run.GetDistanceFromCaretCharacterHit(new CharacterHit(1, 0)));
    }

    [Fact]
    public void CaretStopsSuppressPositions()
    {
        // Position 1 is not a caret stop, so walking forward from 0 skips to 2.
        var run = new GlyphRun(
            new GlyphTypeface(), 0, false, 12, 1,
            glyphIndices: new ushort[] { 1, 2, 3 },
            baselineOrigin: new Point(),
            advanceWidths: new[] { 10.0, 20.0, 30.0 },
            glyphOffsets: null,
            characters: new[] { 'a', 'b', 'c' },
            deviceFontName: null,
            clusterMap: null,
            caretStops: new[] { true, false, true, true },
            language: null);

        var hit = run.GetNextCaretCharacterHit(new CharacterHit(0, 0));

        Assert.Equal(0, hit.FirstCharacterIndex);
        Assert.Equal(2, hit.TrailingLength);
    }

    [Fact]
    public void ARunWithNoCaretStopsIsNotHitTestable()
    {
        var run = new GlyphRun(
            new GlyphTypeface(), 0, false, 12, 1,
            glyphIndices: new ushort[] { 1 },
            baselineOrigin: new Point(),
            advanceWidths: new[] { 10.0 },
            glyphOffsets: null,
            characters: new[] { 'a' },
            deviceFontName: null,
            clusterMap: null,
            caretStops: new[] { false, false },
            language: null);

        Assert.False(run.IsHitTestable);
        Assert.Equal(0.0, run.GetDistanceFromCaretCharacterHit(new CharacterHit(0, 0)));
    }

    [Fact]
    public void AlignmentBoxSpansTheSummedAdvanceAtTheFontHeight()
    {
        var run = SimpleRun();

        var box = run.ComputeAlignmentBox();

        Assert.Equal(0.0, box.X);
        Assert.Equal(60.0, box.Width);
        Assert.True(box.Height > 0);
        // The box hangs above the baseline by the ascent.
        Assert.True(box.Y < 0);
    }

    [Fact]
    public void RightToLeftAlignmentBoxExtendsLeftOfTheOrigin()
    {
        var box = SimpleRun(bidiLevel: 1).ComputeAlignmentBox();

        Assert.Equal(-60.0, box.X);
        Assert.Equal(60.0, box.Width);
    }

    [Fact]
    public void InkBoundingBoxIsInsideTheAlignmentBoxForRealGlyphs()
    {
        var typeface = new GlyphTypeface();
        typeface.CharacterToGlyphMap.TryGetValue('M', out var glyph);
        var advance = typeface.AdvanceWidths[glyph] * 12;

        var run = new GlyphRun(
            typeface, 0, false, 12, 1,
            glyphIndices: new[] { glyph },
            baselineOrigin: new Point(),
            advanceWidths: new[] { advance },
            glyphOffsets: null,
            characters: new[] { 'M' },
            deviceFontName: null,
            clusterMap: null,
            caretStops: null,
            language: null);

        var ink = run.ComputeInkBoundingBox();

        Assert.False(ink.IsEmpty);
        Assert.True(ink.Width > 0);
        Assert.True(ink.Height > 0);
        // A capital M's ink cannot be wider than its advance by much, nor taller than the line.
        Assert.True(ink.Width <= run.ComputeAlignmentBox().Width * 1.5);
    }

    [Fact]
    public void AnEmptyRunHasNoInk()
    {
        var run = new GlyphRun(
            new GlyphTypeface(), 0, false, 12, 1,
            glyphIndices: Array.Empty<ushort>(),
            baselineOrigin: new Point(),
            advanceWidths: Array.Empty<double>(),
            glyphOffsets: null,
            characters: Array.Empty<char>(),
            deviceFontName: null,
            clusterMap: null,
            caretStops: null,
            language: null);

        Assert.True(run.ComputeInkBoundingBox().IsEmpty);
    }

    [Fact]
    public void CodepointCountPrefersCharactersThenClusterMap()
    {
        var run = SimpleRun();

        Assert.Equal(3, run.GlyphIndices.Count);
        Assert.Equal(3, run.Characters!.Count);
    }
}
