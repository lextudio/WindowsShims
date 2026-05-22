#if HAS_UNO
using System.Windows.Documents;
using System.Windows.Input;

namespace System.Windows.Controls;

public partial class RichTextBox
{
    internal static Action<string>? Logger;
    private bool _isPointerSelecting;
    private System.Windows.Documents.Hyperlink? _pressedHyperlink;
    private Point _pressedPoint;
    private bool _pointerMovedSincePress;

    private static readonly string _logPath =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "rtb-template.log");

    private static void Log(string msg)
    {
        Logger?.Invoke($"[RichTextBox] {msg}");
        System.Diagnostics.Debug.WriteLine($"[RichTextBox] {msg}");
        try { System.IO.File.AppendAllText(_logPath,
            $"{DateTime.Now:HH:mm:ss.fff}  [RichTextBox] {msg}\n"); } catch { }
    }

    protected override void InitializeDefaultStyleKey()
    {
        DefaultStyleKey = typeof(RichTextBox);
        Log($"InitializeDefaultStyleKey: set to typeof(RichTextBox)");
    }

    protected override void OnApplyTemplate()
    {
        Log($"OnApplyTemplate: DefaultStyleKey={DefaultStyleKey}, Template={Template}");
        try
        {
            base.OnApplyTemplate();
            Log($"OnApplyTemplate: done, Template={Template}");
        }
        catch (Exception ex)
        {
            Log($"OnApplyTemplate THREW: {ex.GetType().Name}: {ex.Message}");
            Log($"  StackTrace: {ex.StackTrace?.Split('\n')[0]}");
        }
    }

    // ── Uno input forwarding ─────────────────────────────────────────────────

    protected override void OnPointerPressed(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus(Microsoft.UI.Xaml.FocusState.Pointer);

        var te = TextEditor;
        Log($"PointerPressed: TextEditor={te?.GetType().Name ?? "null"}, TextView={te?.TextView?.GetType().Name ?? "null"}");
        if (te?.TextView?.RenderScope is not Microsoft.UI.Xaml.UIElement renderScope)
        {
            Log($"PointerPressed: early return — renderScope is null");
            return;
        }

        var unoPoint = e.GetCurrentPoint(renderScope).Position;
        Log($"PointerPressed at ({unoPoint.X:F1},{unoPoint.Y:F1})");
        _pressedPoint = new Point(unoPoint.X, unoPoint.Y);
        _pointerMovedSincePress = false;
        _pressedHyperlink = (renderScope as MS.Internal.Documents.FlowDocumentView)?.GetHyperlinkAt(unoPoint);

        try
        {
            TextEditorMouse.SetCaretPositionOnMouseEvent(
                te,
                new Point(unoPoint.X, unoPoint.Y),
                MouseButton.Left,
                1);
            Log($"PointerPressed: SetCaretPosition done");

            // Drive caret display from Florence hit-test directly (WPF TextContainer
            // offsets are unusable in the Uno shim — IMECharCount reports stub values).
            if (renderScope is MS.Internal.Documents.FlowDocumentView fdv)
            {
                fdv.SetCaretAt(unoPoint);
            }

            CapturePointer(e.Pointer);
            _isPointerSelecting = true;
        }
        catch (Exception ex)
        {
            Log($"PointerPressed: SetCaretPosition THREW {ex.GetType().Name}: {ex.Message}");
            Log($"  at {ex.StackTrace?.Split('\n')[0]}");
        }

        e.Handled = true;
    }

    protected override void OnPointerMoved(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerMoved(e);

        var te = TextEditor;
        if (te?.TextView?.RenderScope is not Microsoft.UI.Xaml.UIElement renderScope)
            return;

        var point = e.GetCurrentPoint(renderScope);
        var fdv = renderScope as MS.Internal.Documents.FlowDocumentView;
        if (fdv != null)
        {
            fdv.UpdatePointerCursor(point.Position);
        }

        if (!_isPointerSelecting)
            return;

        if (!point.Properties.IsLeftButtonPressed)
            return;

        if (!_pointerMovedSincePress
            && (Math.Abs(point.Position.X - _pressedPoint.X) > 4 || Math.Abs(point.Position.Y - _pressedPoint.Y) > 4))
        {
            _pointerMovedSincePress = true;
        }

        try
        {
            var cursorPosition = te.TextView.GetTextPositionFromPoint(new Point(point.Position.X, point.Position.Y), snapToText: true);
            if (cursorPosition != null && te.Selection is ITextSelection selection)
            {
                selection.ExtendSelectionByMouse(cursorPosition, forceWordSelection: false, forceParagraphSelection: false);
                if (fdv != null)
                {
                    fdv.SetCaretAt(selection.MovingPosition);
                }
            }
        }
        catch (Exception ex)
        {
            Log($"PointerMoved THREW {ex.GetType().Name}: {ex.Message}");
        }

        e.Handled = true;
    }

    protected override void OnPointerReleased(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (!_isPointerSelecting)
            return;

        var te = TextEditor;
        if (te?.TextView?.RenderScope is MS.Internal.Documents.FlowDocumentView fdv)
        {
            var point = e.GetCurrentPoint(fdv);
            var releasedHyperlink = fdv.GetHyperlinkAt(point.Position);
            if (!_pointerMovedSincePress
                && _pressedHyperlink is not null
                && ReferenceEquals(_pressedHyperlink, releasedHyperlink))
            {
                fdv.ActivateHyperlink(_pressedHyperlink);
                e.Handled = true;
            }

            fdv.UpdatePointerCursor(point.Position);
        }

        _isPointerSelecting = false;
        ReleasePointerCapture(e.Pointer);
        _pressedHyperlink = null;
        e.Handled = true;
    }

    protected override void OnPointerExited(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerExited(e);

        if (TextEditor?.TextView?.RenderScope is MS.Internal.Documents.FlowDocumentView fdv)
        {
            fdv.ClearPointerCursor();
        }
    }

    protected override void OnKeyDown(Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        base.OnKeyDown(e);

        var te = TextEditor;
        if (te == null) return;

        var wpfKey = MapVirtualKey(e.Key);
        if (wpfKey == Key.None) return;

        Log($"KeyDown: {e.Key} → {wpfKey}");

        var args = new KeyEventArgs
        {
            Key = wpfKey,
            OriginalSource = this,
            IsRepeat = e.KeyStatus.RepeatCount > 1,
        };
        TextEditorTyping.OnKeyDown(this, args);

        if (!args.Handled)
        {
            var command = GetNavigationCommand(wpfKey);
            if (command != null && command.CanExecute(null, this))
            {
                command.Execute(null, this);
                args.Handled = true;
                Log($"KeyDown: executed {command.Name}");
            }
        }

        if (args.Handled)
        {
            e.Handled = true;
            // After any handled key (navigation or typing), refresh the visual caret
            // from the TextEditor's current selection position. Without this the caret
            // rectangle stays at the pointer-click position even though the logical
            // position has moved.
            UpdateCaretFromSelection();
        }
    }

    protected override void OnCharacterReceived(Microsoft.UI.Xaml.Input.CharacterReceivedRoutedEventArgs e)
    {
        base.OnCharacterReceived(e);

        var te = TextEditor;
        if (te == null) return;

        char c = e.Character;
        // Filter control characters (handled by OnKeyDown / WPF command routing)
        if (c < 0x20 || c == 0x7F) return;

        Log($"CharacterReceived: '{c}' (U+{(int)c:X4})");

        var composition = new TextCompositionEventArgs(c.ToString())
        {
            OriginalSource = this,
        };
        TextEditorTyping.OnTextInput(this, composition);
        if (composition.Handled)
            e.Handled = true;
    }

    private void UpdateCaretFromSelection()
    {
        try
        {
            var te = TextEditor;
            if (te?.TextView?.RenderScope is not MS.Internal.Documents.FlowDocumentView fdv)
                return;
            var position = te.Selection?.MovingPosition;
            if (position == null)
                return;
            fdv.SetCaretAt(position);
            Log($"UpdateCaretFromSelection: offset={position.CharOffset} dir={position.LogicalDirection}");
        }
        catch (Exception ex)
        {
            Log($"UpdateCaretFromSelection THREW {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static Key MapVirtualKey(global::Windows.System.VirtualKey vk) => vk switch
    {
        global::Windows.System.VirtualKey.Left      => Key.Left,
        global::Windows.System.VirtualKey.Right     => Key.Right,
        global::Windows.System.VirtualKey.Up        => Key.Up,
        global::Windows.System.VirtualKey.Down      => Key.Down,
        global::Windows.System.VirtualKey.Home      => Key.Home,
        global::Windows.System.VirtualKey.End       => Key.End,
        global::Windows.System.VirtualKey.PageUp    => Key.PageUp,
        global::Windows.System.VirtualKey.PageDown  => Key.PageDown,
        global::Windows.System.VirtualKey.Back      => Key.Back,
        global::Windows.System.VirtualKey.Delete    => Key.Delete,
        global::Windows.System.VirtualKey.Enter     => Key.Return,
        global::Windows.System.VirtualKey.Tab       => Key.Tab,
        global::Windows.System.VirtualKey.Escape    => Key.Escape,
        _                                   => Key.None,
    };

    private static RoutedUICommand? GetNavigationCommand(Key key) => key switch
    {
        Key.Left => System.Windows.Documents.EditingCommands.MoveLeftByCharacter,
        Key.Right => System.Windows.Documents.EditingCommands.MoveRightByCharacter,
        Key.Up => System.Windows.Documents.EditingCommands.MoveUpByLine,
        Key.Down => System.Windows.Documents.EditingCommands.MoveDownByLine,
        Key.Home => System.Windows.Documents.EditingCommands.MoveToLineStart,
        Key.End => System.Windows.Documents.EditingCommands.MoveToLineEnd,
        Key.PageUp => System.Windows.Documents.EditingCommands.MoveUpByPage,
        Key.PageDown => System.Windows.Documents.EditingCommands.MoveDownByPage,
        _ => null,
    };
}
#endif
