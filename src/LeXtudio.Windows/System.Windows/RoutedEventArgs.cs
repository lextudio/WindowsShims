namespace System.Windows
{
    public class RoutedEventArgs : EventArgs
    {
        public RoutedEventArgs()
        {
        }

        public RoutedEventArgs(RoutedEvent routedEvent, object? source = null)
        {
            RoutedEvent = routedEvent;
            Source = source;
        }

        public RoutedEvent? RoutedEvent { get; set; }
        public object? Source { get; set; }
        public object? OriginalSource { get; set; }
        public bool Handled { get; set; }
    }
}
