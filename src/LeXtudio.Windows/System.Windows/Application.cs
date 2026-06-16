namespace System.Windows
{
    // Window state, matching WPF's System.Windows.WindowState. Lives here (not in the
    // ext Window.cs, whose body is #if !HAS_UNO) so linked ILSpy code that does
    // `using System.Windows;` resolves WindowState.Minimized/Normal in the Uno build.
    public enum WindowState
    {
        Normal,
        Minimized,
        Maximized
    }

    // Minimal stand-in for the WPF main window. The Uno host's real window is a
    // Microsoft.UI.Xaml.Window (no WindowState/Title surface), so Application.MainWindow
    // exposes this shim carrying only the members linked ILSpy code touches
    // (WindowState for single-instance activation, Title for the assembly-list caption).
    public sealed class ShellWindow
    {
        public WindowState WindowState { get; set; } = WindowState.Normal;
        public string? Title { get; set; }
    }

    // Minimal WPF Application shim. The real PresentationFramework Application
    // (DispatcherObject, IHaveResources, IQueryAmbient) is far too heavy to link;
    // linked upstream code only reaches for Application.Current.Dispatcher and
    // Application.Current.MainWindow. On Uno the hosting app is a
    // Microsoft.UI.Xaml.Application, so this WPF-shaped surface stays inert:
    // Current is a lazily-created singleton, MainWindow defaults to null.
    public class Application
    {
        private static Application? _current;

        public static Application Current => _current ??= new Application();

        // Allows the Uno host to register the active application instance if it
        // wants Application.Current to resolve to a specific object.
        public static void RegisterCurrent(Application application) => _current = application;

        public System.Windows.Threading.Dispatcher Dispatcher { get; }
            = System.Windows.Threading.Dispatcher.CurrentDispatcher;

        public ShellWindow? MainWindow { get; set; }
    }
}
