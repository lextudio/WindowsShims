namespace System.Windows.Controls.Primitives;

public class ToggleButton : ButtonBase
{
    public ToggleButton()
    {
        DefaultStyleKey = typeof(ToggleButton);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateVisualState(false);
        UpdateCheckVisualState(false);
    }

    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(nameof(IsChecked), typeof(bool?), typeof(ToggleButton),
            new PropertyMetadata(false, OnIsCheckedChanged));

    private static void OnIsCheckedChanged(Microsoft.UI.Xaml.DependencyObject d, Microsoft.UI.Xaml.DependencyPropertyChangedEventArgs e)
    {
        ((ToggleButton)d).UpdateCheckVisualState(true);
    }

    private void UpdateCheckVisualState(bool useTransitions)
    {
        string state = IsChecked == true ? "Checked"
            : IsChecked == null ? "Indeterminate"
            : "Unchecked";
        Microsoft.UI.Xaml.VisualStateManager.GoToState(this, state, useTransitions);
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
