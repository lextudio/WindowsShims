// Bridges the WPF file-dialog shims to the host app's window. On Windows App SDK the
// WinUI pickers must be initialized with the owning window's HWND before they are shown;
// on Uno (Skia) targets this is a no-op. The host sets ActiveWindow once at startup.

namespace Microsoft.Win32
{
    public static class FileDialogHost
    {
        // The window file pickers are parented to. Set this from the app once the main
        // window exists (e.g. FileDialogHost.ActiveWindow = MainWindow;).
        public static Microsoft.UI.Xaml.Window? ActiveWindow { get; set; }

        // Associates a picker with ActiveWindow. Required by FileOpenPicker/FileSavePicker/
        // FolderPicker on Windows App SDK; harmless elsewhere.
        public static void InitializeWithActiveWindow(object picker)
        {
#if WINDOWS_APP_SDK
            if (ActiveWindow is { } window)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            }
#endif
        }
    }
}
