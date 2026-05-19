namespace System.Windows.Input
{
    public delegate void KeyEventHandler(object sender, System.Windows.KeyEventArgs e);

    public static class Keyboard
    {
        public static ModifierKeys Modifiers => ModifierKeys.None;
        public static IInputElement? FocusedElement => null;

        public static readonly System.Windows.RoutedEvent GotKeyboardFocusEvent = new();
        public static readonly System.Windows.RoutedEvent LostKeyboardFocusEvent = new();
        public static readonly System.Windows.RoutedEvent PreviewKeyDownEvent = new();
        public static readonly System.Windows.RoutedEvent KeyDownEvent = new();
        public static readonly System.Windows.RoutedEvent KeyUpEvent = new();
        public static readonly System.Windows.RoutedEvent PreviewKeyUpEvent = new();
    }
}
