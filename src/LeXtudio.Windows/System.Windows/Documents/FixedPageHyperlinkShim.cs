// Removed from compilation when the fixed-document family is un-gated
// (-p:FixedDocumentRung=all), where upstream FixedPage.cs supplies these members.

namespace System.Windows.Documents
{
    // Minimal FixedPage surface used by Hyperlink. The full upstream FixedPage is part of
    // the fixed-document (XPS) family, which is still gated off (see docs/fixeddocument/).
    public sealed partial class FixedPage : Microsoft.UI.Xaml.DependencyObject
    {
        public static readonly Microsoft.UI.Xaml.DependencyProperty NavigateUriProperty =
            Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(
                "NavigateUri",
                typeof(System.Uri),
                typeof(FixedPage),
                new System.Windows.FrameworkPropertyMetadata(null));

        public static System.Uri GetLinkUri(System.Windows.Input.IInputElement sourceElement, System.Uri targetUri) => targetUri;
    }
}
