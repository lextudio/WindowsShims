namespace System.Windows.Shell
{
    // Minimal WPF taskbar-progress shim. ILSpy's DecompilerTextView updates
    // MainWindow.TaskbarItemInfo during decompilation; on Uno there is no WPF taskbar item,
    // so MainWindow.TaskbarItemInfo stays null and these blocks are inert — this type only
    // needs to exist for the code to compile.
    public enum TaskbarItemProgressState
    {
        None,
        Indeterminate,
        Normal,
        Error,
        Paused
    }

    public sealed class TaskbarItemInfo
    {
        public TaskbarItemProgressState ProgressState { get; set; }
        public double ProgressValue { get; set; }
    }
}
