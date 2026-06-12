namespace System.Windows.Input
{
    public delegate void KeyEventHandler(object sender, System.Windows.KeyEventArgs e);

    public static class Keyboard
    {
        public static ModifierKeys Modifiers
        {
            get
            {
#if HAS_UNO
                var modifiers = ModifierKeys.None;
                var shift = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(
                    global::Windows.System.VirtualKey.Shift);
                var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(
                    global::Windows.System.VirtualKey.Control);
                var alt = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(
                    global::Windows.System.VirtualKey.Menu);
                if ((shift & global::Windows.UI.Core.CoreVirtualKeyStates.Down) != 0)
                    modifiers |= ModifierKeys.Shift;
                if ((ctrl & global::Windows.UI.Core.CoreVirtualKeyStates.Down) != 0)
                    modifiers |= ModifierKeys.Control;
                if ((alt & global::Windows.UI.Core.CoreVirtualKeyStates.Down) != 0)
                    modifiers |= ModifierKeys.Alt;
                return modifiers;
#else
                return ModifierKeys.None;
#endif
            }
        }
        public static IInputElement? FocusedElement => null;

        public static KeyboardDevice PrimaryDevice => KeyboardDevice.Empty;

        // Programmatic focus moves are not bridged to Uno's focus manager yet;
        // the element is reported back as if focus succeeded.
        public static IInputElement? Focus(IInputElement? element) => element;

        public static readonly System.Windows.RoutedEvent GotKeyboardFocusEvent = new();
        public static readonly System.Windows.RoutedEvent LostKeyboardFocusEvent = new();
        public static readonly System.Windows.RoutedEvent PreviewKeyDownEvent = new();
        public static readonly System.Windows.RoutedEvent KeyDownEvent = new();
        public static readonly System.Windows.RoutedEvent KeyUpEvent = new();
        public static readonly System.Windows.RoutedEvent PreviewKeyUpEvent = new();
    }
}
