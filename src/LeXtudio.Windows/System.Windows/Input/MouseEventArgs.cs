namespace System.Windows.Input
{
    public class MouseEventArgs : EventArgs
    {
        public bool Handled { get; set; }
        public bool UserInitiated { get; set; } = true;
        public MouseButtonState LeftButton { get; set; }
        public MouseButtonState RightButton { get; set; }
        public MouseButtonState MiddleButton { get; set; }
        public Point GetPosition(object? relativeTo) => default;
    }
}
