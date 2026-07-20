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
