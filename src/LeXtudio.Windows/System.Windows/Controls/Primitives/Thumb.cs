namespace System.Windows.Controls.Primitives;

public partial class Thumb : Control
{
    public static readonly RoutedEvent DragStartedEvent = EventManager.RegisterRoutedEvent(
        "DragStarted", RoutingStrategy.Bubble, typeof(DragStartedEventHandler), typeof(Thumb));

    public static readonly RoutedEvent DragDeltaEvent = EventManager.RegisterRoutedEvent(
        "DragDelta", RoutingStrategy.Bubble, typeof(DragDeltaEventHandler), typeof(Thumb));

    public static readonly RoutedEvent DragCompletedEvent = EventManager.RegisterRoutedEvent(
        "DragCompleted", RoutingStrategy.Bubble, typeof(DragCompletedEventHandler), typeof(Thumb));

    private static readonly RoutedEvent MouseDoubleClickEvent = EventManager.RegisterRoutedEvent(
        "MouseDoubleClick", RoutingStrategy.Bubble, typeof(Input.MouseButtonEventHandler), typeof(Thumb));

    private double _lastX, _lastY;
    private bool _isDragging;

    public event DragStartedEventHandler DragStarted
    {
        add => AddHandler(DragStartedEvent, value);
        remove => RemoveHandler(DragStartedEvent, value);
    }

    public event DragDeltaEventHandler DragDelta
    {
        add => AddHandler(DragDeltaEvent, value);
        remove => RemoveHandler(DragDeltaEvent, value);
    }

    public event DragCompletedEventHandler DragCompleted
    {
        add => AddHandler(DragCompletedEvent, value);
        remove => RemoveHandler(DragCompletedEvent, value);
    }

    public event Input.MouseButtonEventHandler MouseDoubleClick
    {
        add => AddHandler(MouseDoubleClickEvent, value);
        remove => RemoveHandler(MouseDoubleClickEvent, value);
    }

    public Thumb()
    {
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerCaptureLost += OnPointerCaptureLost;
    }

    private void OnPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var pt = e.GetCurrentPoint(this);
        if (!pt.Properties.IsLeftButtonPressed)
            return;

        _lastX = pt.Position.X;
        _lastY = pt.Position.Y;
        _isDragging = true;
        CapturePointer(e.Pointer);
        e.Handled = true;

        RaiseEvent(new DragStartedEventArgs(_lastX, _lastY)
        {
            RoutedEvent = DragStartedEvent,
            Source = this,
        });
    }

    private void OnPointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (!_isDragging)
            return;

        var pt = e.GetCurrentPoint(this);
        var deltaX = pt.Position.X - _lastX;
        var deltaY = pt.Position.Y - _lastY;
        _lastX = pt.Position.X;
        _lastY = pt.Position.Y;
        e.Handled = true;

        RaiseEvent(new DragDeltaEventArgs(deltaX, deltaY)
        {
            RoutedEvent = DragDeltaEvent,
            Source = this,
        });
    }

    private void OnPointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (!_isDragging)
            return;

        _isDragging = false;
        ReleasePointerCapture(e.Pointer);
        e.Handled = true;

        RaiseEvent(new DragCompletedEventArgs(0, 0, false)
        {
            RoutedEvent = DragCompletedEvent,
            Source = this,
        });
    }

    private void OnPointerCaptureLost(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (!_isDragging)
            return;

        _isDragging = false;
        RaiseEvent(new DragCompletedEventArgs(0, 0, true)
        {
            RoutedEvent = DragCompletedEvent,
            Source = this,
        });
    }
}
