namespace System.Windows.Input
{
    // WPF input-device base; only identity matters to the bridged callers.
    public abstract class InputDevice
    {
    }

    public class KeyboardDevice : InputDevice
    {
        public static readonly KeyboardDevice Empty = new();
        public ModifierKeys Modifiers => Keyboard.Modifiers;
        public bool IsKeyToggled(Key key) => false;
    }
}

namespace System.Windows
{
    public class KeyEventArgs : RoutedEventArgs
    {
        public bool Handled { get; set; }
        public System.Windows.Input.Key Key { get; set; }
        public bool UserInitiated { get; set; } = true;
        public System.Windows.Input.KeyboardDevice KeyboardDevice { get; set; } = System.Windows.Input.KeyboardDevice.Empty;
        public object? OriginalSource { get; set; }
        public bool IsRepeat { get; set; }
    }
}
