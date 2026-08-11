#if HAS_UNO
using System.Reflection;
using Microsoft.UI.Xaml;

namespace System.Windows.Controls;

// Bridges RichTextBox to real OS-level IME composition via CoreTextEditContext.
// On Uno/Skia desktop: uses LeXtudio.UI.Text.Core cross-platform IME bridge.
// On WinUI/Windows App SDK: uses Windows.UI.Text.Core (native WinRT API).
// See docs/RICHTEXTBOX-PORT-CATALOG.md for design.
// The document itself stays the single source of truth: TextRequested/SelectionRequested
// read from TextEditor.Selection/Document, TextUpdating/SelectionUpdating write back into
// them via TextPointer character offsets, exactly like the rest of RichTextBox.uno.cs.
public partial class RichTextBox
{
    private CoreTextEditContext? _imeContext;
    private bool _imeComposing;
    private bool _imeAttachAttempted;
    private int _imeCompositionStart;
    private int _imeCompositionLength;

    private void EnsureImeContext()
    {
        if (_imeContext != null || _imeAttachAttempted)
            return;
        _imeAttachAttempted = true;

        try
        {
            var window = global::Microsoft.UI.Xaml.Window.Current;
            _imeContext = CoreTextServicesManager.GetForCurrentView().CreateEditContext();
            _imeContext.TextRequested += OnImeTextRequested;
            _imeContext.TextUpdating += OnImeTextUpdating;
            _imeContext.SelectionRequested += OnImeSelectionRequested;
            _imeContext.SelectionUpdating += OnImeSelectionUpdating;
            _imeContext.LayoutRequested += OnImeLayoutRequested;
            _imeContext.CompositionStarted += (_, _) =>
            {
                _imeComposing = true;
                _imeCompositionStart = 0;
                _imeCompositionLength = 0;
                NotifyImeCompositionRangeToView();
            };
            _imeContext.CompositionCompleted += (_, _) =>
            {
                _imeComposing = false;
                _imeCompositionStart = -1;
                _imeCompositionLength = -1;
                NotifyImeCompositionRangeToView();
            };
            WireImeCommands(_imeContext);

            bool attached = AttachImeToWindow(_imeContext, window);
            Log($"Ime: ensure -> attached={attached}");
        }
        catch (Exception ex)
        {
            Log($"Ime: EnsureImeContext THREW {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void NotifyImeOfCaretAndSelection()
    {
        if (_imeContext is null)
            return;

        try
        {
            var te = TextEditor;
            var document = Document;
            if (te?.Selection is not { } selection || document is null)
                return;

            var start = GetPlainTextOffset(document, (System.Windows.Documents.TextPointer)selection.Start);
            var end = GetPlainTextOffset(document, (System.Windows.Documents.TextPointer)selection.End);
            _imeContext.NotifySelectionChanged(new CoreTextRange { StartCaretPosition = start, EndCaretPosition = end });

            var rect = selection.MovingPosition.GetCharacterRect(System.Windows.Documents.LogicalDirection.Forward);
            NotifyImeCaretRect(_imeContext, new Rect(rect.X, rect.Y, rect.Width, rect.Height));
            _imeContext.NotifyLayoutChanged();
        }
        catch (Exception ex)
        {
            Log($"Ime: NotifyImeOfCaretAndSelection THREW {ex.GetType().Name}: {ex.Message}");
        }
    }

    // ── Plain-text offset helpers (shared with DragDrop) ─────────────────────

    // Uses GetTextInternal directly: TextRange.Text runs NormalizeRange, which
    // expands a range ending inside a table to the containing cell's end, so
    // offsets inside table cells would all collapse to the cell boundary.
    internal static int GetPlainTextOffset(System.Windows.Documents.FlowDocument document, System.Windows.Documents.TextPointer position) =>
        System.Windows.Documents.TextRangeBase.GetTextInternal(document.ContentStart, position)?.Length ?? 0;

    internal static System.Windows.Documents.TextPointer GetPositionAtPlainTextOffset(System.Windows.Documents.FlowDocument document, int targetOffset)
    {
        if (targetOffset <= 0)
            return document.ContentStart;

        int lo = 0;
        int hi = document.ContentStart.GetOffsetToPosition(document.ContentEnd);
        if (GetPlainTextOffset(document, document.ContentEnd) <= targetOffset)
            return document.ContentEnd;

        while (lo < hi)
        {
            int mid = lo + (hi - lo) / 2;
            var candidate = document.ContentStart.GetPositionAtOffset(mid) ?? document.ContentStart;
            if (GetPlainTextOffset(document, candidate) >= targetOffset)
                hi = mid;
            else
                lo = mid + 1;
        }

        var position = document.ContentStart.GetPositionAtOffset(lo) ?? document.ContentStart;

        // A boundary position (e.g. at a table cell edge) is not an insertion
        // position and the shim's insertion machinery would move it backward;
        // step forward into the first text content at/after this position so
        // composition lands inside the intended cell.
        if (!System.Windows.Documents.TextSchema.IsInTextContent(position))
        {
            var pointer = position.CreatePointer();
            while (pointer.CompareTo(document.ContentEnd) < 0 &&
                   pointer.GetPointerContext(System.Windows.Documents.LogicalDirection.Forward) != System.Windows.Documents.TextPointerContext.Text)
            {
                pointer.MoveToNextContextPosition(System.Windows.Documents.LogicalDirection.Forward);
            }
            return (System.Windows.Documents.TextPointer)pointer;
        }

        return position;
    }

    // ── CoreText event handlers ─────────────────────────────────────────────

    private void OnImeTextRequested(CoreTextEditContext sender, CoreTextTextRequestedEventArgs e)
    {
        var document = Document;
        if (document is null)
            return;

        e.Request.Text = new System.Windows.Documents.TextRange(document.ContentStart, document.ContentEnd).Text ?? string.Empty;
    }

    private void OnImeTextUpdating(CoreTextEditContext sender, CoreTextTextUpdatingEventArgs e)
    {
        var te = TextEditor;
        var document = Document;
        if (te is null || document is null)
            return;

        try
        {
            var start = GetPositionAtPlainTextOffset(document, e.Range.StartCaretPosition);
            var end = GetPositionAtPlainTextOffset(document, e.Range.EndCaretPosition);
            string newText = ImeTextOf(e) ?? string.Empty;
            // Do not use TextRange.Text here: TextRange construction clamps
            // positions inside a table cell to the containing cell's boundaries,
            // so composing inside a cell would insert at the wrong place.
            if (start.CompareTo(end) != 0)
            {
                ((System.Windows.Documents.TextContainer)start.TextContainer).DeleteContentInternal(start, end);
            }
            if (newText.Length > 0)
            {
                start.InsertTextInRun(newText);
            }

            var newCaret = GetPositionAtPlainTextOffset(document, e.Range.StartCaretPosition + newText.Length);
            if (te.Selection is { } selection)
            {
                selection.Select(newCaret, newCaret);
                UpdateCaretFromSelection();
            }

            _imeCompositionStart = e.Range.StartCaretPosition;
            _imeCompositionLength = ImeTextOf(e)?.Length ?? 0;
            Log($"Ime: TextUpdating range=[{e.Range.StartCaretPosition},{e.Range.EndCaretPosition}) text='{ImeTextOf(e)}'");
            NotifyImeCompositionRangeToView();
        }
        catch (Exception ex)
        {
            Log($"Ime: OnImeTextUpdating THREW {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void OnImeSelectionRequested(CoreTextEditContext sender, CoreTextSelectionRequestedEventArgs e)
    {
        var te = TextEditor;
        var document = Document;
        if (te?.Selection is not { } selection || document is null)
            return;

        var selStart = GetPlainTextOffset(document, (System.Windows.Documents.TextPointer)selection.Start);
        var selEnd = GetPlainTextOffset(document, (System.Windows.Documents.TextPointer)selection.End);
        SetImeRequestedSelection(e, selStart, selEnd);
    }

    private void OnImeSelectionUpdating(CoreTextEditContext sender, CoreTextSelectionUpdatingEventArgs e)
    {
        var te = TextEditor;
        var document = Document;
        if (te?.Selection is not { } selection || document is null)
            return;

        try
        {
            var (selStart, selEnd) = GetImeSelectionUpdatingRange(e);
            var start = GetPositionAtPlainTextOffset(document, selStart);
            var end = GetPositionAtPlainTextOffset(document, selEnd);
            selection.Select(start, end);
            UpdateCaretFromSelection();
        }
        catch (Exception ex)
        {
            Log($"Ime: OnImeSelectionUpdating THREW {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void OnImeLayoutRequested(CoreTextEditContext sender, CoreTextLayoutRequestedEventArgs e)
    {
        try
        {
            var te = TextEditor;
            var position = te?.Selection?.MovingPosition;
            if (position is null)
                return;

            var rect = position.GetCharacterRect(System.Windows.Documents.LogicalDirection.Forward);
            SetImeLayoutBounds(e, rect, ActualWidth, ActualHeight);
        }
        catch (Exception ex)
        {
            Log($"Ime: OnImeLayoutRequested THREW {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void NotifyImeCompositionRangeToView()
    {
        if (TextEditor?.TextView?.RenderScope is MS.Internal.Documents.FlowDocumentView fdv)
        {
            fdv.SetImeCompositionRange(_imeCompositionStart, _imeCompositionLength);
        }
    }

#if !WINDOWS_APP_SDK
    private void OnImeCommandReceived(object? sender, CoreTextCommandReceivedEventArgs e)
    {
        var command = e.Command switch
        {
            "deleteBackward:" => System.Windows.Documents.EditingCommands.Backspace,
            "deleteForward:" => System.Windows.Documents.EditingCommands.Delete,
            "deleteWordBackward:" => System.Windows.Documents.EditingCommands.DeletePreviousWord,
            "deleteWordForward:" => System.Windows.Documents.EditingCommands.DeleteNextWord,
            "moveLeft:" => System.Windows.Documents.EditingCommands.MoveLeftByCharacter,
            "moveRight:" => System.Windows.Documents.EditingCommands.MoveRightByCharacter,
            "moveUp:" => System.Windows.Documents.EditingCommands.MoveUpByLine,
            "moveDown:" => System.Windows.Documents.EditingCommands.MoveDownByLine,
            "moveLeftAndModifySelection:" => System.Windows.Documents.EditingCommands.SelectLeftByCharacter,
            "moveRightAndModifySelection:" => System.Windows.Documents.EditingCommands.SelectRightByCharacter,
            "moveToBeginningOfLine:" => System.Windows.Documents.EditingCommands.MoveToLineStart,
            "moveToEndOfLine:" => System.Windows.Documents.EditingCommands.MoveToLineEnd,
            "moveToBeginningOfDocument:" => System.Windows.Documents.EditingCommands.MoveToDocumentStart,
            "moveToEndOfDocument:" => System.Windows.Documents.EditingCommands.MoveToDocumentEnd,
            "insertNewline:" => System.Windows.Documents.EditingCommands.EnterParagraphBreak,
            "insertTab:" => System.Windows.Documents.EditingCommands.TabForward,
            _ => (System.Windows.Input.RoutedUICommand?)null,
        };

        if (command is null || !command.CanExecute(null, this))
            return;

        command.Execute(null, this);
        UpdateCaretFromSelection();
        e.Handled = true;
        Log($"Ime: CommandReceived '{e.Command}' -> executed {command.Name}");
    }
#endif
}
#endif
