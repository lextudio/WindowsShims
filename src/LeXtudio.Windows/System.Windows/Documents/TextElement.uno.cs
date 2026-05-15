namespace System.Windows.Documents;

// Explicit IInputElement implementations — extension members on DependencyObject cannot
// satisfy interface contracts, so they must be provided as real methods.
public abstract partial class TextElement : System.Windows.Input.IInputElement
{
    public static DependencyProperty AccessKeyProperty { get; } =
        DependencyProperty.Register(nameof(AccessKey), typeof(string), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(string.Empty));

    public static DependencyProperty AccessKeyScopeOwnerProperty { get; } =
        DependencyProperty.Register(nameof(AccessKeyScopeOwner), typeof(DependencyObject), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(null));

    public static DependencyProperty AllowFocusOnInteractionProperty { get; } =
        DependencyProperty.Register(nameof(AllowFocusOnInteraction), typeof(bool), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(true));

    public static DependencyProperty CharacterSpacingProperty { get; } =
        DependencyProperty.Register(nameof(CharacterSpacing), typeof(int), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(0));

    public static DependencyProperty ExitDisplayModeOnAccessKeyInvokedProperty { get; } =
        DependencyProperty.Register(nameof(ExitDisplayModeOnAccessKeyInvoked), typeof(bool), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(true));

    public static DependencyProperty FontStretchProperty { get; } =
        DependencyProperty.Register(nameof(FontStretch), typeof(FontStretch), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(System.Windows.FontStretches.Normal));

    public static DependencyProperty FontFamilyProperty { get; } =
        DependencyProperty.Register(nameof(FontFamily), typeof(Microsoft.UI.Xaml.Media.FontFamily), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(new Microsoft.UI.Xaml.Media.FontFamily("Segoe UI")));

    public static DependencyProperty FontSizeProperty { get; } =
        DependencyProperty.Register(nameof(FontSize), typeof(double), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(14d));

    public static DependencyProperty FontStyleProperty { get; } =
        DependencyProperty.Register(nameof(FontStyle), typeof(FontStyle), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(System.Windows.FontStyles.Normal));

    public static DependencyProperty FontWeightProperty { get; } =
        DependencyProperty.Register(nameof(FontWeight), typeof(FontWeight), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(System.Windows.FontWeights.Normal));

    public static DependencyProperty ForegroundProperty { get; } =
        DependencyProperty.Register(nameof(Foreground), typeof(Microsoft.UI.Xaml.Media.Brush), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(new Microsoft.UI.Xaml.Media.SolidColorBrush(global::Windows.UI.Color.FromArgb(255, 0, 0, 0))));

    public static DependencyProperty HorizontalTextAlignmentProperty { get; } =
        DependencyProperty.Register(nameof(HorizontalTextAlignment), typeof(TextAlignment), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(TextAlignment.Left));

    public static DependencyProperty IsAccessKeyScopeProperty { get; } =
        DependencyProperty.Register(nameof(IsAccessKeyScope), typeof(bool), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(false));

    public static DependencyProperty IsTextScaleFactorEnabledProperty { get; } =
        DependencyProperty.Register(nameof(IsTextScaleFactorEnabled), typeof(bool), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(true));

    public static DependencyProperty KeyTipHorizontalOffsetProperty { get; } =
        DependencyProperty.Register(nameof(KeyTipHorizontalOffset), typeof(double), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(0d));

    public static DependencyProperty KeyTipPlacementModeProperty { get; } =
        DependencyProperty.Register(nameof(KeyTipPlacementMode), typeof(Microsoft.UI.Xaml.Input.KeyTipPlacementMode), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(Microsoft.UI.Xaml.Input.KeyTipPlacementMode.Auto));

    public static DependencyProperty KeyTipVerticalOffsetProperty { get; } =
        DependencyProperty.Register(nameof(KeyTipVerticalOffset), typeof(double), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(0d));

    public static DependencyProperty LanguageProperty { get; } =
        DependencyProperty.Register(nameof(Language), typeof(string), typeof(TextElement), new Microsoft.UI.Xaml.PropertyMetadata(string.Empty));

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

    public Microsoft.UI.Xaml.Media.FontFamily FontFamily
    {
        get => (Microsoft.UI.Xaml.Media.FontFamily)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontStyle FontStyle
    {
        get => (FontStyle)GetValue(FontStyleProperty);
        set => SetValue(FontStyleProperty, value);
    }

    public FontStretch FontStretch
    {
        get => (FontStretch)GetValue(FontStretchProperty);
        set => SetValue(FontStretchProperty, value);
    }

    public FontWeight FontWeight
    {
        get => (FontWeight)GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public Microsoft.UI.Xaml.Media.Brush Foreground
    {
        get => (Microsoft.UI.Xaml.Media.Brush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
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

    public string Name => string.Empty;

    public Microsoft.UI.Xaml.XamlRoot XamlRoot { get; set; }

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

    bool System.Windows.Input.IInputElement.Focus() => true;

    void System.Windows.Input.IInputElement.RaiseEvent(RoutedEventArgs e) =>
        ((Microsoft.UI.Xaml.DependencyObject)this).RaiseEvent(e);

    void System.Windows.Input.IInputElement.AddHandler(RoutedEvent routedEvent, Delegate handler) =>
        ((Microsoft.UI.Xaml.DependencyObject)this).AddHandler(routedEvent, handler);

    void System.Windows.Input.IInputElement.RemoveHandler(RoutedEvent routedEvent, Delegate handler) =>
        ((Microsoft.UI.Xaml.DependencyObject)this).RemoveHandler(routedEvent, handler);

    bool System.Windows.Input.IInputElement.IsMouseCaptured =>
        ((Microsoft.UI.Xaml.DependencyObject)this).IsMouseCaptured;

    bool System.Windows.Input.IInputElement.IsMouseOver =>
        ((Microsoft.UI.Xaml.DependencyObject)this).IsMouseOver;

    void System.Windows.Input.IInputElement.ReleaseMouseCapture() =>
        ((Microsoft.UI.Xaml.DependencyObject)this).ReleaseMouseCapture();
}
