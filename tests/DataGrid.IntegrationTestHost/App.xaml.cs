using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Hosting;

namespace DataGrid.IntegrationTestHost;

public partial class App : Application
{
    public App()
    {
        this.InitializeComponent();
    }

    internal static Window? CurrentMainWindow => ((App)Current).MainWindow;

    /// <summary>Window content origin in global screen points (DIPs).</summary>
    ///
    /// <remarks>
    /// On macOS, <see cref="Microsoft.UI.Xaml.Window.AppWindow"/>'s <c>Position</c> does not
    /// reliably line up with the window's *content* origin the way native
    /// <c>NSWindow.contentView.bounds</c> + <c>convertRectToScreen:</c> does (it can report the
    /// outer window-frame origin, in the wrong units, or simply drift from the content area by
    /// roughly a title-bar height). A synthesized drag computed from the wrong origin silently
    /// lands outside the actual header row — e.g. on the title bar — instead of throwing, so this
    /// class of bug shows up as "the drag path goes outside the window" rather than a crash.
    /// UnoDock's <c>DockingManager.ComputeScreenOriginQ()</c> hit the exact same problem and fixed
    /// it by preferring the native ObjC content-origin lookup first, falling back to
    /// <c>AppWindow.Position</c> (with an explicit title-bar-height correction) only when the
    /// native lookup is unavailable — mirrored here.
    /// </remarks>
    internal static (double X, double Y) GetWindowOrigin()
    {
        var window = CurrentMainWindow;
        if (window is null) return (0, 0);

        // Prefer the native macOS content-origin lookup: it reads the real NSWindow content
        // view's bounds via convertRectToScreen:, in the same Quartz global-point, top-left-origin
        // space MacOSNativeInput.TryMouseDrag/CGEventPost require — no unit/space guessing.
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.OSX))
        {
            try
            {
                var nsWindow = MacOSWindowHelper.GetMainNSWindow();
                var (ox, oy) = nsWindow != IntPtr.Zero ? MacOSWindowHelper.GetWindowContentOrigin(nsWindow) : (0, 0);
                try { System.IO.File.AppendAllText("/tmp/datagrid-drag.log", $"[origin] nsWindow=0x{nsWindow:X} native=({ox},{oy})\n"); } catch { }
                if (ox != 0 || oy != 0)
                    return (ox, oy);
            }
            catch (Exception ex)
            {
                try { System.IO.File.AppendAllText("/tmp/datagrid-drag.log", $"[origin] native lookup THREW {ex.GetType().Name}: {ex.Message}\n"); } catch { }
            }
        }

        // Fallback: WinRT AppWindow.Position (reliable on Windows; on this Uno-Skia-macOS target
        // it's a last resort only, since it doesn't match the content origin above). Matches
        // UnoDock's fallback: add an explicit title-bar-height correction on macOS, since
        // AppWindow.Position there is understood to be the outer-frame origin, not the content origin.
        try
        {
            var appWindow = window.AppWindow;
            var pos = appWindow.Position;
            try { System.IO.File.AppendAllText("/tmp/datagrid-drag.log", $"[origin] AppWindow.Position=({pos.X},{pos.Y})\n"); } catch { }
            if (pos.X != 0 || pos.Y != 0)
            {
                var scale = window.Content.XamlRoot?.RasterizationScale ?? 1.0;
                const double macOsTitleBarHeight = 28.0;
                double titleBarCorrection = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                    System.Runtime.InteropServices.OSPlatform.OSX)
                    ? macOsTitleBarHeight
                    : 0.0;
                return (pos.X / scale, pos.Y / scale + titleBarCorrection);
            }
        }
        catch (Exception ex)
        {
            try { System.IO.File.AppendAllText("/tmp/datagrid-drag.log", $"[origin] AppWindow.Position THREW {ex.GetType().Name}: {ex.Message}\n"); } catch { }
        }

        return (0, 0);
    }

    protected Window? MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Default window is uncomfortably small for the scenario gallery's
        // left-nav + card layout; set the preferred launch size before the
        // window is created (AppWindow.Resize() after construction/Activate
        // didn't stick on this target — the native window's own initial size
        // appears to win the race). PreferredLaunchWindowingMode is flagged
        // "not implemented" on this Uno/Skia-macOS target and verified to have
        // no effect either way — PreferredLaunchViewSize alone is sufficient.
        Windows.UI.ViewManagement.ApplicationView.PreferredLaunchViewSize = new global::Windows.Foundation.Size(1200, 800);

        MainWindow = new Window();
        MainWindow.Title = "DataGrid.IntegrationTestHost";

        if (MainWindow.Content is not Frame rootFrame)
        {
            rootFrame = new Frame();
            MainWindow.Content = rootFrame;
        }

        if (rootFrame.Content == null)
        {
            rootFrame.Navigate(typeof(MainPage), args.Arguments);
        }

        MainWindow.Activate();

#if DEBUG
        new LeXtudio.DevFlow.Agent.Uno.UnoAgentService(
            new Microsoft.Maui.DevFlow.Agent.Core.AgentOptions { Port = 9224 }).Start();
#endif
    }
}
