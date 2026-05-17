namespace System.Windows.Documents;

/// <summary>
/// Base class for rich text document elements.
/// Microsoft WPF source reference:
/// System/Windows/Documents/TextElement.cs (captured in docs/PROVENANCE.md).
/// </summary>
public abstract partial class TextElement : System.Windows.FrameworkContentElement, System.Windows.Input.IInputElement
{
    protected static readonly Microsoft.UI.Xaml.DependencyProperty DefaultStyleKeyProperty =
        Microsoft.UI.Xaml.DependencyProperty.Register("DefaultStyleKey", typeof(object), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(null));
    protected static readonly Microsoft.UI.Xaml.DependencyProperty FocusableProperty =
        Microsoft.UI.Xaml.DependencyProperty.Register("Focusable", typeof(bool), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(false));
    public static readonly Microsoft.UI.Xaml.DependencyProperty IsEnabledProperty =
        Microsoft.UI.Xaml.DependencyProperty.Register(
            "IsEnabled",
            typeof(bool),
            typeof(TextElement),
            new System.Windows.FrameworkPropertyMetadata(true));
    public static readonly Microsoft.UI.Xaml.DependencyProperty BackgroundProperty =
        Microsoft.UI.Xaml.DependencyProperty.Register(
            "Background",
            typeof(Brush),
            typeof(TextElement),
            new System.Windows.FrameworkPropertyMetadata(null));
    public static readonly Microsoft.UI.Xaml.DependencyProperty TextEffectsProperty =
        Microsoft.UI.Xaml.DependencyProperty.Register(
            "TextEffects",
            typeof(System.Windows.Media.TextEffectCollection),
            typeof(TextElement),
            new System.Windows.FrameworkPropertyMetadata(null));
    private readonly List<object> _children = [];
    private readonly TextPointer _contentStart;
    private readonly TextPointer _contentEnd;
    private readonly TextContainer _textContainer;

    protected TextElement()
    {
        _textContainer = new TextContainer(this);
        _contentStart = new TextPointer(this, ElementEdge.BeforeStart);
        _contentEnd = new TextPointer(this, ElementEdge.AfterEnd);
    }

    public TextPointer ContentStart => _contentStart;
    public TextPointer ContentEnd => _contentEnd;
    // IsEmpty: shim returns true when there are no logical children.
    internal bool IsEmpty => _children.Count == 0;
    // TextElementNode: WPF-internal node representing this element in the text tree.
    internal TextElementNode TextElementNode => _textElementNode ??= new TextElementNode(this);
    private TextElementNode? _textElementNode;
    public bool IsEnabled => IsEnabledCore;
    public TextPointer ElementStart => new(this, ElementEdge.BeforeStart);
    public TextPointer ElementEnd => new(this, ElementEdge.AfterEnd);
    public new Microsoft.UI.Xaml.DependencyObject? Parent { get; internal set; }
    internal TextContainer TextContainer => _textContainer;
    internal IReadOnlyList<object> ChildObjects => _children;
    protected internal virtual System.Collections.IEnumerator LogicalChildren => _children.GetEnumerator();
    internal TextElement? NextElement => GetSibling(1);
    internal TextElement? PreviousElement => GetSibling(-1);
    internal ITextLayoutHost? LayoutHost => _textContainer.LayoutHost;

    internal void Reposition(TextPointer start, TextPointer end)
    {
    }

    internal virtual void RepositionWithContent(TextPointer textPosition)
    {
        if (textPosition.Parent is TextElement parent)
        {
            parent.InsertLogicalChild(parent.GetInsertionIndex(textPosition), this);
        }
    }

    internal void AddLogicalChild(object child)
    {
        InsertLogicalChild(_children.Count, child);
    }

    internal void InsertLogicalChild(int index, object child)
    {
        if (child is TextElement textElement)
        {
            textElement.Parent = this;
            textElement.SetLayoutHostRecursive(LayoutHost);
        }

        ContainerTextElementField.SetValue(child, this);
        _children.Insert(index, child);
        LayoutHost?.InvalidateLayout();
    }

    internal bool RemoveLogicalChild(object child)
    {
        var removed = _children.Remove(child);
        if (removed)
        {
            if (child is TextElement textElement && ReferenceEquals(textElement.Parent, this))
            {
                textElement.Parent = null;
                textElement.SetLayoutHostRecursive(null);
            }

            ContainerTextElementField.ClearValue(child);
            LayoutHost?.InvalidateLayout();
        }

        return removed;
    }

    internal void ClearLogicalChildren()
    {
        foreach (var child in _children.ToArray())
        {
            RemoveLogicalChild(child);
        }
    }

    internal int IndexOfLogicalChild(object child) => _children.IndexOf(child);

    private int GetInsertionIndex(TextPointer textPosition)
    {
        if (textPosition.Parent is not TextElement owner || !ReferenceEquals(owner, this))
        {
            return _children.Count;
        }

        var anchor = textPosition.Owner;
        if (anchor is null)
        {
            return _children.Count;
        }

        var index = _children.IndexOf(anchor);
        if (index < 0)
        {
            return _children.Count;
        }

        return textPosition.Edge == ElementEdge.BeforeStart ? index : index + 1;
    }

    internal void SetLayoutHostRecursive(ITextLayoutHost? host)
    {
        _textContainer.LayoutHost = host;

        foreach (var child in _children.OfType<TextElement>())
        {
            child.SetLayoutHostRecursive(host);
        }
    }

    private TextElement? GetSibling(int offset)
    {
        if (Parent is not TextElement parent)
        {
            return null;
        }

        var index = parent.IndexOfLogicalChild(this);
        if (index < 0)
        {
            return null;
        }

        index += offset;
        return index >= 0 && index < parent._children.Count ? parent._children[index] as TextElement : null;
    }

    internal static TextElement GetCommonAncestor(TextElement element1, TextElement element2)
    {
        return element1;
    }

    internal virtual void OnTextUpdated()
    {
    }

    internal virtual void BeforeLogicalTreeChange()
    {
    }

    internal virtual void AfterLogicalTreeChange()
    {
    }

    internal virtual void OnNewParent(Microsoft.UI.Xaml.DependencyObject newParent)
    {
        Parent = newParent;
    }

    internal virtual int EffectiveValuesInitialSize => 0;

    internal virtual bool IsIMEStructuralElement => false;

    protected virtual bool IsEnabledCore => true;

    protected internal virtual void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
    {
    }

    protected internal virtual void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
    {
    }

    protected internal virtual void OnKeyDown(System.Windows.KeyEventArgs e)
    {
    }

    protected virtual System.Windows.Automation.Peers.AutomationPeer? OnCreateAutomationPeer()
    {
        return null;
    }

    internal void NotifyTypographicPropertyChanged(bool affectsMeasureOrArrange, bool localValueChanged, Microsoft.UI.Xaml.DependencyProperty? property)
    {
        LayoutHost?.InvalidateLayout();
    }

    internal virtual System.Windows.DependencyObjectType? DTypeThemeStyleKey => null;
}
