// WPF-API shim for System.Windows.MessageBox. The real WPF MessageBox (PresentationFramework
// System/Windows/MessageBox.cs) calls the Win32 MessageBox API directly and cannot run on
// Skia/macOS. This shim mirrors the public API so upstream ILSpy code compiles; a host that
// wants real dialogs can route Show through its own UI.

namespace System.Windows
{
    public enum MessageBoxButton
    {
        OK = 0,
        OKCancel = 1,
        YesNoCancel = 3,
        YesNo = 4
    }

    public enum MessageBoxImage
    {
        None = 0,
        Error = 16,
        Hand = 16,
        Stop = 16,
        Question = 32,
        Exclamation = 48,
        Warning = 48,
        Asterisk = 64,
        Information = 64
    }

    public enum MessageBoxResult
    {
        None = 0,
        OK = 1,
        Cancel = 2,
        Yes = 6,
        No = 7
    }

    [Flags]
    public enum MessageBoxOptions
    {
        None = 0,
        DefaultDesktopOnly = 0x20000,
        RightAlign = 0x80000,
        RtlReading = 0x100000,
        ServiceNotification = 0x200000
    }

    public static class MessageBox
    {
        public static MessageBoxResult Show(string messageBoxText) => MessageBoxResult.None;

        public static MessageBoxResult Show(string messageBoxText, string? caption) => MessageBoxResult.None;

        public static MessageBoxResult Show(string messageBoxText, string? caption, MessageBoxButton button)
            => Default(button);

        public static MessageBoxResult Show(string messageBoxText, string? caption, MessageBoxButton button, MessageBoxImage icon)
            => Default(button);

        public static MessageBoxResult Show(string messageBoxText, string? caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
            => defaultResult;

        public static MessageBoxResult Show(string messageBoxText, string? caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
            => defaultResult;

        public static MessageBoxResult Show(object? owner, string messageBoxText) => MessageBoxResult.None;

        public static MessageBoxResult Show(object? owner, string messageBoxText, string? caption, MessageBoxButton button)
            => Default(button);

        public static MessageBoxResult Show(object? owner, string messageBoxText, string? caption, MessageBoxButton button, MessageBoxImage icon)
            => Default(button);

        public static MessageBoxResult Show(object? owner, string messageBoxText, string? caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
            => defaultResult;

        private static MessageBoxResult Default(MessageBoxButton button)
            => button == MessageBoxButton.OK ? MessageBoxResult.OK : MessageBoxResult.None;
    }
}
