namespace System.Windows
{
    public class KeyEventArgs : EventArgs
    {
        public bool Handled { get; set; }
        public System.Windows.Input.Key Key { get; set; }
        public bool UserInitiated { get; set; } = true;
    }
}
