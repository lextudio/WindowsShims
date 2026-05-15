namespace System.Windows.Input
{
    public class QueryCursorEventArgs : MouseEventArgs
    {
        public new bool Handled { get; set; }
        public object? Cursor { get; set; }
    }
}
