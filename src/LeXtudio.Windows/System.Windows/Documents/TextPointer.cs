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

    public void InsertInline(Inline inline)
    {
        _owner?.AddLogicalChild(inline);
    }

    public int CompareTo(TextPointer other) => ReferenceEquals(_owner, other._owner) && _edge == other._edge ? 0 : -1;
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
