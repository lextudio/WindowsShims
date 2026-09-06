// FixedPage's IInputElement implementation, once the fixed-document family is un-gated
// (see FixedPageHyperlinkShim.cs for the normal-build stand-in). Upstream FixedPage
// derives from FrameworkElement, which implements IInputElement natively; the WinUI Panel
// shim it derives from here does not, but Hyperlink.RaiseNavigate(page, ...) needs it.
// Every member forwards to the WinUIDependencyObjectExtensions already used elsewhere.

namespace System.Windows.Documents
{
    public sealed partial class FixedPage : System.Windows.Input.IInputElement
    {
        bool System.Windows.Input.IInputElement.IsMouseCaptured => this.IsMouseCaptured;

        bool System.Windows.Input.IInputElement.IsMouseOver => this.IsMouseOver;

        void System.Windows.Input.IInputElement.ReleaseMouseCapture() => this.ReleaseMouseCapture();

        bool System.Windows.Input.IInputElement.Focus() => this.Focus();

        void System.Windows.Input.IInputElement.RaiseEvent(System.Windows.RoutedEventArgs e) => this.RaiseEvent(e);

        void System.Windows.Input.IInputElement.AddHandler(System.Windows.RoutedEvent routedEvent, Delegate handler)
            => this.AddHandler(routedEvent, handler);

        void System.Windows.Input.IInputElement.RemoveHandler(System.Windows.RoutedEvent routedEvent, Delegate handler)
            => this.RemoveHandler(routedEvent, handler);
    }
}
