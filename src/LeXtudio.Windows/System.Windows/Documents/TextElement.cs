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
    private ITextLayoutHost? _layoutHost;
    private TextTreeTextElementNode? _textElementNode;
    private TextContainer? _ownedContainer;

    protected TextElement()
    {
    }

    public TextPointer ContentStart
    {
        get
        {
            var tree = EnsureContainer();
            var ptr = new TextPointer(tree, _textElementNode!, ElementEdge.AfterStart, LogicalDirection.Backward);
            ptr.Freeze();
            return ptr;
        }
    }

    public TextPointer ContentEnd
    {
        get
        {
            var tree = EnsureContainer();
            var ptr = new TextPointer(tree, _textElementNode!, ElementEdge.BeforeEnd, LogicalDirection.Forward);
            ptr.Freeze();
            return ptr;
        }
    }

    // IsEmpty: shim returns true when there are no logical children.
    internal bool IsEmpty => _children.Count == 0;

    internal TextTreeTextElementNode? TextElementNode
    {
        get => _textElementNode;
        set => _textElementNode = value;
    }

    internal virtual bool IsFirstIMEVisibleSibling
        => _textElementNode == null || _textElementNode.IsFirstSibling;

    public bool IsEnabled => IsEnabledCore;
    public TextPointer ElementStart
    {
        get
        {
            var tree = EnsureContainer();
            return new TextPointer(tree, _textElementNode!, ElementEdge.BeforeStart, LogicalDirection.Backward);
        }
    }

    public TextPointer ElementEnd
    {
        get
        {
            var tree = EnsureContainer();
            return new TextPointer(tree, _textElementNode!, ElementEdge.AfterEnd, LogicalDirection.Forward);
        }
    }

    public new Microsoft.UI.Xaml.DependencyObject? Parent { get; internal set; }
    internal TextContainer TextContainer => EnsureContainer();
    internal IReadOnlyList<object> ChildObjects => _children;
    protected internal virtual System.Collections.IEnumerator LogicalChildren => _children.GetEnumerator();
    internal TextElement? NextElement => GetSibling(1);
    internal TextElement? PreviousElement => GetSibling(-1);
    internal ITextLayoutHost? LayoutHost => _layoutHost;

    internal void Reposition(TextPointer start, TextPointer end)
    {
    }

    internal virtual void RepositionWithContent(TextPointer textPosition)
    {
        if (textPosition.Parent is TextElement parent)
        {
            parent.AddLogicalChild(this);
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

    internal void SetLayoutHostRecursive(ITextLayoutHost? host)
    {
        _layoutHost = host;

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

    public ResourceDictionary? Resources { get; set; }

    // Ensure this element has a TextContainer. If already in a tree, returns that tree's container.
    // If not, creates an owned container and inserts this element into it.
    private TextContainer EnsureContainer()
    {
        if (_textElementNode != null)
        {
            return _textElementNode.GetTextTree();
        }
        if (_ownedContainer == null)
        {
            _ownedContainer = new TextContainer(null, false);
            _ownedContainer.BeginChange();
            try
            {
                _ownedContainer.InsertElementInternal(_ownedContainer.Start, _ownedContainer.Start, this);
            }
            finally
            {
                _ownedContainer.EndChange();
            }
        }
        return _ownedContainer;
    }
}
