namespace System.Windows.Documents;

// Explicit IInputElement implementations — extension members on DependencyObject cannot
// satisfy interface contracts, so they must be provided as real methods.
public abstract partial class TextElement : System.Windows.Input.IInputElement
{
    bool System.Windows.Input.IInputElement.Focus() => true;

    void System.Windows.Input.IInputElement.RaiseEvent(RoutedEventArgs e) =>
        ((Microsoft.UI.Xaml.DependencyObject)this).RaiseEvent(e);

    void System.Windows.Input.IInputElement.AddHandler(RoutedEvent routedEvent, Delegate handler) =>
        ((Microsoft.UI.Xaml.DependencyObject)this).AddHandler(routedEvent, handler);

    void System.Windows.Input.IInputElement.RemoveHandler(RoutedEvent routedEvent, Delegate handler) =>
        ((Microsoft.UI.Xaml.DependencyObject)this).RemoveHandler(routedEvent, handler);

    bool System.Windows.Input.IInputElement.IsMouseCaptured =>
        ((Microsoft.UI.Xaml.DependencyObject)this).IsMouseCaptured;

    bool System.Windows.Input.IInputElement.IsMouseOver =>
        ((Microsoft.UI.Xaml.DependencyObject)this).IsMouseOver;

    void System.Windows.Input.IInputElement.ReleaseMouseCapture() =>
        ((Microsoft.UI.Xaml.DependencyObject)this).ReleaseMouseCapture();
}
