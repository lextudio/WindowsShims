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
