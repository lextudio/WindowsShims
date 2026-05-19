namespace System.Windows.Input
{
    public class KeyboardDevice
    {
        public static readonly KeyboardDevice Empty = new();
        public ModifierKeys Modifiers => Keyboard.Modifiers;
        public bool IsKeyToggled(Key key) => false;
    }
}

namespace System.Windows
{
    public class KeyEventArgs : EventArgs
    {
        public bool Handled { get; set; }
        public System.Windows.Input.Key Key { get; set; }
        public bool UserInitiated { get; set; } = true;
        public System.Windows.Input.KeyboardDevice KeyboardDevice { get; set; } = System.Windows.Input.KeyboardDevice.Empty;
        public object? OriginalSource { get; set; }
        public bool IsRepeat { get; set; }
    }
}
