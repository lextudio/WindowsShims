// A positioned run of glyphs from one typeface.
//
// Upstream PresentationCore's GlyphRun is 2500 lines wired to DirectWrite glyph metrics,
// the WPF font cache and the retained-mode drawing layer, so it cannot be linked. What the
// fixed-document sources use is the data the run carries plus its measurement and caret
// arithmetic, and that arithmetic is pure index maths over ClusterMap/AdvanceWidths/
// CaretStops — ported here line for line from upstream. Only the font metrics behind
// ComputeAlignmentBox and ComputeInkBoundingBox come from elsewhere, via the
// SkiaSharp-backed GlyphTypeface.

using System.Collections;
using System.Collections.Generic;
using System.Windows.Markup;
using System.Windows.Media.TextFormatting;

namespace System.Windows.Media
{
    public class GlyphRun
    {
        private GlyphTypeface? _glyphTypeface;
        private IList<ushort> _glyphIndices = Array.Empty<ushort>();
        private IList<double> _advanceWidths = Array.Empty<double>();

        public GlyphRun()
        {
        }

        public GlyphRun(float pixelsPerDip)
        {
            PixelsPerDip = pixelsPerDip;
        }

        public GlyphRun(
            GlyphTypeface glyphTypeface,
            int bidiLevel,
            bool isSideways,
            double renderingEmSize,
            float pixelsPerDip,
            IList<ushort> glyphIndices,
            Point baselineOrigin,
            IList<double> advanceWidths,
            IList<Point>? glyphOffsets,
            IList<char>? characters,
            string? deviceFontName,
            IList<ushort>? clusterMap,
            IList<bool>? caretStops,
            XmlLanguage? language)
        {
            ArgumentNullException.ThrowIfNull(glyphTypeface);
            ArgumentNullException.ThrowIfNull(glyphIndices);
            ArgumentNullException.ThrowIfNull(advanceWidths);

            _glyphTypeface = glyphTypeface;
            BidiLevel = bidiLevel;
            IsSideways = isSideways;
            FontRenderingEmSize = renderingEmSize;
            PixelsPerDip = pixelsPerDip;
            _glyphIndices = glyphIndices;
            BaselineOrigin = baselineOrigin;
            _advanceWidths = advanceWidths;
            GlyphOffsets = glyphOffsets;
            Characters = characters;
            DeviceFontName = deviceFontName;
            ClusterMap = clusterMap;
            CaretStops = caretStops;
            Language = language;
        }

        public float PixelsPerDip { get; set; }

        public GlyphTypeface GlyphTypeface
            => _glyphTypeface ?? throw new InvalidOperationException("The GlyphRun is not initialized.");

        public Point BaselineOrigin { get; set; }

        public double FontRenderingEmSize { get; set; }

        public int BidiLevel { get; set; }

        public bool IsSideways { get; set; }

        public IList<ushort> GlyphIndices
        {
            get => _glyphIndices;
            set => _glyphIndices = value ?? Array.Empty<ushort>();
        }

        public IList<double> AdvanceWidths
        {
            get => _advanceWidths;
            set => _advanceWidths = value ?? Array.Empty<double>();
        }

        public IList<Point>? GlyphOffsets { get; set; }

        public IList<char>? Characters { get; set; }

        public IList<ushort>? ClusterMap { get; set; }

        public IList<bool>? CaretStops { get; set; }

        public XmlLanguage? Language { get; set; }

        public string? DeviceFontName { get; set; }

        /// <summary>A run with no caret stops at all cannot be hit tested.</summary>
        public bool IsHitTestable
        {
            get
            {
                var caretStops = CaretStops;
                if (caretStops is null || caretStops.Count == 0)
                {
                    return true;
                }

                foreach (var stop in caretStops)
                {
                    if (stop)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        internal int GlyphCount => _glyphIndices.Count;

        internal int CodepointCount
        {
            get
            {
                if (Characters is { Count: > 0 })
                {
                    return Characters.Count;
                }

                if (ClusterMap is { Count: > 0 })
                {
                    return ClusterMap.Count;
                }

                return _glyphIndices.Count;
            }
        }

        private bool IsLeftToRight => (BidiLevel & 1) == 0;

        private double AdvanceWidth
        {
            get
            {
                double advance = 0;
                foreach (var glyphAdvance in _advanceWidths)
                {
                    advance += glyphAdvance;
                }

                return advance;
            }
        }

        /// <summary>Distance from the baseline to the top of the alignment box.</summary>
        private double Ascent
            // For sideways text the origin sits in the middle of the character cell.
            => IsSideways
                ? FontRenderingEmSize * GlyphTypeface.Height / 2.0
                : GlyphTypeface.Baseline * FontRenderingEmSize;

        private double Height => GlyphTypeface.Height * FontRenderingEmSize;

        /// <summary>
        /// The alignment box, relative to the run's origin. Its width is the summed advance
        /// and its height the font's line height; a negative total advance extends it the
        /// other way, as upstream does.
        /// </summary>
        public Rect ComputeAlignmentBox()
        {
            var advanceWidth = AdvanceWidth;

            // Rect.Width cannot be negative, so a negative advance flips the direction.
            var extendToRight = IsLeftToRight;
            if (advanceWidth < 0.0)
            {
                extendToRight = !extendToRight;
                advanceWidth = -advanceWidth;
            }

            return extendToRight
                ? new Rect(0, -Ascent, advanceWidth, Height)
                : new Rect(-advanceWidth, -Ascent, advanceWidth, Height);
        }

        /// <summary>
        /// The union of the glyphs' ink, relative to the run's origin. Upstream reads the
        /// per-glyph metrics out of DirectWrite; here they come from the glyph outlines.
        /// </summary>
        public Rect ComputeInkBoundingBox()
        {
            if (_glyphTypeface is null || GlyphCount == 0)
            {
                return Rect.Empty;
            }

            var left = double.PositiveInfinity;
            var top = double.PositiveInfinity;
            var right = double.NegativeInfinity;
            var bottom = double.NegativeInfinity;

            double accumulatedAdvance = 0;

            for (var i = 0; i < GlyphCount; ++i)
            {
                var ink = _glyphTypeface.GetGlyphInkBounds(_glyphIndices[i]);
                var advance = i < _advanceWidths.Count ? _advanceWidths[i] : 0.0;

                if (!ink.IsEmpty)
                {
                    var offset = GetGlyphOffset(i);
                    var originX = IsLeftToRight
                        ? accumulatedAdvance + offset.X
                        : -accumulatedAdvance - advance + offset.X;
                    var originY = -offset.Y;

                    left = Math.Min(left, originX + (ink.Left * FontRenderingEmSize));
                    top = Math.Min(top, originY + (ink.Top * FontRenderingEmSize));
                    right = Math.Max(right, originX + ((ink.Left + ink.Width) * FontRenderingEmSize));
                    bottom = Math.Max(bottom, originY + ((ink.Top + ink.Height) * FontRenderingEmSize));
                }

                accumulatedAdvance += advance;
            }

            return double.IsInfinity(left)
                ? Rect.Empty
                : new Rect(left, top, right - left, bottom - top);
        }

        private Point GetGlyphOffset(int index)
            => GlyphOffsets is { } offsets && index < offsets.Count ? offsets[index] : new Point();

        /// <summary>
        /// Distance from the run's leading edge to the given caret position. Ported from
        /// upstream: advance widths are accumulated cluster by cluster, and a hit inside a
        /// multi-codepoint cluster takes a proportional share of that cluster's width.
        /// </summary>
        public double GetDistanceFromCaretCharacterHit(CharacterHit characterHit)
        {
            var caretStops = EffectiveCaretStops();
            if (characterHit.FirstCharacterIndex < 0 || characterHit.FirstCharacterIndex > CodepointCount)
            {
                throw new ArgumentOutOfRangeException(nameof(characterHit));
            }

            FindNearestCaretStop(
                characterHit.FirstCharacterIndex,
                caretStops,
                out var caretStopIndex,
                out var codePointsUntilNextStop);

            // Not a hit testable glyph run.
            if (caretStopIndex == -1)
            {
                return 0.0;
            }

            // Trailing edge of a caret stop with no valid next stop.
            if (codePointsUntilNextStop == -1 && characterHit.TrailingLength != 0)
            {
                return 0.0;
            }

            var caretCodePoint = characterHit.TrailingLength == 0
                ? caretStopIndex
                : caretStopIndex + codePointsUntilNextStop;

            var distance = 0.0;
            var clusterMap = EffectiveClusterMap();
            var advances = AdvanceWidths;

            var clusterCodepointStart = 0;
            var currentCodepoint = clusterCodepointStart;

            while (true)
            {
                ++currentCodepoint;

                if (currentCodepoint < clusterMap.Count
                    && clusterMap[currentCodepoint] == clusterMap[clusterCodepointStart])
                {
                    continue;
                }

                // Reached the next cluster, or the end of the run.
                var clusterGlyphEnd = currentCodepoint >= clusterMap.Count
                    ? advances.Count
                    : clusterMap[currentCodepoint];

                var clusterWidth = 0.0;
                for (var i = clusterMap[clusterCodepointStart]; i < clusterGlyphEnd; ++i)
                {
                    clusterWidth += advances[i];
                }

                if (caretCodePoint < currentCodepoint || currentCodepoint >= clusterMap.Count)
                {
                    // The caret sits inside this cluster: take a proportional share.
                    clusterWidth *= (double)(caretCodePoint - clusterCodepointStart)
                        / (currentCodepoint - clusterCodepointStart);
                    distance += clusterWidth;
                    break;
                }

                distance += clusterWidth;
                clusterCodepointStart = currentCodepoint;
            }

            return distance;
        }

        /// <summary>
        /// The caret position closest to an offset from the run's leading edge.
        /// <paramref name="isInside"/> is false when the offset falls outside the run.
        /// </summary>
        public CharacterHit GetCaretCharacterHitFromDistance(double distance, out bool isInside)
        {
            var advances = AdvanceWidths;
            var caretStops = EffectiveCaretStops();
            var clusterMap = EffectiveClusterMap();

            // Closest caret stop to the left of the offset.
            var firstStopIndex = -1;
            var firstStopAdvance = 0.0;

            // Closest caret stop to its right.
            var secondStopIndex = -1;

            var currentAdvance = 0.0;
            var currentClusterStart = 0;
            var secondStopFound = false;

            // caretStops holds clusterMap.Count + 1 entries, so guard before indexing.
            for (var i = 1; i < caretStops.Count && !secondStopFound; ++i)
            {
                if (i < clusterMap.Count && clusterMap[i] == clusterMap[currentClusterStart])
                {
                    continue;
                }

                // End of an (n:m) cluster: sum its m glyph advances.
                var lastGlyphInCluster = i < clusterMap.Count ? clusterMap[i] : (ushort)advances.Count;

                var clusterAdvance = 0.0;
                for (var j = clusterMap[currentClusterStart]; j < lastGlyphInCluster; ++j)
                {
                    clusterAdvance += advances[j];
                }

                // Divided evenly across the cluster's n code points.
                clusterAdvance /= i - currentClusterStart;

                for (var j = currentClusterStart; j < i; ++j)
                {
                    if (caretStops[j])
                    {
                        if (currentAdvance <= distance)
                        {
                            firstStopIndex = j;
                            firstStopAdvance = currentAdvance;
                        }
                        else
                        {
                            secondStopIndex = j;
                            secondStopFound = true;
                            break;
                        }
                    }

                    currentAdvance += clusterAdvance;
                }

                if (!secondStopFound)
                {
                    currentClusterStart = i;
                }
            }

            // The loop only inspects stops up to i-1, so the final stop is checked here.
            if (!secondStopFound && caretStops[caretStops.Count - 1] && currentAdvance > distance)
            {
                secondStopIndex = caretStops.Count - 1;
            }

            if (firstStopIndex == -1 && secondStopIndex == -1)
            {
                // Every stop but possibly the last is false.
                isInside = false;
                return caretStops[caretStops.Count - 1]
                    ? new CharacterHit(caretStops.Count - 1, 0)
                    : new CharacterHit(0, 0);
            }

            if (firstStopIndex == -1)
            {
                // The offset is left of the first caret stop.
                isInside = false;
                return new CharacterHit(secondStopIndex, 0);
            }

            if (secondStopIndex == -1)
            {
                // The offset is right of the last caret stop.
                isInside = false;
                return new CharacterHit(firstStopIndex, caretStops.Count - 1 - firstStopIndex);
            }

            isInside = true;

            return distance <= (firstStopAdvance + currentAdvance) / 2.0
                ? new CharacterHit(firstStopIndex, 0)
                : new CharacterHit(firstStopIndex, secondStopIndex - firstStopIndex);
        }

        /// <summary>
        /// The next caret position in logical order, or the input when there is none.
        /// </summary>
        public CharacterHit GetNextCaretCharacterHit(CharacterHit characterHit)
        {
            var caretStops = EffectiveCaretStops();
            if (characterHit.FirstCharacterIndex < 0 || characterHit.FirstCharacterIndex > CodepointCount)
            {
                throw new ArgumentOutOfRangeException(nameof(characterHit));
            }

            FindNearestCaretStop(
                characterHit.FirstCharacterIndex,
                caretStops,
                out var caretStopIndex,
                out var codePointsUntilNextStop);

            if (caretStopIndex == -1 || codePointsUntilNextStop == -1)
            {
                return characterHit;
            }

            // From a leading edge, move to the trailing edge of the same code point.
            if (characterHit.TrailingLength == 0)
            {
                return new CharacterHit(caretStopIndex, codePointsUntilNextStop);
            }

            FindNearestCaretStop(
                caretStopIndex + codePointsUntilNextStop,
                caretStops,
                out var nextCaretStopIndex,
                out var nextCodePointsUntilNextStop);

            return nextCodePointsUntilNextStop == -1
                ? characterHit
                : new CharacterHit(nextCaretStopIndex, nextCodePointsUntilNextStop);
        }

        /// <summary>
        /// The previous caret position in logical order, or the input when there is none.
        /// </summary>
        public CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit)
        {
            var caretStops = EffectiveCaretStops();
            if (characterHit.FirstCharacterIndex < 0 || characterHit.FirstCharacterIndex > CodepointCount)
            {
                throw new ArgumentOutOfRangeException(nameof(characterHit));
            }

            FindNearestCaretStop(
                characterHit.FirstCharacterIndex,
                caretStops,
                out var caretStopIndex,
                out _);

            if (caretStopIndex == -1)
            {
                return characterHit;
            }

            // From a trailing edge, move to the leading edge of the same code point.
            if (characterHit.TrailingLength != 0)
            {
                return new CharacterHit(caretStopIndex, 0);
            }

            FindNearestCaretStop(
                caretStopIndex - 1,
                caretStops,
                out var previousCaretStopIndex,
                out _);

            return previousCaretStopIndex == -1 || previousCaretStopIndex == caretStopIndex
                ? characterHit
                : new CharacterHit(previousCaretStopIndex, 0);
        }

        /// <summary>
        /// The nearest caret stop at or left of <paramref name="characterIndex"/>, falling
        /// back to the right, and how many code points until the following stop.
        /// Both are -1 when the run has no usable stop.
        /// </summary>
        private static void FindNearestCaretStop(
            int characterIndex,
            IList<bool> caretStops,
            out int caretStopIndex,
            out int codePointsUntilNextStop)
        {
            caretStopIndex = -1;
            codePointsUntilNextStop = -1;

            if (characterIndex < 0 || characterIndex >= caretStops.Count)
            {
                return;
            }

            for (var i = characterIndex; i >= 0; --i)
            {
                if (caretStops[i])
                {
                    caretStopIndex = i;
                    break;
                }
            }

            if (caretStopIndex == -1)
            {
                for (var i = characterIndex + 1; i < caretStops.Count; ++i)
                {
                    if (caretStops[i])
                    {
                        caretStopIndex = i;
                        break;
                    }
                }
            }

            if (caretStopIndex == -1)
            {
                // No caret stops: the run is not hit testable.
                return;
            }

            for (var lastStop = caretStopIndex + 1; lastStop < caretStops.Count; ++lastStop)
            {
                if (caretStops[lastStop])
                {
                    codePointsUntilNextStop = lastStop - caretStopIndex;
                    return;
                }
            }
        }

        private IList<bool> EffectiveCaretStops()
            => CaretStops is { Count: > 0 } stops ? stops : new AllTrueList(CodepointCount + 1);

        private IList<ushort> EffectiveClusterMap()
            => ClusterMap is { Count: > 0 } map ? map : new IdentityClusterMap(CodepointCount);

        /// <summary>Stands in for an absent CaretStops: every position is a stop.</summary>
        private sealed class AllTrueList(int count) : ReadOnlyList<bool>(count)
        {
            public override bool this[int index]
                => (uint)index < (uint)Count ? true : throw new IndexOutOfRangeException();
        }

        /// <summary>Stands in for an absent ClusterMap: one codepoint to one glyph.</summary>
        private sealed class IdentityClusterMap(int count) : ReadOnlyList<ushort>(count)
        {
            public override ushort this[int index]
                => (uint)index < (uint)Count ? (ushort)index : throw new IndexOutOfRangeException();
        }

        private abstract class ReadOnlyList<T>(int count) : IList<T>
        {
            public int Count { get; } = count;

            public abstract T this[int index] { get; }

            T IList<T>.this[int index]
            {
                get => this[index];
                set => throw new NotSupportedException();
            }

            public bool IsReadOnly => true;

            public IEnumerator<T> GetEnumerator()
            {
                for (var i = 0; i < Count; ++i)
                {
                    yield return this[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public int IndexOf(T item)
            {
                var comparer = EqualityComparer<T>.Default;
                for (var i = 0; i < Count; ++i)
                {
                    if (comparer.Equals(this[i], item))
                    {
                        return i;
                    }
                }

                return -1;
            }

            public bool Contains(T item) => IndexOf(item) >= 0;

            public void CopyTo(T[] array, int arrayIndex)
            {
                for (var i = 0; i < Count; ++i)
                {
                    array[arrayIndex + i] = this[i];
                }
            }

            public void Add(T item) => throw new NotSupportedException();

            public void Clear() => throw new NotSupportedException();

            public void Insert(int index, T item) => throw new NotSupportedException();

            public bool Remove(T item) => throw new NotSupportedException();

            public void RemoveAt(int index) => throw new NotSupportedException();
        }
    }
}
