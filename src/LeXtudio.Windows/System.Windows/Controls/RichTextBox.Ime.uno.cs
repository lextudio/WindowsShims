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
            _imeContext.CompositionStarted += (_, _) => _imeComposing = true;
            _imeContext.CompositionCompleted += (_, _) => _imeComposing = false;
#if !WINDOWS_APP_SDK
            _imeContext.CommandReceived += OnImeCommandReceived;
#endif

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

    internal static int GetPlainTextOffset(System.Windows.Documents.FlowDocument document, System.Windows.Documents.TextPointer position) =>
        new System.Windows.Documents.TextRange(document.ContentStart, position).Text?.Length ?? 0;

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

        return document.ContentStart.GetPositionAtOffset(lo) ?? document.ContentStart;
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
            var range = new System.Windows.Documents.TextRange(start, end);
#if WINDOWS_APP_SDK
            range.Text = e.Text;
#else
            range.Text = e.NewText;
#endif

            var newCaret = range.End;
            if (te.Selection is { } selection)
            {
                selection.Select(newCaret, newCaret);
                UpdateCaretFromSelection();
            }

#if WINDOWS_APP_SDK
            Log($"Ime: TextUpdating range=[{e.Range.StartCaretPosition},{e.Range.EndCaretPosition}) text='{e.Text}'");
#else
            Log($"Ime: TextUpdating range=[{e.Range.StartCaretPosition},{e.Range.EndCaretPosition}) text='{e.NewText}'");
#endif
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

#if WINDOWS_APP_SDK
        var selStart = GetPlainTextOffset(document, (System.Windows.Documents.TextPointer)selection.Start);
        var selEnd = GetPlainTextOffset(document, (System.Windows.Documents.TextPointer)selection.End);
        e.Request.Selection = new CoreTextRange { StartCaretPosition = selStart, EndCaretPosition = selEnd };
#else
        e.Request.Start = GetPlainTextOffset(document, (System.Windows.Documents.TextPointer)selection.Start);
        e.Request.Length = GetPlainTextOffset(document, (System.Windows.Documents.TextPointer)selection.End) - e.Request.Start;
#endif
    }

    private void OnImeSelectionUpdating(CoreTextEditContext sender, CoreTextSelectionUpdatingEventArgs e)
    {
        var te = TextEditor;
        var document = Document;
        if (te?.Selection is not { } selection || document is null)
            return;

        try
        {
#if WINDOWS_APP_SDK
            var start = GetPositionAtPlainTextOffset(document, e.Selection.StartCaretPosition);
            var end = GetPositionAtPlainTextOffset(document, e.Selection.EndCaretPosition);
#else
            var start = GetPositionAtPlainTextOffset(document, e.NewStart);
            var end = GetPositionAtPlainTextOffset(document, e.NewStart + e.NewLength);
#endif
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
#if WINDOWS_APP_SDK
            e.Request.LayoutBounds.TextBounds = new Rect(rect.X, rect.Y, rect.Width, rect.Height);
            e.Request.LayoutBounds.ControlBounds = new Rect(0, 0, ActualWidth, ActualHeight);
#else
            e.Request.LayoutBounds.TextBounds = new CoreTextRect { X = rect.X, Y = rect.Y, Width = rect.Width, Height = rect.Height };
            e.Request.LayoutBounds.ControlBounds = new CoreTextRect { X = 0, Y = 0, Width = ActualWidth, Height = ActualHeight };
#endif
        }
        catch (Exception ex)
        {
            Log($"Ime: OnImeLayoutRequested THREW {ex.GetType().Name}: {ex.Message}");
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
