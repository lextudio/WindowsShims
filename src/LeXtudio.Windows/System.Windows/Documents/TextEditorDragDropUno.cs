#if !WINDOWS_APP_SDK
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using UnoUIElement = Microsoft.UI.Xaml.UIElement;
using UnoDragEventArgs = Microsoft.UI.Xaml.DragEventArgs;
using UnoDragStartingEventArgs = Microsoft.UI.Xaml.DragStartingEventArgs;

namespace System.Windows.Documents;

/// <summary>
/// Uno-native drag-and-drop logic for RichTextBlock.
/// Mirrors the structure of WPF's TextEditorDragDrop._DragDropProcess but uses
/// Uno's UIElement drag events (DragStarting / DragEnter / DragOver / DragLeave / Drop)
/// instead of Win32 OLE drag-drop.
/// </summary>
internal sealed class TextEditorDragDropUno
{
    private readonly UnoUIElement _target;
    private readonly IRichTextDragDropHost _host;

    public TextEditorDragDropUno(UnoUIElement target, IRichTextDragDropHost host)
    {
        _target = target;
        _host = host;

        target.AllowDrop = true;
        target.CanDrag = false; // enabled dynamically when pointer-down lands inside an existing selection
        target.DragStarting += OnDragStarting;
        target.DragEnter += OnDragEnter;
        target.DragOver += OnDragOver;
        target.DragLeave += OnDragLeave;
        target.Drop += OnDrop;
    }

    /// <summary>
    /// Called from the host's PointerPressed handler: enables drag only when the press point
    /// falls inside an existing selection, mirroring WPF _DragDropProcess.SourceOnMouseLeftButtonDown.
    /// </summary>
    public void UpdateCanDrag(bool pressIsInsideSelection)
        => _target.CanDrag = pressIsInsideSelection;

    // Mirrors _DragDropProcess.SourceDoDragDrop — populates DataPackage from current selection.
    private void OnDragStarting(UnoUIElement sender, UnoDragStartingEventArgs e)
    {
        if (!AllowDragDrop())
        {
            e.Cancel = true;
            return;
        }

        var (selMin, selMax) = _host.GetSelectionRange();
        if (selMin < 0 || selMin == selMax)
        {
            e.Cancel = true;
            return;
        }

        e.Data.SetText(_host.GetTextRange(selMin, selMax));
        e.DragUI.SetContentFromDataPackage();
    }

    // Mirrors _DragDropProcess.TargetOnDragEnter — accept text drops.
    private void OnDragEnter(object sender, UnoDragEventArgs e)
    {
        if (!AllowDragDrop()) return;
        if (e.DataView.Contains(StandardDataFormats.Text))
            e.AcceptedOperation = DataPackageOperation.Copy;
    }

    // Mirrors _DragDropProcess.TargetOnDragOver — hit-test → update drop caret.
    private void OnDragOver(object sender, UnoDragEventArgs e)
    {
        if (!AllowDragDrop()) return;
        if (!e.DataView.Contains(StandardDataFormats.Text)) return;

        e.AcceptedOperation = DataPackageOperation.Copy;
        var pt = e.GetPosition(_target);
        _host.SetDropCaretOffset(_host.HitTest(pt));
    }

    // Mirrors _DragDropProcess.DeleteCaret — clear drop caret on leave.
    private void OnDragLeave(object sender, UnoDragEventArgs e)
        => _host.SetDropCaretOffset(-1);

    // Mirrors _DragDropProcess.TargetOnDrop — insert dropped text at caret position.
    private async void OnDrop(object sender, UnoDragEventArgs e)
    {
        _host.SetDropCaretOffset(-1);

        if (!AllowDragDrop()) return;
        if (!e.DataView.Contains(StandardDataFormats.Text)) return;

        var pt = e.GetPosition(_target);
        var insertAt = _host.HitTest(pt);
        if (insertAt < 0) return;

        var text = await e.DataView.GetTextAsync();
        if (string.IsNullOrEmpty(text)) return;

        _host.InsertTextAt(insertAt, text);
    }

    // Mirrors _DragDropProcess.AllowDragDrop — gate all target operations.
    private bool AllowDragDrop() => !_host.IsReadOnly && _host.HasLayout;
}

/// <summary>
/// Contract that a Uno text renderer must satisfy so TextEditorDragDropUno can
/// drive hit-testing, selection reads, and document mutations without coupling
/// to a specific renderer type.
/// </summary>
public interface IRichTextDragDropHost
{
    bool IsReadOnly { get; }
    bool HasLayout { get; }

    /// <summary>Returns (min, max) of the current selection; min == -1 when empty.</summary>
    (int min, int max) GetSelectionRange();

    /// <summary>Extracts plain text for the given absolute char range.</summary>
    string GetTextRange(int start, int end);

    /// <summary>Hit-tests a canvas-local point and returns the nearest char offset.</summary>
    int HitTest(Point pt);

    /// <summary>Inserts <paramref name="text"/> into the document at the given char offset.</summary>
    void InsertTextAt(int offset, string text);

    /// <summary>Shows or hides the drop-insertion caret. Pass -1 to hide.</summary>
    void SetDropCaretOffset(int offset);
}
#endif
