namespace System.Windows.Input
{
    public enum MouseButton
    {
        Left,
        Middle,
        Right,
        XButton1,
        XButton2,
    }

    public class MouseButtonEventArgs : MouseEventArgs
    {
        public int ClickCount { get; set; }
        public MouseButtonState ButtonState { get; set; } = MouseButtonState.Released;
        public MouseButton ChangedButton { get; set; }
        public new Point GetPosition(object? relativeTo) => default;
    }
}
