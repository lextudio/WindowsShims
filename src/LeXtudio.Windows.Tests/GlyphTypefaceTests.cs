using System;
using System.Globalization;
using System.Windows.Media;
using Xunit;

namespace LeXtudio.Windows.Tests;

/// <summary>
/// The SkiaSharp-backed GlyphTypeface (rung 7). Upstream's is a DirectWrite wrapper and
/// cannot be linked, so these pin the contract the fixed-document sources rely on:
/// em-relative metrics and a code-point-to-glyph map.
/// </summary>
public sealed class GlyphTypefaceTests
{
    private static GlyphTypeface Default => new();

    [Fact]
    public void DesignEmHeightIsPositive()
    {
        Assert.True(Default.DesignEmHeight > 0);
    }

    [Fact]
    public void FamilyNamesAnswersForAnyCulture()
    {
        var typeface = Default;

        Assert.True(typeface.FamilyNames.TryGetValue(CultureInfo.InvariantCulture, out var invariant));
        Assert.True(typeface.FamilyNames.TryGetValue(new CultureInfo("ja-JP"), out var japanese));
        Assert.False(string.IsNullOrEmpty(invariant));
        Assert.Equal(invariant, japanese);
    }

    [Fact]
    public void XHeightIsAFractionOfTheEmSquare()
    {
        var xHeight = Default.XHeight;

        // A real x-height sits well inside the em square; 0 would mean the metric is missing.
        Assert.InRange(xHeight, 0.2, 1.0);
    }

    [Fact]
    public void HeightCoversAscentAndDescent()
    {
        var typeface = Default;

        Assert.True(typeface.Height >= typeface.Baseline);
        Assert.InRange(typeface.Height, 0.5, 3.0);
    }

    [Fact]
    public void CharacterToGlyphMapResolvesALetter()
    {
        Assert.True(Default.CharacterToGlyphMap.TryGetValue('A', out var glyph));
        Assert.NotEqual(0, glyph);
    }

    [Fact]
    public void CharacterToGlyphMapReportsMissForAnUnmappedCodepoint()
    {
        // A private-use code point no ordinary font covers. TryGetValue must report the
        // miss while still handing back glyph 0 (.notdef), which is what WPF displays.
        var found = Default.CharacterToGlyphMap.TryGetValue(0x10FFFD, out var glyph);

        Assert.False(found);
        Assert.Equal(0, glyph);
    }

    [Fact]
    public void AdvanceWidthOfALetterIsInEmUnits()
    {
        var typeface = Default;
        typeface.CharacterToGlyphMap.TryGetValue('M', out var glyph);

        var advance = typeface.AdvanceWidths[glyph];

        // Em units: a capital M is a wide glyph but still inside one em.
        Assert.InRange(advance, 0.2, 1.5);
    }

    [Fact]
    public void SpaceAdvancesLessThanACapital()
    {
        var typeface = Default;
        typeface.CharacterToGlyphMap.TryGetValue(' ', out var space);
        typeface.CharacterToGlyphMap.TryGetValue('M', out var capital);

        Assert.True(typeface.AdvanceWidths[space] < typeface.AdvanceWidths[capital]);
    }

    [Fact]
    public void AdvanceWidthIsZeroAndUnfoundBeyondTheGlyphCount()
    {
        var typeface = Default;
        var beyond = (ushort)Math.Min(ushort.MaxValue, typeface.GlyphCount + 1);

        Assert.False(typeface.AdvanceWidths.TryGetValue(beyond, out var advance));
        Assert.Equal(0.0, advance);
    }

    [Fact]
    public void AdvanceHeightsFallBackToTheLineHeight()
    {
        var typeface = Default;
        typeface.CharacterToGlyphMap.TryGetValue('M', out var glyph);

        // Skia exposes no vmtx table, so every glyph reports the line height.
        Assert.Equal(typeface.Height, typeface.AdvanceHeights[glyph]);
    }

    [Fact]
    public void EnumeratingAComputedLookupIsRefused()
    {
        // The lookups are computed, not materialised; enumerating a whole font's glyph set
        // is never what a caller wants, so it throws rather than reporting nothing.
        Assert.Throws<NotSupportedException>(() => Default.AdvanceWidths.Count);
        Assert.Throws<NotSupportedException>(() => Default.AdvanceWidths.Keys);
    }

    [Fact]
    public void AMissingFontFileFallsBackToTheDefaultFace()
    {
        var typeface = new GlyphTypeface(new Uri("file:///nonexistent/nosuchfont.ttf"));

        Assert.True(typeface.DesignEmHeight > 0);
        Assert.True(typeface.FamilyNames.TryGetValue(CultureInfo.InvariantCulture, out var name));
        Assert.False(string.IsNullOrEmpty(name));
    }

    [Fact]
    public void AFontUriFragmentIsTreatedAsAFamilyName()
    {
        // WPF font uris carry the face name in the fragment: …/GlobalUserInterface.CompositeFont#Arial
        var typeface = new GlyphTypeface(new Uri("file:///nonexistent/fonts.ttf#Arial"));

        Assert.True(typeface.FamilyNames.TryGetValue(CultureInfo.InvariantCulture, out var name));
        Assert.False(string.IsNullOrEmpty(name));
    }

    [Fact]
    public void StyleWeightAndStretchReportTheFaceStyle()
    {
        var typeface = Default;

        Assert.Equal(global::Windows.UI.Text.FontStyle.Normal, typeface.Style);
        Assert.InRange(typeface.Weight.Weight, 1, 999);
        Assert.Equal(global::Windows.UI.Text.FontStretch.Normal, typeface.Stretch);
    }
}
