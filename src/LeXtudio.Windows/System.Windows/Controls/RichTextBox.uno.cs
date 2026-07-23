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
    private bool _pressWasInsideSelection;
    private ulong _lastPressTimestamp;
    private Point _lastPressPointForClickCount;
    private int _clickCount;

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
            EnsureImeContext();
            EnsureDragDrop();
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

        var currentPoint = e.GetCurrentPoint(renderScope);
        var unoPoint = currentPoint.Position;
        Log($"PointerPressed at ({unoPoint.X:F1},{unoPoint.Y:F1})");
        _pressedPoint = new Point(unoPoint.X, unoPoint.Y);
        _pointerMovedSincePress = false;
        _pressWasInsideSelection = false;
        _pressedHyperlink = (renderScope as MS.Internal.Documents.FlowDocumentView)?.GetHyperlinkAt(unoPoint);
        int clickCount = ComputeClickCount(currentPoint.Timestamp, _pressedPoint);
        Log($"PointerPressed: clickCount={clickCount}");

        // A press landing inside the existing (non-empty) selection is a candidate drag-start,
        // not a new selection gesture — mirrors WPF's _DragDropProcess.SourceOnMouseLeftButtonDown.
        // Leave the selection and caret untouched so a subsequent pointer move can raise
        // DragStarting with the original selection intact instead of collapsing it immediately.
        if (_dragDrop is not null && te.Selection is { IsEmpty: false } selection)
        {
            var pressOffset = ((IRichTextDragDropHost)this).HitTest(new Point(unoPoint.X, unoPoint.Y));
            var (selMin, selMax) = ((IRichTextDragDropHost)this).GetSelectionRange();
            bool pressInsideSelection = pressOffset >= selMin && pressOffset < selMax;
            _dragDrop.UpdateCanDrag(pressInsideSelection);
            if (pressInsideSelection)
            {
                // Do not capture the pointer here: WinUI's own drag-gesture recognizer needs
                // to see the subsequent pointer-move-with-button-down itself to raise
                // DragStarting; capturing it now would starve that recognizer. If no drag
                // actually happens, OnPointerReleased collapses the selection to the release
                // point instead, matching WPF's plain-click-inside-selection behavior.
                _pressWasInsideSelection = true;
                Log("PointerPressed: inside existing selection, deferring to drag-start");
                e.Handled = true;
                return;
            }
        }

        try
        {
            TextEditorMouse.SetCaretPositionOnMouseEvent(
                te,
                new Point(unoPoint.X, unoPoint.Y),
                MouseButton.Left,
                clickCount);
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

        // Track movement past the drag threshold even when a press-inside-selection deferred
        // to drag-start (which leaves _isPointerSelecting false and the pointer uncaptured) —
        // OnPointerReleased needs this to distinguish a plain click from a drag attempt.
        if (point.Properties.IsLeftButtonPressed
            && !_pointerMovedSincePress
            && (Math.Abs(point.Position.X - _pressedPoint.X) > 4 || Math.Abs(point.Position.Y - _pressedPoint.Y) > 4))
        {
            _pointerMovedSincePress = true;
        }

        if (!_isPointerSelecting)
            return;

        if (!point.Properties.IsLeftButtonPressed)
            return;

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
        {
            // A press that landed inside the existing selection deferred to a possible
            // drag-start (see OnPointerPressed) instead of starting a normal selection
            // gesture. If the pointer never moved past the drag threshold, no drag actually
            // happened — collapse the selection to the release point here, matching WPF's
            // plain-click-inside-selection behavior (_DragDropProcess never got a DragStarting
            // to consume the click, so the click behaves like an ordinary caret placement).
            if (_pressWasInsideSelection && !_pointerMovedSincePress)
            {
                var teForClick = TextEditor;
                if (teForClick?.TextView?.RenderScope is Microsoft.UI.Xaml.UIElement clickRenderScope)
                {
                    var clickPoint = e.GetCurrentPoint(clickRenderScope).Position;
                    try
                    {
                        TextEditorMouse.SetCaretPositionOnMouseEvent(
                            teForClick,
                            new Point(clickPoint.X, clickPoint.Y),
                            MouseButton.Left,
                            1);
                        if (clickRenderScope is MS.Internal.Documents.FlowDocumentView clickFdv)
                            clickFdv.SetCaretAt(new Point(clickPoint.X, clickPoint.Y));
                        Log("PointerReleased: plain click inside selection, collapsed to click point");
                    }
                    catch (Exception ex)
                    {
                        Log($"PointerReleased: collapse-on-click THREW {ex.GetType().Name}: {ex.Message}");
                    }
                }
            }

            _pressWasInsideSelection = false;
            return;
        }

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

        // Letter keys are only forwarded to WPF when a modifier is held (e.g. Ctrl+C/V/Z).
        // Plain letter input arrives via OnCharacterReceived; intercepting it here would double-insert.
        if (wpfKey >= Key.A && wpfKey <= Key.Z &&
            (System.Windows.Input.Keyboard.Modifiers & (System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Alt)) == 0)
            return;

        // Give the platform IME first refusal — while composing (or when a modal candidate
        // window is up), it must see keys like arrows/Enter/Escape before WPF's own
        // navigation/typing commands do.
        if (_imeContext is not null)
        {
            bool shift = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) != 0;
            bool ctrl = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) != 0;
            if (_imeContext.ProcessKeyEvent((int)e.Key, shift, ctrl))
            {
                e.Handled = true;
                Log($"KeyDown: {e.Key} consumed by IME");
                return;
            }
        }

        Log($"KeyDown: {e.Key} → {wpfKey}");

        var mods = System.Windows.Input.Keyboard.Modifiers;
        if ((mods & System.Windows.Input.ModifierKeys.Control) != 0 &&
            (mods & System.Windows.Input.ModifierKeys.Alt) == 0)
        {
            if (wpfKey == Key.Z)
            {
                var command = (mods & System.Windows.Input.ModifierKeys.Shift) != 0
                    ? System.Windows.Input.ApplicationCommands.Redo
                    : System.Windows.Input.ApplicationCommands.Undo;
                if (command.CanExecute(null, this))
                {
                    command.Execute(null, this);
                    e.Handled = true;
                    UpdateCaretFromSelection();
                    Log($"KeyDown: executed {command.Name}");
                    return;
                }
            }

            if (wpfKey == Key.Y && System.Windows.Input.ApplicationCommands.Redo.CanExecute(null, this))
            {
                System.Windows.Input.ApplicationCommands.Redo.Execute(null, this);
                e.Handled = true;
                UpdateCaretFromSelection();
                Log($"KeyDown: executed {System.Windows.Input.ApplicationCommands.Redo.Name}");
                return;
            }

            var formattingCommand = wpfKey switch
            {
                Key.B => System.Windows.Documents.EditingCommands.ToggleBold,
                Key.I => System.Windows.Documents.EditingCommands.ToggleItalic,
                Key.U => System.Windows.Documents.EditingCommands.ToggleUnderline,
                _ => null,
            };

            if (formattingCommand != null && formattingCommand.CanExecute(null, this))
            {
                formattingCommand.Execute(null, this);
                e.Handled = true;
                UpdateCaretFromSelection();
                Log($"KeyDown: executed {formattingCommand.Name}");
                return;
            }
        }

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

    protected override void OnKeyUp(Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        base.OnKeyUp(e);

        var te = TextEditor;
        if (te == null) return;

        var wpfKey = MapVirtualKey(e.Key);
        if (wpfKey == Key.None) return;

        Log($"KeyUp: {e.Key} → {wpfKey}");

        var args = new KeyEventArgs
        {
            Key = wpfKey,
            OriginalSource = this,
            IsRepeat = e.KeyStatus.RepeatCount > 1,
        };
        TextEditorTyping.OnKeyUp(this, args);

        if (args.Handled)
        {
            e.Handled = true;
            UpdateCaretFromSelection();
        }
    }

    protected override void OnCharacterReceived(Microsoft.UI.Xaml.Input.CharacterReceivedRoutedEventArgs e)
    {
        base.OnCharacterReceived(e);

        var te = TextEditor;
        if (te == null) return;

        // While a native IME composition is in progress, composed text arrives through
        // CoreTextEditContext.TextUpdating instead — handling it here too would double-insert.
        if (_imeComposing) return;

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

    internal override void OnTextContainerChanged(object sender, TextContainerChangedEventArgs e)
    {
        base.OnTextContainerChanged(sender, e);

        if (TextEditor?.TextView?.RenderScope is MS.Internal.Documents.FlowDocumentView fdv)
        {
            fdv.InvalidateDocumentLayout();
            Log($"OnTextContainerChanged: invalidated FlowDocumentView layout");
        }
    }

    protected override void OnSelectionChanged(RoutedEventArgs e)
    {
        if (TextEditor?.TextView?.RenderScope is MS.Internal.Documents.FlowDocumentView fdv)
        {
            fdv.InvalidateDocumentLayout();
            Log($"OnSelectionChanged: invalidated FlowDocumentView layout");
        }

        base.OnSelectionChanged(e);
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
            NotifyImeOfCaretAndSelection();
        }
        catch (Exception ex)
        {
            Log($"UpdateCaretFromSelection THREW {ex.GetType().Name}: {ex.Message}");
        }
    }

    // Uno/WinUI pointer events have no built-in click-count concept (unlike WPF's
    // MouseButtonEventArgs.ClickCount), so this reimplements the standard desktop
    // double/triple-click detection: consecutive presses within a short time window and
    // close together spatially increment the count; anything else (too slow, moved too far)
    // restarts at 1. TextEditorMouse.SetCaretPositionOnMouseEvent uses this to distinguish
    // plain click (1: place caret) / double-click (2: select word) / triple-click
    // (3: select paragraph) — previously always hardcoded to 1, making word/paragraph
    // click-to-select entirely unreachable through the real pointer path.
    private int ComputeClickCount(ulong timestampMicroseconds, Point point)
    {
        const ulong maxIntervalMicroseconds = 500_000; // 500ms, typical OS double-click interval
        const double maxDistance = 4;

        bool sameSpot = Math.Abs(point.X - _lastPressPointForClickCount.X) <= maxDistance
            && Math.Abs(point.Y - _lastPressPointForClickCount.Y) <= maxDistance;
        bool withinInterval = _lastPressTimestamp != 0
            && timestampMicroseconds >= _lastPressTimestamp
            && timestampMicroseconds - _lastPressTimestamp <= maxIntervalMicroseconds;

        _clickCount = (sameSpot && withinInterval) ? _clickCount + 1 : 1;
        if (_clickCount > 3)
            _clickCount = 1;

        _lastPressTimestamp = timestampMicroseconds;
        _lastPressPointForClickCount = point;
        return _clickCount;
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
        global::Windows.System.VirtualKey.Shift     => Key.LeftShift,
        global::Windows.System.VirtualKey.LeftShift => Key.LeftShift,
        global::Windows.System.VirtualKey.RightShift => Key.RightShift,
        global::Windows.System.VirtualKey.Control   => Key.LeftCtrl,
        global::Windows.System.VirtualKey.LeftControl => Key.LeftCtrl,
        global::Windows.System.VirtualKey.RightControl => Key.RightCtrl,
        // Letter keys — needed so Ctrl+A/C/V/X/Z/Y/B/I/U reach TextEditorTyping
        global::Windows.System.VirtualKey.A         => Key.A,
        global::Windows.System.VirtualKey.B         => Key.B,
        global::Windows.System.VirtualKey.C         => Key.C,
        global::Windows.System.VirtualKey.I         => Key.I,
        global::Windows.System.VirtualKey.U         => Key.U,
        global::Windows.System.VirtualKey.V         => Key.V,
        global::Windows.System.VirtualKey.X         => Key.X,
        global::Windows.System.VirtualKey.Y         => Key.Y,
        global::Windows.System.VirtualKey.Z         => Key.Z,
        _                                            => Key.None,
    };

    private static RoutedUICommand? GetNavigationCommand(Key key)
    {
        bool shift = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) != 0;
        bool ctrl  = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) != 0;
        return (key, shift, ctrl) switch
        {
            // Shift+Ctrl — select to document boundary
            (Key.Home, true, true)     => System.Windows.Documents.EditingCommands.SelectToDocumentStart,
            (Key.End,  true, true)     => System.Windows.Documents.EditingCommands.SelectToDocumentEnd,
            // Shift — extend selection
            (Key.Left,     true, _)    => System.Windows.Documents.EditingCommands.SelectLeftByCharacter,
            (Key.Right,    true, _)    => System.Windows.Documents.EditingCommands.SelectRightByCharacter,
            (Key.Up,       true, _)    => System.Windows.Documents.EditingCommands.SelectUpByLine,
            (Key.Down,     true, _)    => System.Windows.Documents.EditingCommands.SelectDownByLine,
            (Key.Home,     true, _)    => System.Windows.Documents.EditingCommands.SelectToLineStart,
            (Key.End,      true, _)    => System.Windows.Documents.EditingCommands.SelectToLineEnd,
            (Key.PageUp,   true, _)    => System.Windows.Documents.EditingCommands.SelectUpByPage,
            (Key.PageDown, true, _)    => System.Windows.Documents.EditingCommands.SelectDownByPage,
            // Ctrl — word / document navigation
            (Key.Left,  _, true)       => System.Windows.Documents.EditingCommands.MoveLeftByWord,
            (Key.Right, _, true)       => System.Windows.Documents.EditingCommands.MoveRightByWord,
            (Key.Home,  _, true)       => System.Windows.Documents.EditingCommands.MoveToDocumentStart,
            (Key.End,   _, true)       => System.Windows.Documents.EditingCommands.MoveToDocumentEnd,
            // Plain navigation
            (Key.Left,     _, _)       => System.Windows.Documents.EditingCommands.MoveLeftByCharacter,
            (Key.Right,    _, _)       => System.Windows.Documents.EditingCommands.MoveRightByCharacter,
            (Key.Up,       _, _)       => System.Windows.Documents.EditingCommands.MoveUpByLine,
            (Key.Down,     _, _)       => System.Windows.Documents.EditingCommands.MoveDownByLine,
            (Key.Home,     _, _)       => System.Windows.Documents.EditingCommands.MoveToLineStart,
            (Key.End,      _, _)       => System.Windows.Documents.EditingCommands.MoveToLineEnd,
            (Key.PageUp,   _, _)       => System.Windows.Documents.EditingCommands.MoveUpByPage,
            (Key.PageDown, _, _)       => System.Windows.Documents.EditingCommands.MoveDownByPage,
            // Editing
            (Key.Delete, _, true)      => System.Windows.Documents.EditingCommands.DeleteNextWord,
            (Key.Back,   _, true)      => System.Windows.Documents.EditingCommands.DeletePreviousWord,
            (Key.Delete, _, _)         => System.Windows.Documents.EditingCommands.Delete,
            (Key.Back,   _, _)         => System.Windows.Documents.EditingCommands.Backspace,
            (Key.Return, true, _)      => System.Windows.Documents.EditingCommands.EnterLineBreak,
            (Key.Return, _,    _)      => System.Windows.Documents.EditingCommands.EnterParagraphBreak,
            _                          => null,
        };
    }
}
#endif
