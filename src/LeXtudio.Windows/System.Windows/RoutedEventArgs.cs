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
        protected virtual void InvokeEventHandler(Delegate genericHandler, object genericTarget) { }

        // Allow WinUI RoutedEventArgs to pass where the WPF shim is expected.
        // This bridges XAML-generated code (which uses Microsoft.UI.Xaml.RoutedEventArgs)
        // with handlers whose signatures use the global RoutedEventArgs alias.
        public static implicit operator RoutedEventArgs(Microsoft.UI.Xaml.RoutedEventArgs _) => new RoutedEventArgs();
    }
}
