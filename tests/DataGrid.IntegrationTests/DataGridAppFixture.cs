using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

using Xunit;

namespace DataGrid.IntegrationTests;

// Launches the DataGrid.IntegrationTestHost app once for a test collection, waits for the DevFlow
// agent (port 9224) to come up, and exposes helpers to invoke "datagrid.probe.*" actions.
// Disposing kills the app.
public sealed class DataGridAppFixture : IAsyncLifetime
{
    const int Port = 9224;
    static readonly string BaseUrl = $"http://localhost:{Port}";

    readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    Process? _app;

    public string HostProjectPath { get; } = LocateHostProject();

    public async Task InitializeAsync()
    {
        StopApp();
        await WaitForPortFreeAsync(TimeSpan.FromSeconds(30));
        await StartAsync();
    }

    public async Task DisposeAsync()
    {
        StopApp();
        _http.Dispose();
        await Task.CompletedTask;
    }

    async Task StartAsync()
    {
        var psi = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Path.GetDirectoryName(HostProjectPath)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var a in new[] { "run", "--project", HostProjectPath, "-f", "net10.0-desktop", "--no-build" })
            psi.ArgumentList.Add(a);

        _app = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start DataGrid.IntegrationTestHost");
        _app.OutputDataReceived += (_, _) => { };
        _app.ErrorDataReceived += (_, _) => { };
        _app.BeginOutputReadLine();
        _app.BeginErrorReadLine();
        await WaitForAgentAsync(TimeSpan.FromSeconds(90));
        await WarmUpAsync(TimeSpan.FromSeconds(60));
    }

    async Task WarmUpAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var s = await InvokeAsync("datagrid.probe.state");
                return;
            }
            catch { }
            await Task.Delay(500);
        }
        throw new TimeoutException("DataGrid host did not respond within " + timeout);
    }

    void StopApp()
    {
        try { if (_app is { HasExited: false }) _app.Kill(entireProcessTree: true); } catch { }
        try { foreach (var p in Process.GetProcessesByName("DataGrid.IntegrationTestHost")) { try { p.Kill(true); } catch { } } } catch { }
        _app = null;
    }

    async Task WaitForAgentAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var resp = await _http.GetAsync($"{BaseUrl}/api/v1/agent/status");
                if (resp.IsSuccessStatusCode) return;
            }
            catch { }
            await Task.Delay(1000);
        }
        throw new TimeoutException($"DevFlow agent did not respond on {BaseUrl} within {timeout}.");
    }

    async Task WaitForPortFreeAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            bool gone = Process.GetProcessesByName("DataGrid.IntegrationTestHost").Length == 0;
            if (gone && !IsPortInUse(Port))
                return;
            await Task.Delay(500);
        }
    }

    static bool IsPortInUse(int port)
    {
        try
        {
            return System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpListeners().Any(ep => ep.Port == port);
        }
        catch { return false; }
    }

    public async Task<JsonElement> InvokeAsync(string action, params object[] args)
    {
        var body = JsonSerializer.Serialize(new { args });
        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        using var resp = await _http.PostAsync($"{BaseUrl}/api/v1/invoke/actions/{action}", content);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Probe '{action}' failed ({(int)resp.StatusCode}). Request body: {body}. Response: {err}");
        }
        var envelope = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var raw = envelope.TryGetProperty("returnValue", out var rv) ? rv.GetString() : null;
        if (string.IsNullOrEmpty(raw))
            throw new InvalidOperationException($"Probe '{action}' returned no value: {envelope}");
        var state = JsonDocument.Parse(raw).RootElement.Clone();
        if (state.TryGetProperty("error", out var probeErr))
            throw new InvalidOperationException($"Probe '{action}' reported error: {probeErr.GetString()} (raw: {raw})");
        return state;
    }

    public async Task<JsonElement> PollAsync(string action, Func<JsonElement, bool> predicate, int timeoutMs = 8000, params object[] args)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        JsonElement last = default;
        while (DateTime.UtcNow < deadline)
        {
            last = await InvokeAsync(action, args);
            if (predicate(last)) return last;
            await Task.Delay(250);
        }
        return last;
    }

    static string LocateHostProject()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            var candidate = Path.Combine(dir, "tests", "DataGrid.IntegrationTestHost", "DataGrid.IntegrationTestHost.csproj");
            if (File.Exists(candidate)) return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        throw new FileNotFoundException("Could not locate tests/DataGrid.IntegrationTestHost/DataGrid.IntegrationTestHost.csproj by walking up from " + AppContext.BaseDirectory);
    }
}

[CollectionDefinition("DataGrid app")]
public sealed class DataGridAppCollection : ICollectionFixture<DataGridAppFixture> { }
