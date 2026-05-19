namespace System.Windows.Input
{
    public delegate void MouseButtonEventHandler(object sender, MouseButtonEventArgs e);
    public delegate void MouseEventHandler(object sender, MouseEventArgs e);

    public static class Mouse
    {
        public static readonly System.Windows.RoutedEvent QueryCursorEvent = new();
        public static readonly System.Windows.RoutedEvent PreviewMouseLeftButtonDownEvent = new();
        public static readonly System.Windows.RoutedEvent MouseLeftButtonDownEvent = new();
        public static readonly System.Windows.RoutedEvent MouseDownEvent = new();
        public static readonly System.Windows.RoutedEvent MouseMoveEvent = new();
        public static readonly System.Windows.RoutedEvent MouseUpEvent = new();

        public static void Capture(IInputElement element)
        {
            if (element is Microsoft.UI.Xaml.DependencyObject dependencyObject)
            {
                dependencyObject.CaptureMouse();
            }
        }

        public static Point GetPosition(Microsoft.UI.Xaml.UIElement element) => default;
        public static void UpdateCursor() { }
    }
}
