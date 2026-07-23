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
    // back, not raw TextContainer symbol offsets — FlowDocument.ContentStart sits before
    // the first Paragraph's own structural boundary, so GetPositionAtOffset/GetOffsetToPosition
    // computed from document.ContentStart are off by that paragraph-boundary delta. Anchoring
    // on the first Paragraph's ContentStart instead keeps the offset space self-consistent
    // with how the rest of this shim (and its test suite) measures run-relative offsets.
    // This only produces correct results for single-paragraph content; multi-paragraph/list/
    // table documents would need a proper plain-text-offset walk (see
    // docs/richtextbox/session43.md for the known limitation).
    private static System.Windows.Documents.TextPointer? GetImeOffsetBase(System.Windows.Documents.FlowDocument document)
    {
        if (document.Blocks.FirstBlock is not System.Windows.Documents.Paragraph paragraph)
            return document.ContentStart;

        // Descend past the Paragraph's own ContentStart into the first Run's ContentStart:
        // each TextElement (Paragraph, then Run) contributes its own ElementStart edge as a
        // symbol offset, so paragraph.ContentStart still sits 1 unit before run.ContentStart.
        var inline = paragraph.Inlines.FirstInline;
        while (inline is System.Windows.Documents.Span span)
            inline = span.Inlines.FirstInline;

        return inline is System.Windows.Documents.Run run ? run.ContentStart : paragraph.ContentStart;
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
        if (te is null || document is null || GetImeOffsetBase(document) is not { } basePointer)
            return;

        try
        {
            var start = basePointer.GetPositionAtOffset(e.Range.StartCaretPosition) ?? basePointer;
            var end = basePointer.GetPositionAtOffset(e.Range.EndCaretPosition) ?? start;
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
        if (te?.Selection is not { } selection || document is null || GetImeOffsetBase(document) is not { } basePointer)
            return;

        e.Request.Start = basePointer.GetOffsetToPosition((System.Windows.Documents.TextPointer)selection.Start);
        e.Request.Length = basePointer.GetOffsetToPosition((System.Windows.Documents.TextPointer)selection.End) - e.Request.Start;
    }

    private void OnImeSelectionUpdating(CoreTextEditContext sender, LeXtudio.UI.Text.Core.CoreTextSelectionUpdatingEventArgs e)
    {
        var te = TextEditor;
        var document = Document;
        if (te?.Selection is not { } selection || document is null || GetImeOffsetBase(document) is not { } basePointer)
            return;

        try
        {
            var start = basePointer.GetPositionAtOffset(e.NewStart) ?? basePointer;
            var end = basePointer.GetPositionAtOffset(e.NewStart + e.NewLength) ?? start;
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
            if (te?.Selection is not { } selection || document is null || GetImeOffsetBase(document) is not { } basePointer)
                return;

            var start = basePointer.GetOffsetToPosition((System.Windows.Documents.TextPointer)selection.Start);
            var end = basePointer.GetOffsetToPosition((System.Windows.Documents.TextPointer)selection.End);
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
