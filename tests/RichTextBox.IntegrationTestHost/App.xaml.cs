using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Hosting;

namespace RichTextBox.IntegrationTestHost;

public partial class App : Application
{
    public App()
    {
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Windows.UI.ViewManagement.ApplicationView.PreferredLaunchViewSize =
            new global::Windows.Foundation.Size(900, 600);

        MainWindow = new Window
        {
            Title = "RichTextBox.IntegrationTestHost",
        };

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
