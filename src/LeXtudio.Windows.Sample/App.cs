using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LeXtudio.Windows.Sample;

public partial class App : Application
{
    private Window? _window;

    // Set by Program.Main when --probe is passed.
    public static bool ProbeMode { get; set; }

    public App()
    {
        Resources.MergedDictionaries.Add(new XamlControlsResources());
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new Window
        {
            Title = "LeXtudio.Windows DataGrid Sample",
        };
        _window.Content = new MainPage();
        _window.Activate();

        // In probe mode, MainPage verifies the rendered artifact after the
        // grid's Loaded event and exits the process with the probe verdict
        // (with its own fallback timeout). App does not force an early exit.
    }
}
