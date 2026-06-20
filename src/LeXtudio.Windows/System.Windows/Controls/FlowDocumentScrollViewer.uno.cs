#if HAS_UNO
using System.Windows.Documents;

using MS.Internal.Documents;

namespace System.Windows.Controls;

/// <summary>
/// Minimal read-only host for a <see cref="FlowDocument"/>, mirroring the slice of WPF's
/// <c>FlowDocumentScrollViewer</c> that ILSpy uses (Document / Width / MaxWidth / Foreground).
/// <para/>
/// WinUI has no FlowDocument pipeline, so this renders through the same internal
/// <see cref="FlowDocumentView"/> panel that backs the RichTextBox shim, wrapped in a
/// <see cref="Microsoft.UI.Xaml.Controls.ScrollViewer"/>. It is intentionally view-only: no editing,
/// caret, or selection wiring — enough to show decompiler tooltips / documentation.
/// </summary>
public partial class FlowDocumentScrollViewer : Microsoft.UI.Xaml.Controls.ContentControl
{
    private readonly Microsoft.UI.Xaml.Controls.ScrollViewer _scrollViewer;
    private readonly FlowDocumentView _view;

    public FlowDocumentScrollViewer()
    {
        _view = new FlowDocumentView();
        _scrollViewer = new Microsoft.UI.Xaml.Controls.ScrollViewer
        {
            Content = _view,
            HorizontalScrollBarVisibility = Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Auto,
            HorizontalScrollMode = Microsoft.UI.Xaml.Controls.ScrollMode.Disabled,
        };

        Content = _scrollViewer;
        HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
        VerticalContentAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch;
    }

    public FlowDocument? Document
    {
        get => _view.Document;
        set => _view.Document = value;
    }
}
#endif
