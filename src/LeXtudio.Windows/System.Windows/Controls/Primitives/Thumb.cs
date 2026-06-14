namespace System.Windows.Controls.Primitives;

// Minimal shell carrying the drag routed-event identities that the linked
// Drag*EventArgs files and DataGrid column-header drag paths reference.
// Thumb input behavior (capture, drag tracking) is not implemented.
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
}
