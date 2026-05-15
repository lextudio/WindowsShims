namespace System.Windows.Navigation
{
    public class RequestNavigateEventArgs : System.Windows.RoutedEventArgs
    {
        public RequestNavigateEventArgs()
        {
        }

        public RequestNavigateEventArgs(Uri? uri, string? target)
        {
            Uri = uri;
            Target = target;
        }

        public Uri? Uri { get; set; }
        public string? Target { get; set; }
        public bool Handled { get; set; }
    }
}
