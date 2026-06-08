using System.Collections;

namespace System.Windows.Controls;

/// <summary>
/// WPF-compatible ContentControl base class. Extends WinUI ContentControl with the same
/// WPF surface that the shim Control adds — routed events, focus helpers, DependencyPropertyKey
/// write path — so that ContentControl subclasses (DataGridCell, ButtonBase, etc.) compile
/// upstream WPF source without per-class workarounds.
/// </summary>
public class ContentControl : Microsoft.UI.Xaml.Controls.ContentControl
{
    public static readonly DependencyProperty ContentStringFormatProperty =
        DependencyProperty.Register(nameof(ContentStringFormat), typeof(string), typeof(ContentControl),
            new PropertyMetadata(null));

    public ContentControl()
    {
        PointerEntered += (_, _) => { _isMouseOver = true;  UpdateVisualState(); };
        PointerExited  += (_, _) => { _isMouseOver = false; UpdateVisualState(); };
    }

    private bool _isMouseOver;

    // ── WPF ControlBoolFlags storage ─────────────────────────────────────────
    private ControlBoolFlags _controlBoolField;
    internal bool ReadControlFlag(ControlBoolFlags reqFlag) => (_controlBoolField & reqFlag) != 0;
    internal void WriteControlFlag(ControlBoolFlags reqFlag, bool set)
    {
        if (set) _controlBoolField |= reqFlag;
        else _controlBoolField &= ~reqFlag;
    }

    internal bool ContentIsNotLogical
    {
        get => ReadControlFlag(ControlBoolFlags.ContentIsNotLogical);
        set => WriteControlFlag(ControlBoolFlags.ContentIsNotLogical, value);
    }
    internal bool ContentIsItem
    {
        get => ReadControlFlag(ControlBoolFlags.ContentIsItem);
        set => WriteControlFlag(ControlBoolFlags.ContentIsItem, value);
    }

    // ── WPF HasNonDefaultValue ────────────────────────────────────────────────
    internal bool HasNonDefaultValue(DependencyProperty dp)
        => ReadLocalValue(dp) != Microsoft.UI.Xaml.DependencyProperty.UnsetValue;

    // ── WPF GetPlainText ──────────────────────────────────────────────────────
    internal virtual string GetPlainText() => string.Empty;

    // ── WPF LogicalChildren ───────────────────────────────────────────────────
    protected internal virtual IEnumerator LogicalChildren
        => System.Linq.Enumerable.Empty<object>().GetEnumerator();

    // ── WPF DTypeThemeStyleKey stub ───────────────────────────────────────────
    internal virtual DependencyObjectType DTypeThemeStyleKey
        => DependencyObjectType.FromSystemTypeInternal(GetType());

    // ── WPF internal helper for ToString ─────────────────────────────────────
    internal static string ContentObjectToString(object? content)
        => content switch
        {
            null     => string.Empty,
            string s => s,
            _        => content.ToString() ?? string.Empty,
        };

    // ── Routed event infrastructure ───────────────────────────────────────────

    public void RaiseEvent(RoutedEventArgs e) => WinUIDependencyObjectExtensions.RaiseEvent(this, e);

    protected void AddHandler(System.Windows.RoutedEvent routedEvent, Delegate handler) =>
        WinUIDependencyObjectExtensions.AddHandler(this, routedEvent, handler);

    protected void RemoveHandler(System.Windows.RoutedEvent routedEvent, Delegate handler) =>
        WinUIDependencyObjectExtensions.RemoveHandler(this, routedEvent, handler);

    // ── Read-only DP write path ───────────────────────────────────────────────

    public void SetValue(System.Windows.DependencyPropertyKey key, object? value)
        => SetValue(key.DependencyProperty, value);

    // ── Focus helpers ─────────────────────────────────────────────────────────

    public bool Focus() => Focus(Microsoft.UI.Xaml.FocusState.Programmatic);

    public bool MoveFocus(Input.TraversalRequest request) => false;

    public void BringIntoView() => StartBringIntoView();

    public bool IsVisible => Visibility == Visibility.Visible;

    protected Microsoft.UI.Xaml.DependencyObject? VisualParent
        => Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(this);

    public bool IsKeyboardFocusWithin => false;
    public bool IsKeyboardFocused => false;
    public bool IsMouseOver => _isMouseOver;

    public string? ContentStringFormat
    {
        get => (string?)GetValue(ContentStringFormatProperty);
        set => SetValue(ContentStringFormatProperty, value);
    }

    // ── Logical tree no-ops ────────────────────────────────────────────────────

    protected void AddLogicalChild(object? child) { }
    protected void RemoveLogicalChild(object? child) { }

    // ── WPF coercion no-op ────────────────────────────────────────────────────

    internal void CoerceValue(DependencyProperty dp) { }

    // ── Visual state management ───────────────────────────────────────────────

    internal void UpdateVisualState() => UpdateVisualState(true);
    internal virtual void UpdateVisualState(bool useTransitions) => ChangeVisualState(useTransitions);
    internal virtual void ChangeVisualState(bool useTransitions) { }

    internal static void OnVisualStatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

    protected virtual void OnTextInput(Input.TextCompositionEventArgs e) { }
    protected virtual void OnPreviewKeyDown(System.Windows.KeyEventArgs e) { }
    protected virtual void OnKeyDown(System.Windows.KeyEventArgs e) { }

    public static readonly RoutedEvent MouseLeftButtonDownEvent = new();
    public static readonly RoutedEvent LostFocusEvent = new();
    public static readonly RoutedEvent GotFocusEvent = new();

    protected static readonly DependencyProperty SnapsToDevicePixelsProperty =
        DependencyProperty.Register("SnapsToDevicePixels_CC", typeof(bool), typeof(ContentControl),
            new PropertyMetadata(false));

    protected static readonly DependencyPropertyKey IsMouseOverPropertyKey =
        new(DependencyProperty.Register("IsMouseOver_CC", typeof(bool), typeof(ContentControl),
            new PropertyMetadata(false)));

    public static readonly DependencyProperty IsMouseOverProperty = IsMouseOverPropertyKey.DependencyProperty;
}
