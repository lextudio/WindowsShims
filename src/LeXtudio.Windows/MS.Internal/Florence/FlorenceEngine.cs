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

    /// <summary>A single laid-out line within a page.</summary>
    internal sealed class FlorenceLine
    {
        internal FlorenceLine(int startOffset, int length, double baseline, double height)
        {
            StartOffset = startOffset;
            Length      = length;
            Baseline    = baseline;
            Height      = height;
        }

        internal int    StartOffset { get; }
        internal int    Length      { get; }
        internal double Baseline    { get; }
        internal double Height      { get; }
    }

    /// <summary>A single laid-out page within a document.</summary>
    internal sealed class FlorencePage
    {
        private readonly List<FlorenceLine> _lines = new List<FlorenceLine>();

        internal Size PageSize  { get; set; }
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
