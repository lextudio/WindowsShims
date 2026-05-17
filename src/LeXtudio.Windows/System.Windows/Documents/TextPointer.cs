namespace System.Windows.Documents;

public sealed class TextPointer : ITextPointer
{
    private readonly TextElement? _owner;
    private readonly ElementEdge _edge;

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

    public TextContainer TextContainer { get; }
    public object? Parent { get; set; }
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

    public void MoveToNextContextPosition(LogicalDirection direction)
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

public class TextContainer
{
    private readonly TextElement? _owner;

    public TextContainer(TextElement? owner = null)
    {
        _owner = owner;
        Parent = owner;
    }

    public object? Parent { get; set; }
    public ITextLayoutHost? LayoutHost { get; set; }
    public TextSelectionShim? TextSelection { get; set; }

    public void BeginChange()
    {
    }

    public void EndChange()
    {
    }

    public void DeleteContentInternal(TextPointer start, TextPointer end)
    {
        _owner?.ClearLogicalChildren();
        LayoutHost?.InvalidateLayout();
    }
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
