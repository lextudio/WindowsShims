namespace System.Windows.Controls.Primitives;

/// <summary>
/// WPF ButtonBase shim — abstract base for button-like controls.
/// Inherits ContentControl so DataGridColumnHeader (which inherits ButtonBase
/// in upstream WPF) can be used directly as a WinUI content control.
/// </summary>
public abstract partial class ButtonBase : ContentControl
{
    public static readonly RoutedEvent ClickEvent = new();

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
    protected virtual void OnClick() { }

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

    // ── WinUI pointer bridges ─────────────────────────────────────────────────
    protected override void OnPointerPressed(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pt = e.GetCurrentPoint(this);
        if (pt.Properties.IsLeftButtonPressed)
        {
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

    protected override void OnPointerReleased(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);
        var pt = e.GetCurrentPoint(this);
        if (!pt.Properties.IsLeftButtonPressed)
        {
            var args = new Input.MouseButtonEventArgs();
            OnMouseLeftButtonUp(args);
            if (!args.Handled && ClickMode == Controls.ClickMode.Release)
                OnClick();
        }
    }

    protected override void OnPointerCaptureLost(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        OnLostMouseCapture(new Input.MouseEventArgs());
    }

    // ChangeVisualState stub — upstream DataGridColumnHeader calls this on
    // base but explicitly skips the button state changes.
    internal virtual void ChangeVisualState(bool useTransitions) { }
}
