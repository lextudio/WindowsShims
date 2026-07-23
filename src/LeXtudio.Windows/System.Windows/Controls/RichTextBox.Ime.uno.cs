#if HAS_UNO
using System.Reflection;
using LeXtudio.UI.Text.Core;

namespace System.Windows.Controls;

// Bridges RichTextBox to real OS-level IME composition via LeXtudio.UI.Text.Core's
// CoreTextEditContext (macOS/Linux-IBus/Win32 native adapters, no dependency on WPF's
// unimplemented TSF/TextServices/ImmComposition family — see docs/RICHTEXTBOX-PORT-CATALOG.md).
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
            _imeContext = CoreTextServicesManager.GetForCurrentView().CreateEditContext();
            _imeContext.TextRequested += OnImeTextRequested;
            _imeContext.TextUpdating += OnImeTextUpdating;
            _imeContext.SelectionRequested += OnImeSelectionRequested;
            _imeContext.SelectionUpdating += OnImeSelectionUpdating;
            _imeContext.LayoutRequested += OnImeLayoutRequested;
            _imeContext.CompositionStarted += (_, _) => _imeComposing = true;
            _imeContext.CompositionCompleted += (_, _) => _imeComposing = false;
            _imeContext.CommandReceived += OnImeCommandReceived;

            var (windowHandle, displayHandle) = ResolveNativeWindowHandles();
            if (windowHandle != 0)
            {
                bool attached = _imeContext.AttachToWindowHandle(windowHandle, displayHandle);
                Log($"Ime: AttachToWindowHandle(0x{windowHandle:X}) -> {attached}");
            }
            else
            {
                Log("Ime: no native window handle available, IME composition will not activate");
            }
        }
        catch (Exception ex)
        {
            Log($"Ime: EnsureImeContext THREW {ex.GetType().Name}: {ex.Message}");
        }
    }

    private (nint windowHandle, nint displayHandle) ResolveNativeWindowHandles()
    {
        try
        {
            var window = global::Microsoft.UI.Xaml.Window.Current;

            var windowHelperType = Type.GetType("Uno.UI.Xaml.WindowHelper, Uno.UI");
            var getNativeWindow = windowHelperType?.GetMethod("GetNativeWindow", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            object? nativeWindow = window is not null
                ? getNativeWindow?.Invoke(null, [window])
                : null;

            if (nativeWindow is null)
                return (0, 0);

            nint handle = 0;
            foreach (var name in new[] { "Hwnd", "HWnd", "Handle", "WindowHandle", "NativeHandle", "Pointer", "hwnd", "_hwnd" })
            {
                var prop = nativeWindow.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var value = prop?.GetValue(nativeWindow);
                handle = value switch
                {
                    nint n => n,
                    long l => (nint)l,
                    int i => (nint)i,
                    _ => 0,
                };
                if (handle != 0)
                    break;
            }

            return (handle, 0);
        }
        catch (Exception ex)
        {
            Log($"Ime: ResolveNativeWindowHandles THREW {ex.GetType().Name}: {ex.Message}");
            return (0, 0);
        }
    }

    // IME composition offsets are indices into the plain-text string TextRequested hands
    // back (paragraph breaks as "\n", list markers inserted, etc. — see
    // TextRangeBase.PlainConvertParagraphEnd/PlainConvertListItemStart), not raw
    // TextContainer symbol offsets. Rather than hand-walking that same
    // ElementStart/ElementEnd/Text switch a second time (error-prone — see session 43's
    // single-paragraph off-by-N bugs), reuse the already-correct forward mapping
    // (symbol offset -> plain text, via the linked TextRange.Text getter) and invert it:
    // plain-text length as a function of symbol offset is monotonically non-decreasing,
    // so the offset producing a given plain-text length can be found by binary search.
    private static int GetPlainTextOffset(System.Windows.Documents.FlowDocument document, System.Windows.Documents.TextPointer position) =>
        new System.Windows.Documents.TextRange(document.ContentStart, position).Text?.Length ?? 0;

    private static System.Windows.Documents.TextPointer GetPositionAtPlainTextOffset(System.Windows.Documents.FlowDocument document, int targetOffset)
    {
        if (targetOffset <= 0)
            return document.ContentStart;

        int lo = 0;
        int hi = document.ContentStart.GetOffsetToPosition(document.ContentEnd);
        if (GetPlainTextOffset(document, document.ContentEnd) <= targetOffset)
            return document.ContentEnd;

        // Smallest symbol offset `s` such that the plain text up to `s` has length >= targetOffset.
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

    private void OnImeTextRequested(CoreTextEditContext sender, LeXtudio.UI.Text.Core.CoreTextTextRequestedEventArgs e)
    {
        var document = Document;
        if (document is null)
            return;

        e.Request.Text = new System.Windows.Documents.TextRange(document.ContentStart, document.ContentEnd).Text ?? string.Empty;
    }

    private void OnImeTextUpdating(CoreTextEditContext sender, LeXtudio.UI.Text.Core.CoreTextTextUpdatingEventArgs e)
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
            range.Text = e.NewText;

            // Use the just-mutated range's own End position instead of recomputing an offset
            // from basePointer: for a document that had no Run yet (e.g. freshly empty),
            // basePointer was a pre-insertion fallback position, so a second offset-based
            // lookup here would land on stale/misaligned geometry. range.End reflects exactly
            // where the TextRange.Text setter left the content, with no such staleness.
            var newCaret = range.End;
            if (te.Selection is { } selection)
            {
                selection.Select(newCaret, newCaret);
                UpdateCaretFromSelection();
            }

            Log($"Ime: TextUpdating range=[{e.Range.StartCaretPosition},{e.Range.EndCaretPosition}) text='{e.NewText}'");
        }
        catch (Exception ex)
        {
            Log($"Ime: OnImeTextUpdating THREW {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void OnImeSelectionRequested(CoreTextEditContext sender, LeXtudio.UI.Text.Core.CoreTextSelectionRequestedEventArgs e)
    {
        var te = TextEditor;
        var document = Document;
        if (te?.Selection is not { } selection || document is null)
            return;

        e.Request.Start = GetPlainTextOffset(document, (System.Windows.Documents.TextPointer)selection.Start);
        e.Request.Length = GetPlainTextOffset(document, (System.Windows.Documents.TextPointer)selection.End) - e.Request.Start;
    }

    private void OnImeSelectionUpdating(CoreTextEditContext sender, LeXtudio.UI.Text.Core.CoreTextSelectionUpdatingEventArgs e)
    {
        var te = TextEditor;
        var document = Document;
        if (te?.Selection is not { } selection || document is null)
            return;

        try
        {
            var start = GetPositionAtPlainTextOffset(document, e.NewStart);
            var end = GetPositionAtPlainTextOffset(document, e.NewStart + e.NewLength);
            selection.Select(start, end);
            UpdateCaretFromSelection();
        }
        catch (Exception ex)
        {
            Log($"Ime: OnImeSelectionUpdating THREW {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void OnImeLayoutRequested(CoreTextEditContext sender, LeXtudio.UI.Text.Core.CoreTextLayoutRequestedEventArgs e)
    {
        try
        {
            var te = TextEditor;
            var position = te?.Selection?.MovingPosition;
            if (position is null)
                return;

            var rect = position.GetCharacterRect(System.Windows.Documents.LogicalDirection.Forward);
            e.Request.LayoutBounds.TextBounds = new CoreTextRect { X = rect.X, Y = rect.Y, Width = rect.Width, Height = rect.Height };
            e.Request.LayoutBounds.ControlBounds = new CoreTextRect { X = 0, Y = 0, Width = ActualWidth, Height = ActualHeight };
        }
        catch (Exception ex)
        {
            Log($"Ime: OnImeLayoutRequested THREW {ex.GetType().Name}: {ex.Message}");
        }
    }

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
            _imeContext.NotifyCaretRectChanged(rect.X, rect.Y, rect.Width, rect.Height);
            _imeContext.NotifyLayoutChanged();
        }
        catch (Exception ex)
        {
            Log($"Ime: NotifyImeOfCaretAndSelection THREW {ex.GetType().Name}: {ex.Message}");
        }
    }
}
#endif
