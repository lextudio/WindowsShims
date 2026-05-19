namespace System.Windows.Documents;

// AdornerDecorator shim: upstream AdornerDecorator.cs deferred (requires WPF Decorator base class).
// Provides the AdornerLayer property surface consumed by TextBoxBase and the WPF compatibility layer.
public class AdornerDecorator : FrameworkElement
{
    public AdornerLayer AdornerLayer { get; } = AdornerLayer.GetAdornerLayer(null);
}
