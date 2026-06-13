namespace System.Windows.Input
{
    // Faithful to WPF's hierarchy (MouseEventArgs : InputEventArgs : RoutedEventArgs)
    // so the linked DataGrid column bodies can downcast a RoutedEventArgs to a
    // MouseButtonEventArgs. Handled/Source/RoutedEvent come from RoutedEventArgs.
    public class MouseEventArgs : InputEventArgs
    {
        public bool UserInitiated { get; set; } = true;
        public MouseButtonState LeftButton { get; set; }
        public MouseButtonState RightButton { get; set; }
        public MouseButtonState MiddleButton { get; set; }
        public Point GetPosition(object? relativeTo) => default;
    }
}
