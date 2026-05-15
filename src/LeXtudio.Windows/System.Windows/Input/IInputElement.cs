namespace System.Windows.Input
{
    public interface IInputElement
    {
        bool Focus();
        void RaiseEvent(System.Windows.RoutedEventArgs e);
        void AddHandler(System.Windows.RoutedEvent routedEvent, Delegate handler);
        void RemoveHandler(System.Windows.RoutedEvent routedEvent, Delegate handler);
        bool IsMouseCaptured { get; }
        bool IsMouseOver { get; }
        void ReleaseMouseCapture();
    }
}
