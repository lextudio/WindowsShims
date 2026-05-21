// Florence Layout Engine — cross-platform document layout for HAS_UNO.
//
// Florence replaces WPF's PTS (Page/Text Services) engine with a clean,
// layered architecture designed for Uno Platform rendering targets.
//
// Architecture layers:
//
//   ┌─────────────────────────────────────────────────────┐
//   │  FlowDocument / TextBoxBase  (upstream WPF source)  │
//   ├─────────────────────────────────────────────────────┤
//   │  Florence API surface                               │
//   │  (StructuralCache, IFlowDocumentFormatter, …)       │
//   ├─────────────────────────────────────────────────────┤
//   │  Florence layout model                              │
//   │  (FlorenceDocument → FlorencePage → FlorenceLine)   │
//   ├─────────────────────────────────────────────────────┤
//   │  Text arrangement (future: Preedit integration)     │
//   └─────────────────────────────────────────────────────┘
//
// Current status: API-complete stubs.  All formatting state is "not yet
// formatted" so guard-checked invalidation paths exit early.  Layout model
// is allocated but empty — ready for a real line-breaking pass.

using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using MS.Internal;
using MS.Internal.Documents;

namespace MS.Internal.Florence
{
    // -----------------------------------------------------------------------
    // Text pointer — position within a FlorenceTextContainer.
    // Modeled on NullTextPointer; all content is empty, so all positions are
    // equivalent and every distance is 0.
    // -----------------------------------------------------------------------

    internal sealed class FlorenceTextPointer : ITextPointer
    {
        private FlorenceTextContainer _container;
        private LogicalDirection _gravity;
        private bool _isFrozen;

        internal FlorenceTextPointer(FlorenceTextContainer container, LogicalDirection gravity)
        {
            _container = container;
            _gravity   = gravity;
        }

        // ── ITextPointer methods ────────────────────────────────────────────

        ITextPointer ITextPointer.CreatePointer()                            => new FlorenceTextPointer(_container, _gravity);
        ITextPointer ITextPointer.CreatePointer(int offset)                  => new FlorenceTextPointer(_container, _gravity);
        ITextPointer ITextPointer.CreatePointer(LogicalDirection gravity)    => new FlorenceTextPointer(_container, gravity);
        ITextPointer ITextPointer.CreatePointer(int offset, LogicalDirection gravity) => new FlorenceTextPointer(_container, gravity);

        StaticTextPointer ITextPointer.CreateStaticPointer()
            => new StaticTextPointer(_container, ((ITextPointer)this).CreatePointer());

        void ITextPointer.SetLogicalDirection(LogicalDirection direction) { _gravity = direction; }

        int ITextPointer.CompareTo(ITextPointer position)        => 0;
        int ITextPointer.CompareTo(StaticTextPointer position)   => 0;
        bool ITextPointer.HasEqualScope(ITextPointer position)   => true;

        TextPointerContext ITextPointer.GetPointerContext(LogicalDirection direction) => TextPointerContext.None;
        int ITextPointer.GetOffsetToPosition(ITextPointer position) => 0;
        int ITextPointer.GetTextRunLength(LogicalDirection direction) => 0;
        string ITextPointer.GetTextInRun(LogicalDirection direction) => TextPointerBase.GetTextInRun(this, direction);
        int ITextPointer.GetTextInRun(LogicalDirection direction, char[] textBuffer, int startIndex, int count) => 0;
        object ITextPointer.GetAdjacentElement(LogicalDirection direction) => null;
        Type ITextPointer.GetElementType(LogicalDirection direction) => null;

        object ITextPointer.GetValue(DependencyProperty formattingProperty) => formattingProperty.DefaultMetadata.DefaultValue;
        object ITextPointer.ReadLocalValue(DependencyProperty formattingProperty) => DependencyProperty.UnsetValue;
        LocalValueEnumerator ITextPointer.GetLocalValueEnumerator()
            => (new FormattingDependencyObject()).GetLocalValueEnumerator();

        void ITextPointer.MoveToPosition(ITextPointer position) { }
        int ITextPointer.MoveByOffset(int offset) => 0;
        bool ITextPointer.MoveToNextContextPosition(LogicalDirection direction) => false;
        void ITextPointer.MoveToElementEdge(ElementEdge edge) { }
        int ITextPointer.MoveToLineBoundary(int count) => 0;
        Rect ITextPointer.GetCharacterRect(LogicalDirection direction) => new Rect();
        bool ITextPointer.MoveToInsertionPosition(LogicalDirection direction) => TextPointerBase.MoveToInsertionPosition(this, direction);
        bool ITextPointer.MoveToNextInsertionPosition(LogicalDirection direction) => TextPointerBase.MoveToNextInsertionPosition(this, direction);
        void ITextPointer.InsertTextInRun(string textData) { }
        void ITextPointer.DeleteContentToPosition(ITextPointer limit) { }

        ITextPointer ITextPointer.GetNextContextPosition(LogicalDirection direction)
        {
            ITextPointer ptr = ((ITextPointer)this).CreatePointer();
            if (ptr.MoveToNextContextPosition(direction)) ptr.Freeze();
            else ptr = null;
            return ptr;
        }

        ITextPointer ITextPointer.GetInsertionPosition(LogicalDirection direction)
        {
            ITextPointer ptr = ((ITextPointer)this).CreatePointer();
            ptr.MoveToInsertionPosition(direction);
            ptr.Freeze();
            return ptr;
        }

        ITextPointer ITextPointer.GetFormatNormalizedPosition(LogicalDirection direction)
        {
            ITextPointer ptr = ((ITextPointer)this).CreatePointer();
            TextPointerBase.MoveToFormatNormalizedPosition(ptr, direction);
            ptr.Freeze();
            return ptr;
        }

        ITextPointer ITextPointer.GetNextInsertionPosition(LogicalDirection direction)
        {
            ITextPointer ptr = ((ITextPointer)this).CreatePointer();
            if (ptr.MoveToNextInsertionPosition(direction)) ptr.Freeze();
            else ptr = null;
            return ptr;
        }

        ITextPointer ITextPointer.GetFrozenPointer(LogicalDirection logicalDirection)
            => TextPointerBase.GetFrozenPointer(this, logicalDirection);

        void ITextPointer.Freeze() { _isFrozen = true; }
        bool ITextPointer.ValidateLayout() => false;

        // ── ITextPointer properties ─────────────────────────────────────────

        ITextContainer ITextPointer.TextContainer  => _container;
        bool ITextPointer.HasValidLayout           => false;
        bool ITextPointer.IsAtCaretUnitBoundary    => false;
        LogicalDirection ITextPointer.LogicalDirection => _gravity;
        Type ITextPointer.ParentType               => typeof(object);
        bool ITextPointer.IsAtInsertionPosition    => TextPointerBase.IsAtInsertionPosition(this);
        bool ITextPointer.IsFrozen                 => _isFrozen;
        int ITextPointer.Offset                    => 0;
        int ITextPointer.CharOffset                => 0;
    }

    // -----------------------------------------------------------------------
    // Text container — mutable empty ITextContainer for HAS_UNO.
    // Modeled on NullTextContainer; provides real event subscriptions so that
    // InitializeForFirstFormatting can hook Changing/Change without crashing.
    // -----------------------------------------------------------------------

    internal sealed class FlorenceTextContainer : ITextContainer
    {
        private readonly ITextPointer _start;
        private readonly ITextPointer _end;
        private readonly Highlights _highlights;

        internal FlorenceTextContainer(System.Windows.Documents.FlowDocument owner)
        {
            Parent     = owner;
            _start     = new FlorenceTextPointer(this, LogicalDirection.Backward);
            _end       = new FlorenceTextPointer(this, LogicalDirection.Forward);
            _highlights = new Highlights(this);
        }

        // ── ITextContainer methods ──────────────────────────────────────────

        void ITextContainer.BeginChange()      { }
        void ITextContainer.BeginChangeNoUndo() { ((ITextContainer)this).BeginChange(); }
        void ITextContainer.EndChange()        { ((ITextContainer)this).EndChange(false); }
        void ITextContainer.EndChange(bool skipEvents) { }

        ITextPointer ITextContainer.CreatePointerAtOffset(int offset, LogicalDirection direction)
            => ((ITextContainer)this).Start.CreatePointer(offset, direction);

        ITextPointer ITextContainer.CreatePointerAtCharOffset(int charOffset, LogicalDirection direction)
            => throw new NotImplementedException();

        ITextPointer ITextContainer.CreateDynamicTextPointer(StaticTextPointer position, LogicalDirection direction)
            => ((ITextPointer)position.Handle0).CreatePointer(direction);

        StaticTextPointer ITextContainer.CreateStaticPointerAtOffset(int offset)
            => new StaticTextPointer(this, ((ITextContainer)this).CreatePointerAtOffset(offset, LogicalDirection.Forward));

        TextPointerContext ITextContainer.GetPointerContext(StaticTextPointer pointer, LogicalDirection direction)
            => ((ITextPointer)pointer.Handle0).GetPointerContext(direction);

        int ITextContainer.GetOffsetToPosition(StaticTextPointer position1, StaticTextPointer position2)
            => ((ITextPointer)position1.Handle0).GetOffsetToPosition((ITextPointer)position2.Handle0);

        int ITextContainer.GetTextInRun(StaticTextPointer position, LogicalDirection direction, char[] textBuffer, int startIndex, int count)
            => ((ITextPointer)position.Handle0).GetTextInRun(direction, textBuffer, startIndex, count);

        object ITextContainer.GetAdjacentElement(StaticTextPointer position, LogicalDirection direction)
            => ((ITextPointer)position.Handle0).GetAdjacentElement(direction);

        DependencyObject ITextContainer.GetParent(StaticTextPointer position) => null;

        StaticTextPointer ITextContainer.CreatePointer(StaticTextPointer position, int offset)
            => new StaticTextPointer(this, ((ITextPointer)position.Handle0).CreatePointer(offset));

        StaticTextPointer ITextContainer.GetNextContextPosition(StaticTextPointer position, LogicalDirection direction)
            => new StaticTextPointer(this, ((ITextPointer)position.Handle0).GetNextContextPosition(direction));

        int ITextContainer.CompareTo(StaticTextPointer position1, StaticTextPointer position2)
            => ((ITextPointer)position1.Handle0).CompareTo((ITextPointer)position2.Handle0);

        int ITextContainer.CompareTo(StaticTextPointer position1, ITextPointer position2)
            => ((ITextPointer)position1.Handle0).CompareTo(position2);

        object ITextContainer.GetValue(StaticTextPointer position, DependencyProperty formattingProperty)
            => ((ITextPointer)position.Handle0).GetValue(formattingProperty);

        // ── ITextContainer properties ───────────────────────────────────────

        bool ITextContainer.IsReadOnly     => false;
        ITextPointer ITextContainer.Start  => _start;
        ITextPointer ITextContainer.End    => _end;
        DependencyObject ITextContainer.Parent => Parent;
        Highlights ITextContainer.Highlights   => _highlights;
        ITextSelection ITextContainer.TextSelection { get => null; set { } }
        UndoManager ITextContainer.UndoManager => null;
        ITextView ITextContainer.TextView  { get => null; set { } }
        int ITextContainer.SymbolCount     => 0;
        int ITextContainer.IMECharCount    => 0;
        uint ITextContainer.Generation     => _generation;

        // ── ITextContainer events ───────────────────────────────────────────
        // Real subscriptions so InitializeForFirstFormatting can hook without crashing.

        public event EventHandler Changing;
        public event TextContainerChangeEventHandler Change;
        public event TextContainerChangedEventHandler Changed;

        // ── Internal helpers ────────────────────────────────────────────────

        internal DependencyObject Parent { get; }

        internal void RaiseChanging() => Changing?.Invoke(this, EventArgs.Empty);
        internal void RaiseChange(TextContainerChangeEventArgs e) => Change?.Invoke(this, e);
        internal void RaiseChanged(TextContainerChangedEventArgs e) => Changed?.Invoke(this, e);

        private uint _generation;
        internal void BumpGeneration() => _generation++;
    }

    // -----------------------------------------------------------------------
    // Layout model — the output of a Florence layout pass.
    // -----------------------------------------------------------------------

    /// <summary>A single text run within a laid-out line.</summary>
    internal sealed class FlorenceRun
    {
        internal FlorenceRun(int startOffset, int length, double x, double width, string text,
            double fontSize, bool bold, bool italic,
            Microsoft.UI.Xaml.Media.FontFamily? fontFamily,
            Microsoft.UI.Xaml.Media.Brush? foreground,
            bool underline,
            System.Windows.Documents.Hyperlink? hyperlink)
        {
            StartOffset = startOffset;
            Length      = length;
            X           = x;
            Width       = width;
            Text        = text;
            FontSize    = fontSize;
            Bold        = bold;
            Italic      = italic;
            FontFamily  = fontFamily;
            Foreground  = foreground;
            Underline   = underline;
            Hyperlink   = hyperlink;
        }

        internal int    StartOffset { get; }
        internal int    EndOffset   => StartOffset + Length;
        internal int    Length      { get; }
        internal double X           { get; }
        internal double Width       { get; }
        internal string Text        { get; }
        internal double FontSize    { get; }
        internal bool   Bold        { get; }
        internal bool   Italic      { get; }
        // Mirrors WPF's TextRunProperties.Typeface.FontFamily. Null means inherit
        // whatever the host's TextBlock default resolves to.
        internal Microsoft.UI.Xaml.Media.FontFamily? FontFamily { get; }
        internal Microsoft.UI.Xaml.Media.Brush? Foreground { get; }
        internal bool Underline { get; }
        internal System.Windows.Documents.Hyperlink? Hyperlink { get; }

        // Average pixel width per character in this run (estimated).
        internal double CharWidth => Length > 0 ? Width / Length : 0;
    }

    /// <summary>A single laid-out line within a page.</summary>
    internal sealed class FlorenceLine
    {
        internal FlorenceLine(int startOffset, int length, double y, double baseline, double height,
            string fullText, IReadOnlyList<FlorenceRun> runs)
        {
            StartOffset = startOffset;
            Length      = length;
            Y           = y;
            Baseline    = baseline;
            Height      = height;
            FullText    = fullText;
            Runs        = runs;
        }

        internal int    StartOffset { get; }
        internal int    EndOffset   => StartOffset + Length;
        internal int    Length      { get; }
        internal double Y           { get; }
        internal double Baseline    { get; }
        internal double Height      { get; }
        internal string FullText    { get; }
        internal IReadOnlyList<FlorenceRun> Runs { get; }
    }

    /// <summary>A single laid-out page within a document.</summary>
    internal sealed class FlorencePage
    {
        private readonly List<FlorenceLine> _lines = new List<FlorenceLine>();

        internal Windows.Foundation.Size PageSize  { get; set; }
        internal IReadOnlyList<FlorenceLine> Lines => _lines;

        internal void AddLine(FlorenceLine line) => _lines.Add(line);
        internal void Clear()                    => _lines.Clear();
    }

    /// <summary>
    /// The top-level Florence document — holds all formatted pages.
    /// A layout pass populates Pages; an invalidation clears it.
    /// </summary>
    internal sealed class FlorenceDocument
    {
        private readonly List<FlorencePage> _pages = new List<FlorencePage>();

        internal IReadOnlyList<FlorencePage> Pages => _pages;
        internal bool IsFormatted => _pages.Count > 0;

        internal void AddPage(FlorencePage page) => _pages.Add(page);

        /// <summary>Discard all layout results — next access triggers a fresh pass.</summary>
        internal void Invalidate() => _pages.Clear();
    }

    // -----------------------------------------------------------------------
    // Florence layout engine — walks FlowDocument, produces FlorencePage.
    // Uses character-width estimation (FontSize * 0.5) for line breaking and
    // hit-testing. Replace with Uno text measurement for pixel-perfect layout.
    // -----------------------------------------------------------------------

    internal static class TextMeasurer
    {
        // One reusable TextBlock per thread (always UI thread for Measure calls).
        [ThreadStatic] private static Microsoft.UI.Xaml.Controls.TextBlock? _probe;

        // TextBlock.DesiredSize.Width strips trailing whitespace (matches WPF/DirectWrite
        // TextLine.Width, but NOT WidthIncludingTrailingWhitespace). That breaks caret
        // positioning: prefix "ab " measures the same as "ab", so the caret jumps backward
        // across spaces. We work around by appending a non-whitespace sentinel and
        // subtracting its width — this preserves trailing whitespace in the measurement
        // while keeping kerning impact small (period has no kern pairs with whitespace
        // in any reasonable font).
        private const char SentinelChar = '.';

        internal static double MeasureWidth(string text, double fontSize, bool bold, bool italic,
            Microsoft.UI.Xaml.Media.FontFamily? fontFamily = null)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            ConfigureProbe(fontSize, bold, italic, fontFamily);

            // Width(text) = Width(text + sentinel) - Width(sentinel). Doing it this way
            // means trailing whitespace inside `text` is no longer at the end of the
            // measured string and survives the trim.
            double sentinelWidth = MeasureRaw(SentinelChar.ToString());
            double withSentinel  = MeasureRaw(text + SentinelChar);
            return Math.Max(0, withSentinel - sentinelWidth);
        }

        internal static double MeasurePrefixWidth(Florence.FlorenceRun run, int charCount)
        {
            if (charCount <= 0) return 0;
            if (charCount >= run.Length) return run.Width;
            return MeasureWidth(run.Text.Substring(0, charCount),
                run.FontSize, run.Bold, run.Italic, run.FontFamily);
        }

        private static double MeasureRaw(string text)
        {
            _probe!.Text = text;
            _probe.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
            return _probe.DesiredSize.Width;
        }

        private static void ConfigureProbe(double fontSize, bool bold, bool italic,
            Microsoft.UI.Xaml.Media.FontFamily? fontFamily)
        {
            _probe ??= new Microsoft.UI.Xaml.Controls.TextBlock();
            _probe.FontSize = fontSize;
            _probe.FontWeight = bold
                ? Microsoft.UI.Text.FontWeights.Bold
                : Microsoft.UI.Text.FontWeights.Normal;
            _probe.FontStyle = italic
                ? Windows.UI.Text.FontStyle.Italic
                : Windows.UI.Text.FontStyle.Normal;
            if (fontFamily is not null)
                _probe.FontFamily = fontFamily;
            else
                _probe.ClearValue(Microsoft.UI.Xaml.Controls.TextBlock.FontFamilyProperty);
        }
    }

    internal static class FlorenceLayoutEngine
    {
        private const double DefaultFontSize = 14.0;
        private const double LineSpacing     = 1.2;  // line height = FontSize * LineSpacing

        /// <summary>
        /// Format the FlowDocument into a single bottomless FlorencePage.
        /// </summary>
        internal static FlorencePage Format(System.Windows.Documents.FlowDocument document, Windows.Foundation.Size constraint)
        {
            var page = new FlorencePage { PageSize = constraint };
            double availWidth = constraint.Width <= 0 ? 600 : constraint.Width;
            double y = 0;
            int globalOffset = 0;
            var paragraphs = document.Blocks.OfType<System.Windows.Documents.Paragraph>().ToList();

            for (int i = 0; i < paragraphs.Count; i++)
            {
                var para = paragraphs[i];
                FormatParagraph(para, availWidth, ref y, ref globalOffset, page);

                // WPF text model exposes an invisible paragraph boundary position
                // between neighboring paragraphs. Reserve one logical char slot so
                // Florence offsets align with TextContainer caret navigation.
                if (i < paragraphs.Count - 1)
                    globalOffset += 1;

                // paragraph spacing
                y += DefaultFontSize * 0.3;
            }

            page.PageSize = new Windows.Foundation.Size(availWidth, Math.Max(y, constraint.Height));
            return page;
        }

        private static void FormatParagraph(
            System.Windows.Documents.Paragraph para,
            double availWidth, ref double y, ref int globalOffset,
            FlorencePage page)
        {
            int paragraphStartOffset = globalOffset;
            var spans = CollectSpans(para.Inlines, DefaultFontSize, bold: false, italic: false, fontFamily: null);

            // Empty paragraph: emit a blank line so the cursor can be placed.
            if (spans.Count == 0 || spans.All(s => s.Text.Length == 0))
            {
                double lineH = DefaultFontSize * LineSpacing;
                var emptyRun = new FlorenceRun(globalOffset, 0, 0, 0, "", DefaultFontSize, false, false, null, null, false, null);
                var emptyLine = new FlorenceLine(globalOffset, 0, y, y + DefaultFontSize, lineH, "", new[] { emptyRun });
                page.AddLine(emptyLine);
                y += lineH;
                return;
            }

            double x = 0;
            double lineHeight = 0;
            var currentLineRuns = new List<(string text, double runX, double runWidth,
                int runStart, int runLen, double fontSize, bool bold, bool italic,
                Microsoft.UI.Xaml.Media.FontFamily? fontFamily,
                Microsoft.UI.Xaml.Media.Brush? foreground,
                bool underline,
                System.Windows.Documents.Hyperlink? hyperlink)>();
            int lineStart = globalOffset;
            string lineText = "";

            foreach (var span in spans)
            {
                string remaining = span.Text;
                int spanOffset = paragraphStartOffset + span.GlobalOffset;
                lineHeight = Math.Max(lineHeight, span.FontSize * LineSpacing);

                while (remaining.Length > 0)
                {
                    // Binary-search for how many characters fit on the remaining line width.
                    double spaceLeft = availWidth - x;
                    int fitChars = FindFitChars(remaining, spaceLeft, span.FontSize, span.Bold, span.Italic, span.FontFamily);

                    if (fitChars >= remaining.Length)
                    {
                        double w = TextMeasurer.MeasureWidth(remaining, span.FontSize, span.Bold, span.Italic, span.FontFamily);
                        currentLineRuns.Add((remaining, x, w, spanOffset, remaining.Length,
                            span.FontSize, span.Bold, span.Italic, span.FontFamily, span.Foreground, span.Underline, span.Hyperlink));
                        x += w;
                        lineText += remaining;
                        spanOffset += remaining.Length;
                        remaining = "";
                    }
                    else
                    {
                        int breakAt = FindWordBreak(remaining, fitChars);
                        string lineChunk = remaining[..breakAt];
                        double w = TextMeasurer.MeasureWidth(lineChunk, span.FontSize, span.Bold, span.Italic, span.FontFamily);
                        currentLineRuns.Add((lineChunk, x, w, spanOffset, lineChunk.Length,
                            span.FontSize, span.Bold, span.Italic, span.FontFamily, span.Foreground, span.Underline, span.Hyperlink));
                        lineText += lineChunk;
                        int consumed = breakAt;
                        while (consumed < remaining.Length && remaining[consumed] == ' ')
                            consumed++;
                        spanOffset += consumed;
                        remaining = remaining[consumed..];
                        globalOffset = spanOffset;
                        EmitLine(page, currentLineRuns, lineStart, lineText, y, lineHeight);
                        y += lineHeight;
                        lineStart = globalOffset;
                        lineText = "";
                        x = 0;
                        lineHeight = span.FontSize * LineSpacing;
                        currentLineRuns = new();
                    }
                }
                globalOffset = spanOffset;
            }

            if (currentLineRuns.Count > 0 || lineText.Length == 0)
            {
                EmitLine(page, currentLineRuns, lineStart, lineText, y,
                    lineHeight > 0 ? lineHeight : DefaultFontSize * LineSpacing);
                y += lineHeight > 0 ? lineHeight : DefaultFontSize * LineSpacing;
            }
        }

        private static void EmitLine(FlorencePage page,
            List<(string text, double runX, double runWidth, int runStart, int runLen,
                double fontSize, bool bold, bool italic,
                Microsoft.UI.Xaml.Media.FontFamily? fontFamily,
                Microsoft.UI.Xaml.Media.Brush? foreground,
                bool underline,
                System.Windows.Documents.Hyperlink? hyperlink)> runData,
            int lineStart, string lineText, double y, double lineHeight)
        {
            var runs = runData.Select(r => new FlorenceRun(r.runStart, r.runLen, r.runX, r.runWidth,
                r.text, r.fontSize, r.bold, r.italic, r.fontFamily, r.foreground, r.underline, r.hyperlink)).ToList();
            var line = new FlorenceLine(lineStart, lineText.Length, y, y + lineHeight * 0.8,
                lineHeight, lineText, runs);
            page.AddLine(line);
        }

        private static int FindFitChars(string text, double availWidth, double fontSize, bool bold, bool italic,
            Microsoft.UI.Xaml.Media.FontFamily? fontFamily)
        {
            if (availWidth <= 0) return 1;
            double totalWidth = TextMeasurer.MeasureWidth(text, fontSize, bold, italic, fontFamily);
            if (totalWidth <= availWidth) return text.Length;

            // Binary search
            int lo = 1, hi = text.Length - 1, best = 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                double w = TextMeasurer.MeasureWidth(text[..mid], fontSize, bold, italic, fontFamily);
                if (w <= availWidth) { best = mid; lo = mid + 1; }
                else hi = mid - 1;
            }
            return best;
        }

        private static int FindWordBreak(string text, int maxChars)
        {
            if (maxChars >= text.Length) return text.Length;
            for (int i = maxChars; i > 0; i--)
                if (text[i - 1] == ' ') return i;
            return maxChars;
        }

        private record SpanInfo(
            string Text, int GlobalOffset, double FontSize, bool Bold, bool Italic,
            Microsoft.UI.Xaml.Media.FontFamily? FontFamily,
            Microsoft.UI.Xaml.Media.Brush? Foreground,
            bool Underline,
            System.Windows.Documents.Hyperlink? Hyperlink);

        private static List<SpanInfo> CollectSpans(
            System.Windows.Documents.InlineCollection inlines,
            double fontSize, bool bold, bool italic,
            Microsoft.UI.Xaml.Media.FontFamily? fontFamily,
            Microsoft.UI.Xaml.Media.Brush? foreground = null,
            bool underline = false,
            System.Windows.Documents.Hyperlink? hyperlink = null)
        {
            var result = new List<SpanInfo>();
            int localOffset = 0;
            foreach (var inline in inlines)
            {
                double fs = double.IsNaN(inline.FontSize) || inline.FontSize <= 0 ? fontSize : inline.FontSize;
                bool isBold = bold || inline.FontWeight.Weight >= System.Windows.FontWeights.SemiBold.Weight;
                bool isItalic = italic || inline.FontStyle != System.Windows.FontStyles.Normal;
                // WPF inheritance: a Run/Bold/Italic/Span without an explicit FontFamily
                // inherits the parent's value. WPF's Inline.FontFamily DP defaults to a
                // non-null Segoe UI, so we treat "matches the inherited default" as
                // no-override and pass null down for the default branch.
                var ff = ResolveInheritedFontFamily(inline.FontFamily, fontFamily);
                var fg = inline.Foreground ?? foreground;
                bool hasUnderline = underline || HasUnderline(inline);
                var currentHyperlink = inline as System.Windows.Documents.Hyperlink ?? hyperlink;

                if (inline is System.Windows.Documents.Run run)
                {
                    string text = new System.Windows.Documents.TextRange(run.ContentStart, run.ContentEnd).Text;
                    result.Add(new SpanInfo(text, localOffset, fs, isBold, isItalic, ff, fg, hasUnderline, currentHyperlink));
                    localOffset += text.Length;
                }
                else if (inline is System.Windows.Documents.Hyperlink link)
                {
                    var sub = CollectSpans(link.Inlines, fs, isBold, isItalic, ff, fg, hasUnderline, link);
                    result.AddRange(sub);
                    localOffset += sub.Sum(s => s.Text.Length);
                }
                else if (inline is System.Windows.Documents.Bold b)
                {
                    var sub = CollectSpans(b.Inlines, fs, bold: true, isItalic, ff, fg, hasUnderline, currentHyperlink);
                    result.AddRange(sub);
                    localOffset += sub.Sum(s => s.Text.Length);
                }
                else if (inline is System.Windows.Documents.Italic it)
                {
                    var sub = CollectSpans(it.Inlines, fs, isBold, italic: true, ff, fg, hasUnderline, currentHyperlink);
                    result.AddRange(sub);
                    localOffset += sub.Sum(s => s.Text.Length);
                }
                else if (inline is System.Windows.Documents.Span sp)
                {
                    var sub = CollectSpans(sp.Inlines, fs, isBold, isItalic, ff, fg, hasUnderline, currentHyperlink);
                    result.AddRange(sub);
                    localOffset += sub.Sum(s => s.Text.Length);
                }
                else if (inline is System.Windows.Documents.LineBreak)
                {
                    result.Add(new SpanInfo("\n", localOffset, fs, isBold, isItalic, ff, fg, hasUnderline, currentHyperlink));
                    localOffset++;
                }
            }
            return result;
        }

        private static bool HasUnderline(System.Windows.Documents.Inline inline)
        {
            if (inline is System.Windows.Documents.Underline or System.Windows.Documents.Hyperlink)
            {
                return true;
            }

            object rawValue = inline.GetValue(System.Windows.Documents.Inline.TextDecorationsProperty);
            if (rawValue is not System.Windows.Media.TextDecorationCollection decorations || decorations.Count == 0)
            {
                return false;
            }

            return decorations.Any(d => d.Location == System.Windows.Media.TextDecorationLocation.Underline);
        }

        private static Microsoft.UI.Xaml.Media.FontFamily? ResolveInheritedFontFamily(
            Microsoft.UI.Xaml.Media.FontFamily? inlineValue,
            Microsoft.UI.Xaml.Media.FontFamily? inherited)
        {
            // WPF's Inline.FontFamily DP has the inheritable default "Segoe UI". When
            // the source value matches that default we cannot distinguish "explicitly
            // set to Segoe UI" from "inherited", so we prefer the parent's value to
            // preserve inheritance semantics from outer Bold/Italic/Span scopes.
            if (inlineValue is null) return inherited;
            string? source = inlineValue.Source;
            if (string.IsNullOrEmpty(source)) return inherited;
            if (inherited is null && string.Equals(source, "Segoe UI", StringComparison.OrdinalIgnoreCase))
                return null; // root default — let TextBlock resolve it
            return new Microsoft.UI.Xaml.Media.FontFamily(source);
        }
    }
}

// -----------------------------------------------------------------------
// PTS API surface — types referenced by upstream FlowDocument.cs and friends.
// Namespaces match the WPF originals so #if !HAS_UNO gates can be removed.
// -----------------------------------------------------------------------

namespace MS.Internal.PtsHost
{
    using MS.Internal.Florence;

    /// <summary>
    /// Florence implementation of WPF StructuralCache.
    /// Holds the document's TextContainer and the Florence layout document.
    /// IsFormattedOnce is false until a real layout pass runs.
    /// </summary>
    internal sealed class StructuralCache
    {
        private readonly FlorenceDocument _florenceDoc = new FlorenceDocument();

        internal StructuralCache(System.Windows.Documents.FlowDocument owner,
                                 System.Windows.Documents.TextContainer textContainer)
        {
            TextContainer = textContainer;
            TextFormatterHost = new FlorenceTextFormatterHost();
        }

        // ── Formatting state ────────────────────────────────────────────
        // IsFormattedOnce stays false until Florence runs a real layout pass.
        // All invalidation guards check this; they silently no-op while false.
        internal bool IsFormattedOnce           { get; set; }
        internal bool IsFormattingInProgress    { get; private set; }
        internal bool IsContentChangeInProgress { get; set; }
        internal bool ForceReformat             { get; set; }

        // ── Core data ───────────────────────────────────────────────────
        /// <summary>The WPF TextContainer for this document.</summary>
        internal System.Windows.Documents.TextContainer TextContainer { get; }

        /// <summary>The Florence layout document — populated by formatters.</summary>
        internal FlorenceDocument FlorenceDoc => _florenceDoc;

        internal FlorenceTextFormatterHost TextFormatterHost { get; }

        // ── PTS API ─────────────────────────────────────────────────────
        internal bool HasPtsContext() => false;  // PTS not available on HAS_UNO

        internal void AddDirtyTextRange(DirtyTextRange dtr)
        {
            // Mark layout as stale — a real pass will pick it up.
            _florenceDoc.Invalidate();
        }

        internal void InvalidateFormatCache(bool clearStructure)
        {
            _florenceDoc.Invalidate();
        }

        internal void OnInvalidOperationDetected()
        {
            // On HAS_UNO we never reach IsFormattingInProgress = true,
            // so this should not fire; log and continue.
            System.Diagnostics.Debug.WriteLine("[Florence] OnInvalidOperationDetected");
        }
    }

    /// <summary>
    /// Florence text formatter host — owns per-DPI rendering context.
    /// Currently a thin stub; expands as Florence gains a text-shaping pass.
    /// </summary>
    internal sealed class FlorenceTextFormatterHost
    {
        internal double PixelsPerDip { get; set; } = 1.0;
    }

    /// <summary>
    /// Marks a changed text span so Florence can do an incremental layout update.
    /// </summary>
    internal readonly struct DirtyTextRange
    {
        internal DirtyTextRange(int cpFirst, int cchAdded, int cchDeleted)
        {
            CpFirst    = cpFirst;
            CchAdded   = cchAdded;
            CchDeleted = cchDeleted;
        }

        internal int CpFirst    { get; }
        internal int CchAdded   { get; }
        internal int CchDeleted { get; }
    }
}

namespace MS.Internal.Documents
{
    using MS.Internal.Florence;

    /// <summary>
    /// Florence formatter interface — mirrors WPF IFlowDocumentFormatter.
    /// </summary>
    internal interface IFlowDocumentFormatter
    {
        void Suspend();
        void OnContentInvalidated(bool affectsLayout);
        void OnContentInvalidated(bool affectsLayout,
                                  ITextPointer start,
                                  ITextPointer end);
        bool IsLayoutDataValid { get; }
    }

    /// <summary>
    /// Florence bottomless formatter — used by RichTextBox (scrollable content).
    /// Layout is incremental: only dirty ranges are reflowed.
    /// Currently a stub; plug in a line-breaking pass here.
    /// </summary>
    internal sealed class FlowDocumentFormatter : IFlowDocumentFormatter
    {
        private readonly System.Windows.Documents.FlowDocument _owner;

        internal FlowDocumentFormatter(System.Windows.Documents.FlowDocument owner)
        {
            _owner = owner;
        }

        public void Suspend() { }

        public void OnContentInvalidated(bool affectsLayout)
            => OnContentInvalidated(affectsLayout, null, null);

        public void OnContentInvalidated(bool affectsLayout, ITextPointer start, ITextPointer end)
        {
            // TODO: schedule an incremental Florence layout pass over [start, end].
            // For now, mark as stale so IsLayoutDataValid stays false.
        }

        public bool IsLayoutDataValid => false;
    }

    /// <summary>
    /// Florence paginated formatter — used by FlowDocumentReader / printing.
    /// Each layout pass produces a list of FlorencePages.
    /// Currently a stub; plug in a pagination algorithm here.
    /// </summary>
    internal sealed class FlowDocumentPaginator
        : System.Windows.Documents.DocumentPaginator, IFlowDocumentFormatter
    {
        private readonly System.Windows.Documents.FlowDocument _owner;

        internal FlowDocumentPaginator(System.Windows.Documents.FlowDocument owner)
        {
            _owner = owner;
        }

        // ── DocumentPaginator ───────────────────────────────────────────
        public override bool IsPageCountValid => false;
        public override int  PageCount        => 0;
        private Size _pageSize;
        public override Size PageSize { get => _pageSize; set => _pageSize = value; }
        public override System.Windows.Documents.IDocumentPaginatorSource Source => _owner;

        // ── IFlowDocumentFormatter ──────────────────────────────────────
        public void Suspend() { }

        public void OnContentInvalidated(bool affectsLayout)
            => OnContentInvalidated(affectsLayout, null, null);

        public void OnContentInvalidated(bool affectsLayout, ITextPointer start, ITextPointer end)
        {
            // TODO: schedule a full Florence pagination pass.
        }

        public bool IsLayoutDataValid => false;
    }
}
