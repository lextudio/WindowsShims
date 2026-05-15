namespace System.Windows.Documents
{
    public sealed class FixedPage : System.Windows.DependencyObject
    {
        public static readonly Microsoft.UI.Xaml.DependencyProperty NavigateUriProperty =
            Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(
                "NavigateUri",
                typeof(System.Uri),
                typeof(FixedPage),
                new System.Windows.FrameworkPropertyMetadata(null));

        public static System.Uri GetLinkUri(System.Windows.Input.IInputElement sourceElement, System.Uri targetUri) => targetUri;
    }

    public static class BaseUriHelper
    {
        public static readonly Microsoft.UI.Xaml.DependencyProperty BaseUriProperty =
            Microsoft.UI.Xaml.DependencyProperty.Register(
                "BaseUri",
                typeof(System.Uri),
                typeof(BaseUriHelper),
                new System.Windows.FrameworkPropertyMetadata(null));
    }

    public static class TextRangeBase
    {
        public static string GetTextInternal(object start, object end) => string.Empty;
    }

    public sealed class RequestSetStatusBarEventArgs : System.Windows.RoutedEventArgs
    {
        public RequestSetStatusBarEventArgs(System.Uri? uri)
        {
            Uri = uri;
        }

        public static RequestSetStatusBarEventArgs Clear { get; } = new(null);
        public System.Uri? Uri { get; }
    }
}
