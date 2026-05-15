namespace System.Windows;

public class FrameworkElement : DependencyObject
{
    public static readonly DependencyProperty CursorProperty =
        DependencyProperty.Register(
            nameof(Cursor),
            typeof(object),
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty FlowDirectionProperty =
        DependencyProperty.Register(
            nameof(FlowDirection),
            typeof(FlowDirection),
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(FlowDirection.LeftToRight));

    public object? Cursor
    {
        get => GetValue(CursorProperty);
        set => SetValue(CursorProperty, value);
    }

    public FlowDirection FlowDirection
    {
        get => (FlowDirection)(GetValue(FlowDirectionProperty) ?? FlowDirection.LeftToRight);
        set => SetValue(FlowDirectionProperty, value);
    }
}

public class FrameworkContentElement : DependencyObject
{
}
