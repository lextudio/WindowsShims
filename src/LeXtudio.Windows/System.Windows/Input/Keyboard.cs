namespace System.Windows.Input
{
    public static class Keyboard
    {
        public static ModifierKeys Modifiers => ModifierKeys.None;
        public static IInputElement? FocusedElement => null;

        public static readonly System.Windows.RoutedEvent GotKeyboardFocusEvent = new();
        public static readonly System.Windows.RoutedEvent LostKeyboardFocusEvent = new();
    }
}
