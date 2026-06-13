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
        public static readonly System.Windows.RoutedEvent MouseLeaveEvent = new();
        public static readonly System.Windows.RoutedEvent MouseEnterEvent = new();

        public static bool Capture(IInputElement element)
        {
            if (element is Microsoft.UI.Xaml.UIElement uie)
            {
                uie.CaptureMouse();
                return true;
            }
            return false;
        }

        public static bool Capture(IInputElement element, CaptureMode captureMode) => Capture(element);

        public static bool Capture(Microsoft.UI.Xaml.UIElement element, CaptureMode captureMode)
        {
            element.CaptureMouse();
            return true;
        }

        public static Point GetPosition(Microsoft.UI.Xaml.UIElement element) => default;
        public static void UpdateCursor() { }

        public static IInputElement? Captured => null;
        public static IInputElement? DirectlyOver => null;
        public static MouseButtonState LeftButton => MouseButtonState.Released;
        public static MouseButtonState RightButton => MouseButtonState.Released;
    }

    public enum CaptureMode
    {
        None     = 0,
        Element  = 1,
        SubTree  = 2,
    }
}
