using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace RichTextBox.IntegrationTestHost;

public partial class App : Application
{
#if WINDOWS_APP_SDK
    internal static readonly string UnhandledLogPath =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "rtb-winui-unhandled.log");
#endif

    public App()
    {
        this.InitializeComponent();

#if WINDOWS_APP_SDK
        // Some WPF-sourced work runs on dispatcher callbacks (caret updates, layout passes).
        // An exception there has no caller to observe it, so under WinAppSDK it terminates
        // the process — which shows up as every later test failing with "connection refused"
        // instead of one test failing. Record it and keep the host alive so the suite still
        // reports real per-test results.
        UnhandledException += (_, e) =>
        {
            try
            {
                System.IO.File.AppendAllText(
                    UnhandledLogPath,
                    $"{DateTime.Now:HH:mm:ss.fff} {e.Exception.GetType().FullName}: {e.Message}\n{e.Exception.StackTrace}\n\n");
            }
            catch { }
            e.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            try
            {
                System.IO.File.AppendAllText(
                    UnhandledLogPath, $"{DateTime.Now:HH:mm:ss.fff} [AppDomain] {e.ExceptionObject}\n\n");
            }
            catch { }
        };

        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            try
            {
                System.IO.File.AppendAllText(
                    UnhandledLogPath, $"{DateTime.Now:HH:mm:ss.fff} [Task] {e.Exception}\n\n");
            }
            catch { }
            e.SetObserved();
        };
#endif
    }

    protected Window? MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
#if !WINDOWS_APP_SDK
        // UWP-era sizing API; WinAppSDK drops it, so the WinUI 3 head sizes below instead.
        Windows.UI.ViewManagement.ApplicationView.PreferredLaunchViewSize =
            new global::Windows.Foundation.Size(900, 600);
#endif

        MainWindow = new Window
        {
            Title = "RichTextBox.IntegrationTestHost",
        };

#if WINDOWS_APP_SDK
        // MainPage is a code-only Page (no .xaml), so it is absent from the XAML type
        // metadata that WinAppSDK's Frame.Navigate(Type) resolves through — navigating
        // to it faults in the ABI layer. The tests only need the page in the visual
        // tree, so host it directly.
        MainWindow.Content = new MainPage();
#else
        if (MainWindow.Content is not Frame rootFrame)
        {
            rootFrame = new Frame();
            MainWindow.Content = rootFrame;
        }

        if (rootFrame.Content == null)
        {
            rootFrame.Navigate(typeof(MainPage), args.Arguments);
        }
#endif

#if WINDOWS_APP_SDK
        // Match the Skia head's launch size so layout-sensitive probes (wrapping,
        // viewport scrolling) see the same viewport on both heads.
        MainWindow.AppWindow?.Resize(new global::Windows.Graphics.SizeInt32(900, 600));
#endif

        MainWindow.Activate();

#if DEBUG
        new LeXtudio.DevFlow.Agent.Uno.UnoAgentService(
            new Microsoft.Maui.DevFlow.Agent.Core.AgentOptions { Port = 9224 }).Start();
#endif
    }
}
