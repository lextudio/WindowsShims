namespace System.Windows.Controls.Primitives;

/// <summary>
/// WPF ButtonBase shim — abstract base for button-like controls.
/// It is based on WinUI's button base so shim controls get native command/click
/// behavior while still exposing the WPF-shaped members used by linked sources.
/// </summary>
public abstract partial class ButtonBase : Microsoft.UI.Xaml.Controls.Primitives.ButtonBase
{
    protected static readonly DependencyProperty FocusableProperty =
        DependencyProperty.Register("Focusable", typeof(bool), typeof(ButtonBase), new PropertyMetadata(true));

    public static readonly DependencyProperty ContentStringFormatProperty =
        DependencyProperty.Register(nameof(ContentStringFormat), typeof(string), typeof(ButtonBase), new PropertyMetadata(null));

    public static readonly RoutedEvent ClickEvent = new();
    public event RoutedEventHandler? Click;
    private bool _isPressed;
    private bool _isPointerOver;

    protected ButtonBase()
    {
        // Fade the whole control when disabled (WPF toolbar buttons grey out).
        // Done with a direct Opacity set rather than a VisualState because
        // VisualStateManager.GoToState needs the template applied and the
        // control hosted, which isn't guaranteed when toolbar items are created
        // with IsEnabled=false before being added to the tree.
        IsEnabledChanged += (_, _) => UpdateVisualState();
    }

    // ── ClickMode ────────────────────────────────────────────────────────────
    public static readonly DependencyProperty ClickModeProperty =
        DependencyProperty.Register(nameof(ClickMode), typeof(Controls.ClickMode), typeof(ButtonBase),
            new PropertyMetadata(Controls.ClickMode.Release));

    public Controls.ClickMode ClickMode
    {
        get => (Controls.ClickMode)GetValue(ClickModeProperty);
        set => SetValue(ClickModeProperty, value);
    }

    // ── Command ──────────────────────────────────────────────────────────────
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(System.Windows.Input.ICommand), typeof(ButtonBase),
            new PropertyMetadata(null));

    public System.Windows.Input.ICommand? Command
    {
        get => (System.Windows.Input.ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(ButtonBase),
            new PropertyMetadata(null));

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public static readonly DependencyProperty CommandTargetProperty =
        DependencyProperty.Register(nameof(CommandTarget), typeof(IInputElement), typeof(ButtonBase),
            new PropertyMetadata(null));

    public IInputElement? CommandTarget
    {
        get => (IInputElement?)GetValue(CommandTargetProperty);
        set => SetValue(CommandTargetProperty, value);
    }

    // ── WPF virtual click/mouse entry points ─────────────────────────────────
    // Subclasses (DataGridColumnHeader) override OnClick to perform their action.
    protected virtual void OnClick()
    {
        Click?.Invoke(this, new RoutedEventArgs());
    }

    protected virtual void OnMouseLeftButtonDown(Input.MouseButtonEventArgs e) { }
    protected virtual void OnMouseMove(Input.MouseEventArgs e) { }
    protected virtual void OnMouseLeftButtonUp(Input.MouseButtonEventArgs e) { }
    protected virtual void OnLostMouseCapture(Input.MouseEventArgs e) { }

    // WPF mouse-capture shims — DataGridColumnHeader checks IsMouseCaptured
    // before calling ReleaseMouseCapture; both are no-ops until full drag
    // plumbing is added.
    public bool IsMouseCaptured => false;
    public void CaptureMouse() { }
    public void ReleaseMouseCapture() { }

    public bool IsPressed => _isPressed;
    public bool IsMouseOver => _isPointerOver;

    public string? ContentStringFormat
    {
        get => (string?)GetValue(ContentStringFormatProperty);
        set => SetValue(ContentStringFormatProperty, value);
    }

    protected Microsoft.UI.Xaml.DependencyObject? VisualParent => Parent;

    protected void CoerceValue(DependencyProperty dp) { }

    // ── WinUI pointer bridges ─────────────────────────────────────────────────
    protected override void OnPointerPressed(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pt = e.GetCurrentPoint(this);
        if (pt.Properties.IsLeftButtonPressed)
        {
            _isPressed = true;
            UpdateVisualState();
            var args = new Input.MouseButtonEventArgs();
            OnMouseLeftButtonDown(args);
            if (!args.Handled && ClickMode == Controls.ClickMode.Press)
                OnClick();
        }
    }

    protected override void OnPointerMoved(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerMoved(e);
        OnMouseMove(new Input.MouseEventArgs());
    }

    protected override void OnPointerEntered(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerEntered(e);
        _isPointerOver = true;
        UpdateVisualState();
    }

    protected override void OnPointerExited(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerExited(e);
        _isPointerOver = false;
        _isPressed = false;
        UpdateVisualState();
    }

    protected override void OnPointerReleased(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);
        var pt = e.GetCurrentPoint(this);
        if (!pt.Properties.IsLeftButtonPressed)
        {
            _isPressed = false;
            UpdateVisualState();
            var args = new Input.MouseButtonEventArgs();
            OnMouseLeftButtonUp(args);
            if (!args.Handled && ClickMode == Controls.ClickMode.Release)
                OnClick();
        }
    }

    protected override void OnPointerCaptureLost(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _isPressed = false;
        UpdateVisualState();
        OnLostMouseCapture(new Input.MouseEventArgs());
    }

    // Drive the CommonStates visual-state group (Normal/PointerOver/Pressed/Disabled)
    // so the toolbar Button/ToggleButton templates show hover and pressed highlights.
    // DataGridColumnHeader overrides this to skip button state changes.
    internal void UpdateVisualState() => UpdateVisualState(true);

    internal virtual void UpdateVisualState(bool useTransitions) => ChangeVisualState(useTransitions);

    internal virtual void ChangeVisualState(bool useTransitions)
    {
        string state = !IsEnabled ? "Disabled"
            : _isPressed ? "Pressed"
            : _isPointerOver ? "PointerOver"
            : "Normal";
        Microsoft.UI.Xaml.VisualStateManager.GoToState(this, state, useTransitions);
    }

    internal static void OnVisualStatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

    internal void ChangeValidationVisualState(bool useTransitions)
    {
        Microsoft.UI.Xaml.VisualStateManager.GoToState(this, VisualStates.StateValid, useTransitions);
    }
}
