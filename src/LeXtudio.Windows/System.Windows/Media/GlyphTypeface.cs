// A SkiaSharp-backed GlyphTypeface.
//
// Upstream PresentationCore's GlyphTypeface cannot be linked: it is a thin managed layer
// over DirectWrite (MS.Internal.Text.TextInterface.Font, GetGlyphMetrics, the WPF font
// cache), none of which exists off Windows. The contract it exposes, however, is plain
// font metrics, so it is reimplemented here over SkiaSharp — the same font stack Uno's
// Skia targets already render with.
//
// Units follow WPF: advance widths, heights and XHeight are in em units (a fraction of
// the font's em square), and callers multiply by the rendering em size.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SkiaSharp;

// WinUI already defines StyleSimulations with the same members; use it rather than
// shadowing it, so upstream Glyphs.StyleSimulations binds without ambiguity.
using StyleSimulations = Microsoft.UI.Xaml.Media.StyleSimulations;

namespace System.Windows.Media
{
    /// <summary>
    /// Glyph-level metrics for one physical font face.
    /// </summary>
    public sealed class GlyphTypeface
    {
        private readonly SKTypeface _typeface;
        private readonly SKFont _emFont;
        private readonly float _unitsPerEm;

        private IDictionary<CultureInfo, string>? _familyNames;
        private IDictionary<ushort, double>? _advanceWidths;
        private IDictionary<ushort, double>? _advanceHeights;
        private IDictionary<int, ushort>? _characterToGlyphMap;

        public GlyphTypeface()
            : this(SKTypeface.Default, StyleSimulations.None)
        {
        }

        public GlyphTypeface(Uri typefaceSource)
            : this(typefaceSource, StyleSimulations.None)
        {
        }

        public GlyphTypeface(Uri typefaceSource, StyleSimulations styleSimulations)
            : this(Resolve(typefaceSource), styleSimulations)
        {
            FontUri = typefaceSource;
        }

        private GlyphTypeface(SKTypeface typeface, StyleSimulations styleSimulations)
        {
            _typeface = typeface;
            StyleSimulations = styleSimulations;
            _unitsPerEm = typeface.UnitsPerEm <= 0 ? 1000f : typeface.UnitsPerEm;
            // Sizing the font at the em square makes every metric come back in font design
            // units, which divided by UnitsPerEm gives WPF's em-relative values.
            _emFont = new SKFont(typeface, _unitsPerEm);
        }

        /// <summary>Where the face was loaded from, when it was loaded by uri.</summary>
        public Uri? FontUri { get; private set; }

        public StyleSimulations StyleSimulations { get; }

        /// <summary>Font design units per em; 1000 for most CFF fonts, 2048 for most TrueType.</summary>
        public int DesignEmHeight => (int)_unitsPerEm;

        public int GlyphCount => _typeface.GlyphCount;

        /// <summary>True for a font whose cmap is a symbol encoding rather than Unicode.</summary>
        public bool Symbol => false;

        /// <summary>
        /// Family name per culture. Skia exposes one family name rather than the font's full
        /// localized name table, so the same name is reported for every culture asked for,
        /// which is what the fixed-document code needs it for.
        /// </summary>
        public IDictionary<CultureInfo, string> FamilyNames
            => _familyNames ??= new SingleNameDictionary(_typeface.FamilyName);

        public global::Windows.UI.Text.FontStyle Style => _typeface.FontSlant switch
        {
            SKFontStyleSlant.Italic => global::Windows.UI.Text.FontStyle.Italic,
            SKFontStyleSlant.Oblique => global::Windows.UI.Text.FontStyle.Oblique,
            _ => global::Windows.UI.Text.FontStyle.Normal,
        };

        public global::Windows.UI.Text.FontWeight Weight
            => new() { Weight = (ushort)Math.Clamp(_typeface.FontWeight, 1, 999) };

        public global::Windows.UI.Text.FontStretch Stretch => _typeface.FontWidth switch
        {
            <= 1 => global::Windows.UI.Text.FontStretch.UltraCondensed,
            2 => global::Windows.UI.Text.FontStretch.ExtraCondensed,
            3 => global::Windows.UI.Text.FontStretch.Condensed,
            4 => global::Windows.UI.Text.FontStretch.SemiCondensed,
            5 => global::Windows.UI.Text.FontStretch.Normal,
            6 => global::Windows.UI.Text.FontStretch.SemiExpanded,
            7 => global::Windows.UI.Text.FontStretch.Expanded,
            8 => global::Windows.UI.Text.FontStretch.ExtraExpanded,
            _ => global::Windows.UI.Text.FontStretch.UltraExpanded,
        };

        /// <summary>Height of a lowercase 'x' in em units.</summary>
        public double XHeight => Math.Abs(_emFont.Metrics.XHeight) / _unitsPerEm;

        /// <summary>Distance from the baseline to the top of the em box, in em units.</summary>
        public double Baseline => Math.Abs(_emFont.Metrics.Ascent) / _unitsPerEm;

        /// <summary>Line height in em units.</summary>
        public double Height
            => (Math.Abs(_emFont.Metrics.Ascent) + Math.Abs(_emFont.Metrics.Descent)
                + Math.Abs(_emFont.Metrics.Leading)) / _unitsPerEm;

        /// <summary>Horizontal advance per glyph index, in em units.</summary>
        public IDictionary<ushort, double> AdvanceWidths
            => _advanceWidths ??= new GlyphMetricDictionary(this, HorizontalAdvance);

        /// <summary>
        /// Vertical advance per glyph index, in em units, used for sideways runs. Skia does
        /// not expose the font's vmtx table, so every glyph reports the line height — which
        /// is what a font without vmtx would fall back to anyway.
        /// </summary>
        public IDictionary<ushort, double> AdvanceHeights
            => _advanceHeights ??= new GlyphMetricDictionary(this, _ => Height);

        /// <summary>Unicode code point to glyph index. Missing code points map to glyph 0.</summary>
        public IDictionary<int, ushort> CharacterToGlyphMap
            => _characterToGlyphMap ??= new CodepointToGlyphDictionary(this);

        /// <summary>Ink bounds of one glyph in em units, relative to its origin on the baseline.</summary>
        internal Rect GetGlyphInkBounds(ushort glyphIndex)
        {
            using var path = _emFont.GetGlyphPath(glyphIndex);
            if (path is null || path.IsEmpty)
            {
                // A blank glyph (space) has no ink.
                return Rect.Empty;
            }

            var b = path.Bounds;
            return new Rect(
                b.Left / _unitsPerEm,
                b.Top / _unitsPerEm,
                b.Width / _unitsPerEm,
                b.Height / _unitsPerEm);
        }

        private double HorizontalAdvance(ushort glyphIndex)
        {
            var widths = _emFont.GetGlyphWidths(new[] { glyphIndex });
            return widths is { Length: > 0 } ? widths[0] / _unitsPerEm : 0.0;
        }

        private ushort GlyphForCodepoint(int codepoint) => _emFont.GetGlyph(codepoint);

        private static SKTypeface Resolve(Uri typefaceSource)
        {
            ArgumentNullException.ThrowIfNull(typefaceSource);

            // WPF font uris carry the face index as a fragment (…/arial.ttf#Arial).
            if (typefaceSource.IsAbsoluteUri && typefaceSource.IsFile)
            {
                var path = typefaceSource.LocalPath;
                if (File.Exists(path) && SKTypeface.FromFile(path) is { } fromFile)
                {
                    return fromFile;
                }
            }

            // Anything else (pack://, a relative uri) is matched by family name, taken from
            // the fragment when present and the file name otherwise.
            var name = typefaceSource.IsAbsoluteUri && !string.IsNullOrEmpty(typefaceSource.Fragment)
                ? Uri.UnescapeDataString(typefaceSource.Fragment.TrimStart('#'))
                : Path.GetFileNameWithoutExtension(typefaceSource.OriginalString);

            return (string.IsNullOrEmpty(name) ? null : SKTypeface.FromFamilyName(name))
                ?? SKTypeface.Default;
        }

        /// <summary>Reports one name for whatever culture is asked for.</summary>
        private sealed class SingleNameDictionary(string name) : ReadOnlyLookup<CultureInfo, string>
        {
            public override bool TryGetValue(CultureInfo key, out string value)
            {
                value = name;
                return true;
            }
        }

        /// <summary>Computes a per-glyph metric on demand rather than materialising the font.</summary>
        private sealed class GlyphMetricDictionary(GlyphTypeface owner, Func<ushort, double> metric)
            : ReadOnlyLookup<ushort, double>
        {
            public override bool TryGetValue(ushort key, out double value)
            {
                if (key >= owner.GlyphCount)
                {
                    value = 0.0;
                    return false;
                }

                value = metric(key);
                return true;
            }
        }

        private sealed class CodepointToGlyphDictionary(GlyphTypeface owner) : ReadOnlyLookup<int, ushort>
        {
            public override bool TryGetValue(int key, out ushort value)
            {
                value = owner.GlyphForCodepoint(key);
                // Glyph 0 is .notdef, which is the correct thing to display for a code point
                // the font does not cover, but it is not a successful lookup.
                return value != 0;
            }
        }

        /// <summary>
        /// A computed, read-only IDictionary. Only the indexer, TryGetValue and ContainsKey
        /// are meaningful; enumerating a font's whole glyph set is never what the callers want,
        /// so the collection members throw rather than silently returning nothing.
        /// </summary>
        private abstract class ReadOnlyLookup<TKey, TValue> : IDictionary<TKey, TValue>
            where TKey : notnull
        {
            public abstract bool TryGetValue(TKey key, out TValue value);

            public TValue this[TKey key]
            {
                get => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException();
                set => throw new NotSupportedException();
            }

            public bool ContainsKey(TKey key) => TryGetValue(key, out _);

            public bool Contains(KeyValuePair<TKey, TValue> item)
                => TryGetValue(item.Key, out var value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);

            public int Count => throw new NotSupportedException("The lookup is computed, not enumerable.");

            public bool IsReadOnly => true;

            public ICollection<TKey> Keys => throw new NotSupportedException("The lookup is computed, not enumerable.");

            public ICollection<TValue> Values => throw new NotSupportedException("The lookup is computed, not enumerable.");

            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
                => throw new NotSupportedException("The lookup is computed, not enumerable.");

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public void Add(TKey key, TValue value) => throw new NotSupportedException();

            public void Add(KeyValuePair<TKey, TValue> item) => throw new NotSupportedException();

            public void Clear() => throw new NotSupportedException();

            public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => throw new NotSupportedException();

            public bool Remove(TKey key) => throw new NotSupportedException();

            public bool Remove(KeyValuePair<TKey, TValue> item) => throw new NotSupportedException();
        }
    }
}
