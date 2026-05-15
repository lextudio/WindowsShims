namespace System.Windows.Input
{
    public static class Mouse
    {
        public static readonly System.Windows.RoutedEvent QueryCursorEvent = new();
        public static readonly System.Windows.RoutedEvent PreviewMouseLeftButtonDownEvent = new();
        public static readonly System.Windows.RoutedEvent MouseLeftButtonDownEvent = new();

        public static void Capture(IInputElement element)
        {
            if (element is Microsoft.UI.Xaml.DependencyObject dependencyObject)
            {
                dependencyObject.CaptureMouse();
            }
        }
    }
}
