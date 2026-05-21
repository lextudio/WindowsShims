#if HAS_UNO
using MS.Internal.Documents;

namespace System.Windows.Controls.Primitives;

public partial class TextBoxBase
{
    internal void NotifySelectionChanged()
    {
        if (RenderScope is FlowDocumentView flowDocumentView)
        {
            flowDocumentView.RefreshSelection();
        }

        OnSelectionChanged(new RoutedEventArgs(SelectionChangedEvent));
    }
}
#endif
