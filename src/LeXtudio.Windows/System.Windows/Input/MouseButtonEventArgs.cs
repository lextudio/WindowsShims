namespace System.Windows.Input
{
    public class MouseButtonEventArgs : MouseEventArgs
    {
        public int ClickCount { get; set; }
        public MouseButtonState ButtonState { get; set; } = MouseButtonState.Released;
    }
}
