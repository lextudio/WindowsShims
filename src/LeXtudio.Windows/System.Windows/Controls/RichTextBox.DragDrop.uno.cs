#if HAS_UNO
using System.Windows.Documents;

namespace System.Windows.Controls;

// Wires the previously-unused System.Windows.Documents.TextEditorDragDropUno (built for
// RichTextBlock, per its own header comment, but never actually instantiated anywhere in
// this repo) into RichTextBox. Upstream WPF's real _DragDropProcess is stubbed to no-ops on
// the Uno path (see TextEditorDragDrop.uno.cs) specifically because drag/drop was meant to be
// driven by this renderer-level class instead — this file is that missing wiring.
public partial class RichTextBox : IRichTextDragDropHost
{
    private TextEditorDragDropUno? _dragDrop;

    private void EnsureDragDrop()
    {
        if (_dragDrop != null)
            return;

        _dragDrop = new TextEditorDragDropUno(this, this);
    }

    // ── IRichTextDragDropHost ────────────────────────────────────────────────

    bool IRichTextDragDropHost.IsReadOnly => IsReadOnly;

    bool IRichTextDragDropHost.HasLayout => TextEditor?.TextView?.RenderScope is MS.Internal.Documents.FlowDocumentView { Page: not null };

    (int min, int max) IRichTextDragDropHost.GetSelectionRange()
    {
        var document = Document;
        if (TextEditor?.Selection is not { IsEmpty: false } selection || document is null)
            return (-1, -1);

        var start = GetPlainTextOffset(document, (System.Windows.Documents.TextPointer)selection.Start);
        var end = GetPlainTextOffset(document, (System.Windows.Documents.TextPointer)selection.End);
        return (start, end);
    }

    string IRichTextDragDropHost.GetTextRange(int start, int end)
    {
        var document = Document;
        if (document is null)
            return string.Empty;

        var startPointer = GetPositionAtPlainTextOffset(document, start);
        var endPointer = GetPositionAtPlainTextOffset(document, end);
        return new System.Windows.Documents.TextRange(startPointer, endPointer).Text ?? string.Empty;
    }

    int IRichTextDragDropHost.HitTest(Point pt)
    {
        var te = TextEditor;
        var document = Document;
        if (te?.TextView is null || document is null)
            return -1;

        var position = te.TextView.GetTextPositionFromPoint(pt, snapToText: true);
        return position is System.Windows.Documents.TextPointer textPointer
            ? GetPlainTextOffset(document, textPointer)
            : -1;
    }

    void IRichTextDragDropHost.InsertTextAt(int offset, string text)
    {
        var te = TextEditor;
        var document = Document;
        if (te is null || document is null)
            return;

        var position = GetPositionAtPlainTextOffset(document, offset);
        var range = new System.Windows.Documents.TextRange(position, position);
        range.Text = text;

        if (te.Selection is { } selection)
        {
            selection.Select(range.End, range.End);
            UpdateCaretFromSelection();
        }

        Log($"DragDrop: inserted '{text}' at offset {offset}");
    }

    void IRichTextDragDropHost.SetDropCaretOffset(int offset)
    {
        var document = Document;
        if (TextEditor?.TextView?.RenderScope is not MS.Internal.Documents.FlowDocumentView fdv || document is null)
            return;

        if (offset < 0)
            return;

        var position = GetPositionAtPlainTextOffset(document, offset);
        fdv.SetCaretAt(position);
    }
}
#endif
