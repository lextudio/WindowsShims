namespace System.Windows.Controls.Primitives;

public class ToggleButton : ButtonBase
{
    private Microsoft.UI.Xaml.Controls.Border? _templateRoot;
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush CheckedBackgroundBrush = new(global::Windows.UI.Color.FromArgb(0x3D, 0x66, 0x99, 0xCC));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush CheckedBorderBrush = new(global::Windows.UI.Color.FromArgb(0x99, 0x66, 0x99, 0xCC));

    public ToggleButton()
    {
        DefaultStyleKey = typeof(ToggleButton);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _templateRoot = GetTemplateChild("Root") as Microsoft.UI.Xaml.Controls.Border;
        UpdateVisualState(false);
    }

    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(nameof(IsChecked), typeof(bool?), typeof(ToggleButton),
            new PropertyMetadata(false, OnIsCheckedChanged));

    private static void OnIsCheckedChanged(Microsoft.UI.Xaml.DependencyObject d, Microsoft.UI.Xaml.DependencyPropertyChangedEventArgs e)
    {
        var button = (ToggleButton)d;
        button.UpdateVisualState(true);
    }

    private void UpdateCheckVisualState(bool useTransitions)
    {
        string state = IsChecked == true ? "Checked"
            : IsChecked == null ? "Indeterminate"
            : "Unchecked";
        Microsoft.UI.Xaml.VisualStateManager.GoToState(this, state, useTransitions);
        ApplyCheckedChrome();
    }

    private void ApplyCheckedChrome()
    {
        if (_templateRoot is null)
            return;

        if (IsChecked == true)
        {
            _templateRoot.Background = CheckedBackgroundBrush;
            _templateRoot.BorderBrush = CheckedBorderBrush;
            return;
        }

        _templateRoot.ClearValue(Microsoft.UI.Xaml.Controls.Border.BackgroundProperty);
        _templateRoot.ClearValue(Microsoft.UI.Xaml.Controls.Border.BorderBrushProperty);
    }

    internal override void ChangeVisualState(bool useTransitions)
    {
        base.ChangeVisualState(useTransitions);
        UpdateCheckVisualState(useTransitions);
    }

    public bool? IsChecked
    {
        get => (bool?)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public static readonly DependencyProperty IsThreeStateProperty =
        DependencyProperty.Register(nameof(IsThreeState), typeof(bool), typeof(ToggleButton),
            new PropertyMetadata(false));

    public bool IsThreeState
    {
        get => (bool)GetValue(IsThreeStateProperty);
        set => SetValue(IsThreeStateProperty, value);
    }

    public event RoutedEventHandler? Checked;
    public event RoutedEventHandler? Unchecked;
    public event RoutedEventHandler? Indeterminate;

    protected override void OnClick()
    {
        if (IsChecked == true)
        {
            IsChecked = IsThreeState ? null : false;
            if (IsChecked == null)
                Indeterminate?.Invoke(this, new RoutedEventArgs());
            else
                Unchecked?.Invoke(this, new RoutedEventArgs());
        }
        else if (IsChecked == null)
        {
            IsChecked = false;
            Unchecked?.Invoke(this, new RoutedEventArgs());
        }
        else
        {
            IsChecked = true;
            Checked?.Invoke(this, new RoutedEventArgs());
        }
        base.OnClick();
    }
}
