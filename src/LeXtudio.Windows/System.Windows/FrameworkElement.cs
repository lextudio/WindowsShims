namespace System.Windows;

public class FrameworkElement : DependencyObject
{
    public static readonly DependencyProperty HorizontalAlignmentProperty =
        DependencyProperty.Register(
            nameof(HorizontalAlignment),
            typeof(HorizontalAlignment),
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(HorizontalAlignment.Stretch));

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

    public HorizontalAlignment HorizontalAlignment
    {
        get => (HorizontalAlignment)(GetValue(HorizontalAlignmentProperty) ?? HorizontalAlignment.Stretch);
        set => SetValue(HorizontalAlignmentProperty, value);
    }
}

public class FrameworkContentElement : DependencyObject
{
    public DependencyObject? Parent { get; internal set; }

    public virtual void BeginInit()
    {
    }

    public virtual void EndInit()
    {
    }

    protected virtual System.Windows.Automation.Peers.AutomationPeer? OnCreateAutomationPeer()
    {
        return null;
    }
}
