namespace System.Windows.Documents;

// Uno-specific partial additions to WPF TextElement.
// WPF TextElement.cs registers FontFamily/Size/Style/Weight/Stretch/Foreground/Background/TextEffects
// — those are NOT re-registered here.  Only WinUI-specific properties and the Uno layout-host
// infrastructure that WPF TextElement does not have are added in this file.
public abstract partial class TextElement : System.Windows.Input.IInputElement
{
    // ───── WinUI-specific DependencyProperties (not in WPF TextElement) ─────

    protected static readonly DependencyProperty DefaultStyleKeyProperty =
        DependencyProperty.Register("DefaultStyleKey", typeof(object), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(null));
    protected static readonly DependencyProperty FocusableProperty =
        DependencyProperty.Register("Focusable", typeof(bool), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(false));
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.Register("IsEnabled", typeof(bool), typeof(TextElement), new FrameworkPropertyMetadata(true));

    public static readonly DependencyProperty AccessKeyProperty =
        DependencyProperty.Register(nameof(AccessKey), typeof(string), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(string.Empty));
    public static readonly DependencyProperty AccessKeyScopeOwnerProperty =
        DependencyProperty.Register(nameof(AccessKeyScopeOwner), typeof(DependencyObject), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(null));
    public static readonly DependencyProperty AllowFocusOnInteractionProperty =
        DependencyProperty.Register(nameof(AllowFocusOnInteraction), typeof(bool), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(true));
    public static readonly DependencyProperty CharacterSpacingProperty =
        DependencyProperty.Register(nameof(CharacterSpacing), typeof(int), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(0));
    public static readonly DependencyProperty ExitDisplayModeOnAccessKeyInvokedProperty =
        DependencyProperty.Register(nameof(ExitDisplayModeOnAccessKeyInvoked), typeof(bool), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(true));
    public static readonly DependencyProperty HorizontalTextAlignmentProperty =
        DependencyProperty.Register(nameof(HorizontalTextAlignment), typeof(TextAlignment), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(TextAlignment.Left));
    public static readonly DependencyProperty IsAccessKeyScopeProperty =
        DependencyProperty.Register(nameof(IsAccessKeyScope), typeof(bool), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(false));
    public static readonly DependencyProperty IsTextScaleFactorEnabledProperty =
        DependencyProperty.Register(nameof(IsTextScaleFactorEnabled), typeof(bool), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(true));
    public static readonly DependencyProperty KeyTipHorizontalOffsetProperty =
        DependencyProperty.Register(nameof(KeyTipHorizontalOffset), typeof(double), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(0d));
    public static readonly DependencyProperty KeyTipPlacementModeProperty =
        DependencyProperty.Register(nameof(KeyTipPlacementMode), typeof(Microsoft.UI.Xaml.Input.KeyTipPlacementMode), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(Microsoft.UI.Xaml.Input.KeyTipPlacementMode.Auto));
    public static readonly DependencyProperty KeyTipVerticalOffsetProperty =
        DependencyProperty.Register(nameof(KeyTipVerticalOffset), typeof(double), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(0d));
    public static readonly DependencyProperty LanguageProperty =
        DependencyProperty.Register(nameof(Language), typeof(string), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(string.Empty));

    // ───── WinUI-specific CLR wrappers ─────

    public string AccessKey
    {
        get => (string?)GetValue(AccessKeyProperty) ?? string.Empty;
        set => SetValue(AccessKeyProperty, value);
    }
    public DependencyObject AccessKeyScopeOwner
    {
        get => (DependencyObject)GetValue(AccessKeyScopeOwnerProperty);
        set => SetValue(AccessKeyScopeOwnerProperty, value);
    }
    public bool AllowFocusOnInteraction
    {
        get => (bool)GetValue(AllowFocusOnInteractionProperty);
        set => SetValue(AllowFocusOnInteractionProperty, value);
    }
    public int CharacterSpacing
    {
        get => (int)GetValue(CharacterSpacingProperty);
        set => SetValue(CharacterSpacingProperty, value);
    }
    public bool ExitDisplayModeOnAccessKeyInvoked
    {
        get => (bool)GetValue(ExitDisplayModeOnAccessKeyInvokedProperty);
        set => SetValue(ExitDisplayModeOnAccessKeyInvokedProperty, value);
    }
    public TextAlignment HorizontalTextAlignment
    {
        get => (TextAlignment)GetValue(HorizontalTextAlignmentProperty);
        set => SetValue(HorizontalTextAlignmentProperty, value);
    }
    public bool IsAccessKeyScope
    {
        get => (bool)GetValue(IsAccessKeyScopeProperty);
        set => SetValue(IsAccessKeyScopeProperty, value);
    }
    public bool IsEnabled => IsEnabledCore;
    public bool IsTextScaleFactorEnabled
    {
        get => (bool)GetValue(IsTextScaleFactorEnabledProperty);
        set => SetValue(IsTextScaleFactorEnabledProperty, value);
    }
    public double KeyTipHorizontalOffset
    {
        get => (double)GetValue(KeyTipHorizontalOffsetProperty);
        set => SetValue(KeyTipHorizontalOffsetProperty, value);
    }
    public Microsoft.UI.Xaml.Input.KeyTipPlacementMode KeyTipPlacementMode
    {
        get => (Microsoft.UI.Xaml.Input.KeyTipPlacementMode)GetValue(KeyTipPlacementModeProperty);
        set => SetValue(KeyTipPlacementModeProperty, value);
    }
    public double KeyTipVerticalOffset
    {
        get => (double)GetValue(KeyTipVerticalOffsetProperty);
        set => SetValue(KeyTipVerticalOffsetProperty, value);
    }
    public string Language
    {
        get => (string?)GetValue(LanguageProperty) ?? string.Empty;
        set => SetValue(LanguageProperty, value);
    }

    // WPF's TextElement.Parent comes from FrameworkContentElement (DependencyObject?).
    // We shadow it with a more specific type to match our usage pattern.
    public new DependencyObject? Parent
    {
        get => base.Parent;
        internal set => base.Parent = value;
    }

    // ───── Uno layout-host infrastructure (not in WPF TextElement) ─────

    private readonly List<object> _children = [];
    private ITextLayoutHost? _layoutHost;

    internal ITextLayoutHost? LayoutHost => _layoutHost;

    internal void SetLayoutHostRecursive(ITextLayoutHost? host)
    {
        _layoutHost = host;
        foreach (var child in _children.OfType<TextElement>())
            child.SetLayoutHostRecursive(host);
    }

    internal IReadOnlyList<object> ChildObjects => _children;

    internal void AddLogicalChild(object child) => InsertLogicalChild(_children.Count, child);

    internal void InsertLogicalChild(int index, object child)
    {
        if (child is TextElement te)
        {
            te.Parent = this;
            te.SetLayoutHostRecursive(LayoutHost);
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
            if (child is TextElement te && ReferenceEquals(te.Parent, this))
            {
                te.Parent = null;
                te.SetLayoutHostRecursive(null);
            }
            ContainerTextElementField.ClearValue(child);
            LayoutHost?.InvalidateLayout();
        }
        return removed;
    }

    internal void ClearLogicalChildren()
    {
        foreach (var child in _children.ToArray())
            RemoveLogicalChild(child);
    }

    internal int IndexOfLogicalChild(object child) => _children.IndexOf(child);

    // ───── Misc Uno additions ─────

    public ResourceDictionary? Resources { get; set; }
    public string Name => string.Empty;
    public Microsoft.UI.Xaml.XamlRoot XamlRoot { get; set; }

    protected virtual bool IsEnabledCore => true;

    // Virtual input handlers — defined here because ContentElement (WPF-only) isn't in our shim
    protected internal virtual void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e) { }
    protected internal virtual void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e) { }
    protected internal virtual void OnKeyDown(System.Windows.KeyEventArgs e) { }

    internal override int EffectiveValuesInitialSize => 0;
    internal override DependencyObjectType? DTypeThemeStyleKey => null;

    public event global::Windows.Foundation.TypedEventHandler<TextElement, Microsoft.UI.Xaml.Input.AccessKeyDisplayDismissedEventArgs> AccessKeyDisplayDismissed;
    public event global::Windows.Foundation.TypedEventHandler<TextElement, Microsoft.UI.Xaml.Input.AccessKeyDisplayRequestedEventArgs> AccessKeyDisplayRequested;
    public event global::Windows.Foundation.TypedEventHandler<TextElement, Microsoft.UI.Xaml.Input.AccessKeyInvokedEventArgs> AccessKeyInvoked;

    public object FindName(string name) => null;

    public bool Equals(TextElement other) => ReferenceEquals(this, other);
    public override bool Equals(object obj) => ReferenceEquals(this, obj);
    public override int GetHashCode() => base.GetHashCode();

    protected void RaiseAccessKeyDisplayDismissed(Microsoft.UI.Xaml.Input.AccessKeyDisplayDismissedEventArgs args) =>
        AccessKeyDisplayDismissed?.Invoke(this, args);
    protected void RaiseAccessKeyDisplayRequested(Microsoft.UI.Xaml.Input.AccessKeyDisplayRequestedEventArgs args) =>
        AccessKeyDisplayRequested?.Invoke(this, args);
    protected void RaiseAccessKeyInvoked(Microsoft.UI.Xaml.Input.AccessKeyInvokedEventArgs args) =>
        AccessKeyInvoked?.Invoke(this, args);

    // ───── IAddChild (Uno implementation uses _children list) ─────

    void System.Windows.Markup.IAddChild.AddChild(object value)
    {
        if (value is TextElement te)
            AddLogicalChild(te);
        else if (value is UIElement uie)
            AddLogicalChild(uie);
    }

    void System.Windows.Markup.IAddChild.AddText(string text)
    {
        AddLogicalChild(new Run { Text = text });
    }

    // LogicalChildren via _children list for Uno
    protected internal override System.Collections.IEnumerator LogicalChildren =>
        _children.GetEnumerator();

    // ───── IInputElement (not on WPF TextElement, added for WinUI compat) ─────

    bool System.Windows.Input.IInputElement.Focus() => true;
    void System.Windows.Input.IInputElement.RaiseEvent(RoutedEventArgs e) =>
        ((DependencyObject)this).RaiseEvent(e);
    void System.Windows.Input.IInputElement.AddHandler(RoutedEvent routedEvent, Delegate handler) =>
        ((DependencyObject)this).AddHandler(routedEvent, handler);
    void System.Windows.Input.IInputElement.RemoveHandler(RoutedEvent routedEvent, Delegate handler) =>
        ((DependencyObject)this).RemoveHandler(routedEvent, handler);
    bool System.Windows.Input.IInputElement.IsMouseCaptured =>
        ((DependencyObject)this).IsMouseCaptured;
    bool System.Windows.Input.IInputElement.IsMouseOver =>
        ((DependencyObject)this).IsMouseOver;
    void System.Windows.Input.IInputElement.ReleaseMouseCapture() =>
        ((DependencyObject)this).ReleaseMouseCapture();
}
