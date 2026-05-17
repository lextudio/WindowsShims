namespace System.Windows.Documents;

public sealed class TextPointer : ITextPointer
{
    private readonly TextElement? _owner;
    private readonly ElementEdge _edge;
    private LogicalDirection _logicalDirection = LogicalDirection.Forward;

    public TextPointer()
    {
        TextContainer = new TextContainer(null);
    }

    internal TextPointer(TextElement owner, ElementEdge edge)
    {
        _owner = owner;
        _edge = edge;
        TextContainer = owner.TextContainer;
        Parent = owner;
        ParentType = owner.GetType();
        Paragraph = owner as Paragraph ?? owner.Parent as Paragraph;
    }

    // Used by WPF Table collection internals when positioning insertion points.
    internal TextPointer(TextContainer container, TextElementNode? node, ElementEdge edge, LogicalDirection direction)
    {
        _owner = node?.Element;
        _edge = edge;
        TextContainer = container;
        Parent = _owner;
        ParentType = _owner?.GetType();
        Paragraph = _owner as Paragraph ?? _owner?.Parent as Paragraph;
    }

    // Used when constructing a pointer relative to another by offset.
    internal TextPointer(TextPointer other, int offset)
    {
        _owner = other._owner;
        _edge = other._edge;
        TextContainer = other.TextContainer;
        Parent = other.Parent;
        ParentType = other.ParentType;
        Paragraph = other.Paragraph;
    }

    // Copy ctor used by upstream RangeContentEnumerator (`new TextPointer(_start)`).
    internal TextPointer(TextPointer other) : this(other, 0) { }

    public TextContainer TextContainer { get; }
    public LogicalDirection LogicalDirection => _logicalDirection;
    public bool IsFrozen => false;
    public bool HasValidLayout => true;
    public int Offset => 0;
    public DependencyObject? Parent { get; set; }
    public System.Type? ParentType { get; set; }
    public Paragraph? Paragraph { get; set; }
    internal TextElement? Owner => _owner;
    internal ElementEdge Edge => _edge;
    internal TextElement? DocumentRoot => GetDocumentRoot();

    public void InsertInline(Inline inline)
    {
        _owner?.AddLogicalChild(inline);
    }

    public int CompareTo(TextPointer other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (ReferenceEquals(this, other))
        {
            return 0;
        }

        if (!IsInSameDocument(other))
        {
            throw new ArgumentException(SR.InDifferentTextContainers, nameof(other));
        }

        if (ReferenceEquals(_owner, other._owner))
        {
            if (_edge == other._edge)
            {
                return 0;
            }

            return _edge == ElementEdge.BeforeStart ? -1 : 1;
        }

        return CompareDistinctOwners(other);
    }

    public int CompareTo(ITextPointer position) => position is TextPointer pointer ? CompareTo(pointer) : 0;

    public TextPointerContext GetPointerContext(LogicalDirection direction)
    {
        var adjacent = GetAdjacentElement(direction);
        return adjacent switch
        {
            null => TextPointerContext.None,
            TextElement => direction == LogicalDirection.Forward ? TextPointerContext.ElementStart : TextPointerContext.ElementEnd,
            _ => TextPointerContext.EmbeddedElement
        };
    }

    public TextPointer GetNextContextPosition(LogicalDirection direction) => this;

    ITextPointer? ITextPointer.GetNextContextPosition(LogicalDirection direction) => GetNextContextPosition(direction);

    public Inline? GetNonMergeableInlineAncestor() => null;

    public object? GetAdjacentElement(LogicalDirection direction)
    {
        if (_owner is null)
        {
            return null;
        }

        if (_edge is ElementEdge.BeforeStart && direction is LogicalDirection.Forward)
        {
            return _owner.ChildObjects.FirstOrDefault();
        }

        if (_edge is ElementEdge.AfterEnd && direction is LogicalDirection.Backward)
        {
            return _owner.ChildObjects.LastOrDefault();
        }

        if (_owner.Parent is TextElement parent)
        {
            var index = parent.IndexOfLogicalChild(_owner);
            if (index >= 0)
            {
                var siblingIndex = direction is LogicalDirection.Forward ? index + 1 : index - 1;
                if (siblingIndex >= 0 && siblingIndex < parent.ChildObjects.Count)
                {
                    return parent.ChildObjects[siblingIndex];
                }
            }
        }

        return null;
    }

    internal object? GetAdjacentElementFromOuterPosition(LogicalDirection direction) => GetAdjacentElement(direction);

    public TextPointer CreatePointer() => this;

    ITextPointer ITextPointer.CreatePointer() => CreatePointer();

    public ITextPointer CreatePointer(int distance) => new TextPointer(this, distance);

    public ITextPointer CreatePointer(LogicalDirection gravity)
    {
        var pointer = new TextPointer(this, 0);
        pointer._logicalDirection = gravity;
        return pointer;
    }

    public ITextPointer GetFrozenPointer(LogicalDirection logicalDirection) => CreatePointer(logicalDirection);

    public ITextPointer? GetNextInsertionPosition(LogicalDirection direction) => this;

    public ITextPointer GetInsertionPosition(LogicalDirection direction) => this;

    public bool MoveToInsertionPosition(LogicalDirection direction)
    {
        _logicalDirection = direction;
        return true;
    }

    public bool MoveToNextInsertionPosition(LogicalDirection direction)
    {
        _logicalDirection = direction;
        return true;
    }

    public void MoveToNextContextPosition(LogicalDirection direction)
    {
        _logicalDirection = direction;
    }

    public void SetLogicalDirection(LogicalDirection direction)
    {
        _logicalDirection = direction;
    }

    public string GetTextInRun(LogicalDirection direction)
    {
        _logicalDirection = direction;
        return _owner is Run run ? run.Text : string.Empty;
    }

    public int GetTextInRun(LogicalDirection direction, char[] textBuffer, int startIndex, int count)
    {
        var text = GetTextInRun(direction);
        if (string.IsNullOrEmpty(text) || count <= 0)
        {
            return 0;
        }

        var copied = Math.Min(count, text.Length);
        text.CopyTo(0, textBuffer, startIndex, copied);
        return copied;
    }

    // Length of the contiguous text run in the given direction. Without a real
    // text-element walk we expose the single Run's text length when applicable.
    public int GetTextRunLength(LogicalDirection direction)
    {
        _logicalDirection = direction;
        return _owner is Run run ? run.Text?.Length ?? 0 : 0;
    }

    // No-op pointer movements: the shim doesn't move through the tree, but
    // upstream callers depend on these existing on the type.
    internal void MoveToElementEdge(ElementEdge edge)
    {
    }

    public void MoveToPosition(ITextPointer position)
    {
    }

    public Type? GetElementType(LogicalDirection direction)
    {
        _logicalDirection = direction;
        return ParentType;
    }

    public bool ValidateLayout()
    {
        return true;
    }

    public void Freeze()
    {
    }

    public void InsertUIElement(object element)
    {
        _owner?.AddLogicalChild(element);
    }

    public void InsertTextInRun(string text)
    {
        if (_owner is Run run)
        {
            run.Text = text;
        }
    }

    internal FrameworkElement? ContainingFrameworkElement => null;

    internal Block? ParagraphOrBlockUIContainer => Paragraph;

    internal void InsertTextElement(TextElement element)
    {
        _owner?.AddLogicalChild(element);
    }

    internal bool IsInSameDocument(TextPointer? other)
        => other is not null && ReferenceEquals(DocumentRoot, other.DocumentRoot);

    private int CompareDistinctOwners(TextPointer other)
    {
        var commonAncestor = GetCommonAncestor(_owner, other._owner);
        if (commonAncestor is null)
        {
            throw new ArgumentException(SR.InDifferentTextContainers, nameof(other));
        }

        var firstChild = GetImmediateChild(commonAncestor, _owner);
        var secondChild = GetImmediateChild(commonAncestor, other._owner);
        if (firstChild is null || secondChild is null)
        {
            throw new ArgumentException(SR.BadTextPositionOrder, nameof(other));
        }

        var firstIndex = commonAncestor.IndexOfLogicalChild(firstChild);
        var secondIndex = commonAncestor.IndexOfLogicalChild(secondChild);
        return firstIndex.CompareTo(secondIndex);
    }

    private TextElement? GetDocumentRoot()
    {
        var current = _owner;
        while (current?.Parent is TextElement parent)
        {
            current = parent;
        }

        return current;
    }

    private static TextElement? GetCommonAncestor(TextElement? first, TextElement? second)
    {
        if (first is null || second is null)
        {
            return null;
        }

        HashSet<TextElement> ancestors = [];
        for (var current = first; current is not null; current = current.Parent as TextElement)
        {
            ancestors.Add(current);
        }

        for (var current = second; current is not null; current = current.Parent as TextElement)
        {
            if (ancestors.Contains(current))
            {
                return current;
            }
        }

        return null;
    }

    private static TextElement? GetImmediateChild(TextElement ancestor, TextElement? descendant)
    {
        var current = descendant;
        while (current is not null)
        {
            if (ReferenceEquals(current.Parent, ancestor))
            {
                return current;
            }

            current = current.Parent as TextElement;
        }

        return null;
    }
}

public class TextContainer : ITextContainer
{
    private readonly TextElement? _owner;
    private readonly Highlights _highlights;
    private readonly MS.Internal.Documents.UndoManager _undoManager = new();
    private ITextSelection? _iTextSelection;
    private ITextView? _iTextView;

    public TextContainer(TextElement? owner = null)
    {
        _owner = owner;
        _highlights = new Highlights(this);
        Parent = owner;
    }

    public object? Parent { get; set; }
    public ITextLayoutHost? LayoutHost { get; set; }

    // Public mirror of ITextContainer.Generation; always 0 — we don't track edits
    // by generation. Stale-tree guards in upstream code compare against this and
    // see "unchanged", which is the right behavior for the shim baseline.
    public uint Generation => 0;
    public TextSelectionShim? TextSelection { get; set; }
    internal Highlights Highlights => _highlights;
    public TextPointer Start => new(this, null, ElementEdge.BeforeStart, LogicalDirection.Forward);
    public TextPointer End => new(this, null, ElementEdge.AfterEnd, LogicalDirection.Backward);

    public void BeginChange()
    {
    }

    public void EndChange()
    {
    }

    void ITextContainer.BeginChangeNoUndo()
    {
    }

    void ITextContainer.EndChange(bool skipEvents)
    {
    }

    ITextPointer ITextContainer.CreatePointerAtOffset(int offset, LogicalDirection direction)
        => Start.CreatePointer(offset);

    ITextPointer ITextContainer.CreatePointerAtCharOffset(int charOffset, LogicalDirection direction)
        => Start.CreatePointer(charOffset);

    ITextPointer ITextContainer.CreateDynamicTextPointer(StaticTextPointer position, LogicalDirection direction)
        => Start.CreatePointer(direction);

    StaticTextPointer ITextContainer.CreateStaticPointerAtOffset(int offset)
        => new(this, null, offset);

    TextPointerContext ITextContainer.GetPointerContext(StaticTextPointer pointer, LogicalDirection direction)
        => TextPointerContext.None;

    int ITextContainer.GetOffsetToPosition(StaticTextPointer position1, StaticTextPointer position2)
        => 0;

    int ITextContainer.GetTextInRun(StaticTextPointer position, LogicalDirection direction, char[] textBuffer, int startIndex, int count)
        => 0;

    object ITextContainer.GetAdjacentElement(StaticTextPointer position, LogicalDirection direction)
        => DependencyProperty.UnsetValue;

    DependencyObject ITextContainer.GetParent(StaticTextPointer position)
        => (Parent as DependencyObject) ?? new FormattingDependencyObject();

    StaticTextPointer ITextContainer.CreatePointer(StaticTextPointer position, int offset)
        => position;

    StaticTextPointer ITextContainer.GetNextContextPosition(StaticTextPointer position, LogicalDirection direction)
        => position;

    int ITextContainer.CompareTo(StaticTextPointer position1, StaticTextPointer position2)
        => 0;

    int ITextContainer.CompareTo(StaticTextPointer position1, ITextPointer position2)
        => 0;

    object ITextContainer.GetValue(StaticTextPointer position, DependencyProperty formattingProperty)
        => DependencyProperty.UnsetValue;

    bool ITextContainer.IsReadOnly => false;

    ITextPointer ITextContainer.Start => Start;

    ITextPointer ITextContainer.End => End;

    DependencyObject ITextContainer.Parent => (Parent as DependencyObject) ?? new FormattingDependencyObject();

    Highlights ITextContainer.Highlights => _highlights;

    ITextSelection ITextContainer.TextSelection
    {
        get => _iTextSelection!;
        set => _iTextSelection = value;
    }

    MS.Internal.Documents.UndoManager ITextContainer.UndoManager => _undoManager;

    ITextView ITextContainer.TextView
    {
        get => _iTextView!;
        set => _iTextView = value;
    }

    int ITextContainer.SymbolCount => 0;

    int ITextContainer.IMECharCount => 0;

    uint ITextContainer.Generation => 0;

    event EventHandler ITextContainer.Changing
    {
        add { }
        remove { }
    }

    event TextContainerChangeEventHandler ITextContainer.Change
    {
        add { }
        remove { }
    }

    event TextContainerChangedEventHandler ITextContainer.Changed
    {
        add { }
        remove { }
    }

    public void DeleteContentInternal(TextPointer start, TextPointer end)
    {
        _owner?.ClearLogicalChildren();
        LayoutHost?.InvalidateLayout();
    }
}

internal static class TextPointerBase
{
    internal static bool IsAtLineWrappingPosition(ITextPointer position, object? textView) => false;

    internal static bool IsNextToPlainLineBreak(ITextPointer position, LogicalDirection direction) => false;

    internal static bool IsAtRowEnd(ITextPointer position) => false;

    internal static TextSegment GetWordRange(ITextPointer position) => new(position, position);

    internal static TextSegment GetWordRange(ITextPointer position, LogicalDirection direction) => new(position, position);

    internal static bool IsAtWordBoundary(ITextPointer position, LogicalDirection insideWordDirection) => false;

    internal static bool IsAfterLastParagraph(ITextPointer position) => false;

    internal static bool IsNextToAnyBreak(ITextPointer position, LogicalDirection direction) => false;

    internal static ITextPointer GetFollowingNonMergeableInlineContentStart(ITextPointer position) => position;
}

public sealed class TextSelectionShim
{
    public TextEditorShim TextEditor { get; } = new();
}

public sealed class TextEditorShim
{
    public bool IsReadOnly { get; set; }
    public object? _cursor { get; set; }
}
