using Uno.UI.Hosting;

namespace LeXtudio.Windows.Sample;

public static class Program
{
    [STAThread]
    public static async Task Main(string[] args)
    {
        // --probe: exercise the DataGrid surface, print the report, and exit
        // without opening a window. Used to gate behavior work headlessly.
        App.ProbeMode = args.Contains("--probe");

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseMacOS()
            .UseWin32()
            .Build();
        await host.RunAsync();
    }
}
