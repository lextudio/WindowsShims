// WPF-API shim for System.Windows.MessageBox. Provides real synchronous dialogs on all platforms:
// Windows: P/Invoke user32!MessageBoxW; macOS: osascript; Linux: zenity/kdialog.

using System.Diagnostics;
using System.Runtime.InteropServices;

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
#if WINDOWS_APP_SDK
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, int type);
#endif

        public static MessageBoxResult Show(string messageBoxText)
            => ShowCore(messageBoxText, string.Empty, MessageBoxButton.OK, MessageBoxImage.None);

        public static MessageBoxResult Show(string messageBoxText, string? caption)
            => ShowCore(messageBoxText, caption ?? string.Empty, MessageBoxButton.OK, MessageBoxImage.None);

        public static MessageBoxResult Show(string messageBoxText, string? caption, MessageBoxButton button)
            => ShowCore(messageBoxText, caption ?? string.Empty, button, MessageBoxImage.None);

        public static MessageBoxResult Show(string messageBoxText, string? caption, MessageBoxButton button, MessageBoxImage icon)
            => ShowCore(messageBoxText, caption ?? string.Empty, button, icon);

        public static MessageBoxResult Show(string messageBoxText, string? caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
            => ShowCore(messageBoxText, caption ?? string.Empty, button, icon);

        public static MessageBoxResult Show(string messageBoxText, string? caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
            => ShowCore(messageBoxText, caption ?? string.Empty, button, icon);

        public static MessageBoxResult Show(object? owner, string messageBoxText)
            => ShowCore(messageBoxText, string.Empty, MessageBoxButton.OK, MessageBoxImage.None);

        public static MessageBoxResult Show(object? owner, string messageBoxText, string? caption, MessageBoxButton button)
            => ShowCore(messageBoxText, caption ?? string.Empty, button, MessageBoxImage.None);

        public static MessageBoxResult Show(object? owner, string messageBoxText, string? caption, MessageBoxButton button, MessageBoxImage icon)
            => ShowCore(messageBoxText, caption ?? string.Empty, button, icon);

        public static MessageBoxResult Show(object? owner, string messageBoxText, string? caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
            => ShowCore(messageBoxText, caption ?? string.Empty, button, icon);

        private static MessageBoxResult ShowCore(string text, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
#if WINDOWS_APP_SDK
            int type = (int)button | (int)icon;
            int result = MessageBoxW(IntPtr.Zero, text, caption, type);
            return result >= 0 && Enum.IsDefined(typeof(MessageBoxResult), result)
                ? (MessageBoxResult)result
                : Default(button);
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return ShowMacOs(text, caption, button);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return ShowLinux(text, caption, button);
            return Default(button);
#endif
        }

#if !WINDOWS_APP_SDK
        private static MessageBoxResult ShowMacOs(string text, string caption, MessageBoxButton button)
        {
            string buttons = button switch
            {
                MessageBoxButton.YesNo => "{\"No\",\"Yes\"}",
                MessageBoxButton.YesNoCancel => "{\"Cancel\",\"No\",\"Yes\"}",
                MessageBoxButton.OKCancel => "{\"Cancel\",\"OK\"}",
                _ => "{\"OK\"}"
            };
            string defaultBtn = button switch
            {
                MessageBoxButton.YesNo or MessageBoxButton.YesNoCancel => "\"Yes\"",
                MessageBoxButton.OKCancel => "\"OK\"",
                _ => "\"OK\""
            };
            string safeText = text.Replace("\\", "\\\\").Replace("\"", "\\\"");
            string safeCaption = caption.Replace("\\", "\\\\").Replace("\"", "\\\"");
            string script = $"display dialog \"{safeText}\" buttons {buttons} default button {defaultBtn} with title \"{safeCaption}\"";

            try
            {
                using var proc = Process.Start(new ProcessStartInfo("osascript", $"-e '{script}'")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                });
                proc?.WaitForExit();
                string output = proc?.StandardOutput.ReadToEnd() ?? string.Empty;
                if (output.Contains("Yes")) return MessageBoxResult.Yes;
                if (output.Contains("No")) return MessageBoxResult.No;
                if (output.Contains("Cancel")) return MessageBoxResult.Cancel;
                if (output.Contains("OK")) return MessageBoxResult.OK;
            }
            catch { }
            return Default(button);
        }

        private static MessageBoxResult ShowLinux(string text, string caption, MessageBoxButton button)
        {
            // Try zenity first, then kdialog.
            string? tool = FindExecutable("zenity") ?? FindExecutable("kdialog");
            if (tool == null)
                return Default(button);

            bool isZenity = tool.EndsWith("zenity", StringComparison.OrdinalIgnoreCase);

            string args;
            if (isZenity)
            {
                args = button switch
                {
                    MessageBoxButton.YesNo or MessageBoxButton.YesNoCancel =>
                        $"--question --text={Quote(text)} --title={Quote(caption)}",
                    MessageBoxButton.OKCancel =>
                        $"--question --ok-label=OK --cancel-label=Cancel --text={Quote(text)} --title={Quote(caption)}",
                    _ =>
                        $"--info --text={Quote(text)} --title={Quote(caption)}"
                };
            }
            else // kdialog
            {
                args = button switch
                {
                    MessageBoxButton.YesNo =>
                        $"--yesno {Quote(text)} --title {Quote(caption)}",
                    MessageBoxButton.YesNoCancel =>
                        $"--yesnocancel {Quote(text)} --title {Quote(caption)}",
                    MessageBoxButton.OKCancel =>
                        $"--okcancel {Quote(text)} --title {Quote(caption)}",
                    _ =>
                        $"--msgbox {Quote(text)} --title {Quote(caption)}"
                };
            }

            try
            {
                using var proc = Process.Start(new ProcessStartInfo(tool, args) { UseShellExecute = false });
                proc?.WaitForExit();
                int exitCode = proc?.ExitCode ?? 0;

                if (isZenity)
                {
                    return button switch
                    {
                        MessageBoxButton.YesNo or MessageBoxButton.YesNoCancel =>
                            exitCode == 0 ? MessageBoxResult.Yes : MessageBoxResult.No,
                        MessageBoxButton.OKCancel =>
                            exitCode == 0 ? MessageBoxResult.OK : MessageBoxResult.Cancel,
                        _ => MessageBoxResult.OK
                    };
                }
                else
                {
                    // kdialog: 0=Yes/OK, 1=No, 2=Cancel
                    return button switch
                    {
                        MessageBoxButton.YesNo =>
                            exitCode == 0 ? MessageBoxResult.Yes : MessageBoxResult.No,
                        MessageBoxButton.YesNoCancel =>
                            exitCode == 0 ? MessageBoxResult.Yes :
                            exitCode == 1 ? MessageBoxResult.No : MessageBoxResult.Cancel,
                        MessageBoxButton.OKCancel =>
                            exitCode == 0 ? MessageBoxResult.OK : MessageBoxResult.Cancel,
                        _ => MessageBoxResult.OK
                    };
                }
            }
            catch { }
            return Default(button);
        }

        private static string? FindExecutable(string name)
        {
            try
            {
                using var proc = Process.Start(new ProcessStartInfo("which", name)
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                });
                proc?.WaitForExit();
                string path = proc?.StandardOutput.ReadToEnd().Trim() ?? string.Empty;
                return path.Length > 0 ? path : null;
            }
            catch { return null; }
        }

        private static string Quote(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
#endif

        private static MessageBoxResult Default(MessageBoxButton button)
            => button == MessageBoxButton.OK ? MessageBoxResult.OK : MessageBoxResult.None;
    }
}
