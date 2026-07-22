using Uno.UI.Hosting;

namespace RichTextBox.IntegrationTestHost;

public static class Program
{
    [STAThread]
    public static async Task Main(string[] args)
    {
        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseMacOS()
            .UseWin32()
            .Build();
        await host.RunAsync();
    }
}
