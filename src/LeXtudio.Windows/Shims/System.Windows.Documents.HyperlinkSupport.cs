namespace System.Windows.Documents
{
    public sealed class FixedPage : System.Windows.DependencyObject
    {
        public static readonly System.Windows.DependencyProperty NavigateUriProperty =
            System.Windows.DependencyProperty.RegisterAttached(
                "NavigateUri",
                typeof(System.Uri),
                typeof(FixedPage),
                new System.Windows.FrameworkPropertyMetadata(null));

        public static System.Uri GetLinkUri(System.Windows.Input.IInputElement sourceElement, System.Uri targetUri) => targetUri;
    }

    internal static class BaseUriHelper
    {
        internal static readonly System.Windows.DependencyProperty BaseUriProperty =
            System.Windows.DependencyProperty.Register(
                "BaseUri",
                typeof(System.Uri),
                typeof(BaseUriHelper),
                new System.Windows.FrameworkPropertyMetadata(null));
    }

    internal static class TextRangeBase
    {
        internal static string GetTextInternal(TextPointer start, TextPointer end) => string.Empty;
    }

    internal sealed class RequestSetStatusBarEventArgs : System.Windows.RoutedEventArgs
    {
        internal RequestSetStatusBarEventArgs(System.Uri? uri)
        {
            Uri = uri;
        }

        internal static RequestSetStatusBarEventArgs Clear { get; } = new(null);
        internal System.Uri? Uri { get; }
    }
}

namespace MS.Internal.Commands
{
    internal static class CommandHelpers
    {
        internal static bool CanExecuteCommandSource(System.Windows.Controls.ICommandSource commandSource) => true;
        internal static void ExecuteCommandSource(System.Windows.Controls.ICommandSource commandSource) { }
    }
}
