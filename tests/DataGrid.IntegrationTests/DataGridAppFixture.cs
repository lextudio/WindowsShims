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

    /// <summary>
    /// Window CONTENT origin in Quartz global points (top-left), as measured natively by the
    /// agent (capabilities.windowContentOrigin). Adding a page-local point to this gives the
    /// global screen coordinate a cliclick drag needs.
    /// </summary>
    public async Task<(double X, double Y, double Scale)> GetWindowContentOriginAsync()
    {
        using var resp = await _http.GetAsync($"{BaseUrl}/api/v1/agent/status");
        resp.EnsureSuccessStatusCode();
        var root = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var origin = root.TryGetProperty("capabilities", out var caps) && caps.TryGetProperty("windowContentOrigin", out var o)
            ? o
            : (root.TryGetProperty("windowContentOrigin", out var o2) ? o2 : default);
        if (origin.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException($"windowContentOrigin not found in agent status: {root}");
        return (origin.GetProperty("x").GetDouble(), origin.GetProperty("y").GetDouble(),
            origin.TryGetProperty("scale", out var s) ? s.GetDouble() : 1.0);
    }

    /// <summary>
    /// Bring the app window to the foreground by clicking its title bar. A background window's
    /// first click is consumed by macOS to activate it (not delivered to content), so injected
    /// drags need the window already focused. Implemented as a zero-length drag (click) via the
    /// same cliclick path the real drag uses.
    /// </summary>
    public async Task FocusWindowAsync(double titleX, double titleY)
    {
        await DragAsync(titleX, titleY, titleX, titleY, global: true);
        await Task.Delay(300);
    }

    public async Task<JsonElement> DragAsync(double fromX, double fromY, double toX, double toY, bool global = false)
    {
        var body = JsonSerializer.Serialize(new { fromX, fromY, toX, toY, global });
        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        using var resp = await _http.PostAsync($"{BaseUrl}/api/v1/ui/actions/drag", content);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Drag failed ({(int)resp.StatusCode}). Response: {err}");
        }
        return await resp.Content.ReadFromJsonAsync<JsonElement>();
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
