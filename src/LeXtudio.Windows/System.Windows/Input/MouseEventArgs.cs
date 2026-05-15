namespace System.Windows.Input
{
    public class MouseEventArgs : EventArgs
    {
        public bool Handled { get; set; }
        public bool UserInitiated { get; set; } = true;
    }
}
