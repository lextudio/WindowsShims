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
}
