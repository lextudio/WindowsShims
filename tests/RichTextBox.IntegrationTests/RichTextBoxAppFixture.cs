using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RichTextBox.IntegrationTests;

public sealed class RichTextBoxAppFixture : IAsyncLifetime
{
    const int Port = 9224;
    static readonly string BaseUrl = $"http://localhost:{Port}";

    readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    Process? _app;

    public string HostProjectPath { get; } = HostProjectPathStatic;

    public async ValueTask InitializeAsync()
    {
        StopApp();
        await WaitForPortFreeAsync(TimeSpan.FromSeconds(30));
        await StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        StopApp();
        _http.Dispose();
        await Task.CompletedTask;
    }

    /// <summary>
    /// True when the host under test is the native WinUI 3 (WinAppSDK) head rather
    /// than the Uno Skia desktop head. A few probes are Skia-only; see IsWinAppSdkHost
    /// usages in the tests.
    /// </summary>
    public static bool IsWinAppSdkHost { get; } =
        (Environment.GetEnvironmentVariable("DOTNET_HOST_TFM") ?? "net10.0-desktop")
            .Contains("-windows", StringComparison.OrdinalIgnoreCase);

    async Task StartAsync()
    {
        var tfm = Environment.GetEnvironmentVariable("DOTNET_HOST_TFM") ?? "net10.0-desktop";
        var psi = new ProcessStartInfo
        {
            WorkingDirectory = Path.GetDirectoryName(HostProjectPath)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        if (IsWinAppSdkHost)
        {
            // `dotnet run` cannot build the WinUI 3 head: it transitively builds
            // LeXtudio.Windows, a WinUI class library containing XAML, which the Uno
            // SDK rejects under `dotnet build` (UNOB0008 — "use msbuild instead").
            // So the WinUI head is built with msbuild beforehand and launched directly.
            psi.FileName = LocateHostExecutable(tfm);
        }
        else
        {
            psi.FileName = "dotnet";
            foreach (var a in new[] { "run", "--project", HostProjectPath, "-f", tfm, "--configuration", "Debug" })
                psi.ArgumentList.Add(a);
        }

        _app = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start RichTextBox.IntegrationTestHost");

        // Keep the host's own output: when it dies mid-suite every later test just reports
        // "connection refused", and the reason (a runtime abort message, a stack overflow
        // notice) is only ever printed here.
        var hostLog = Path.Combine(Path.GetTempPath(), "rtb-host-output.log");
        void Append(string? line)
        {
            if (line is null) return;
            try { File.AppendAllText(hostLog, line + Environment.NewLine); } catch { }
        }
        _app.OutputDataReceived += (_, e) => Append(e.Data);
        _app.ErrorDataReceived += (_, e) => Append(e.Data);
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
                await InvokeAsync("richtextbox.probe.state");
                return;
            }
            catch { }
            await Task.Delay(500);
        }
        throw new TimeoutException("RichTextBox host did not respond within " + timeout);
    }

    void StopApp()
    {
        try { if (_app is { HasExited: false }) _app.Kill(entireProcessTree: true); } catch { }
        try { foreach (var p in Process.GetProcessesByName("RichTextBox.IntegrationTestHost")) { try { p.Kill(true); } catch { } } } catch { }
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
            bool gone = Process.GetProcessesByName("RichTextBox.IntegrationTestHost").Length == 0;
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

    // Finds the msbuild-produced host executable for a given TFM. The WinUI head is
    // built per-platform (bin/x64/Debug/<tfm>/), so pick the most recently written
    // match rather than assuming an architecture.
    static string LocateHostExecutable(string tfm)
    {
        var hostDir = Path.GetDirectoryName(HostProjectPathStatic)!;
        var binDir = Path.Combine(hostDir, "bin");
        var overridePath = Environment.GetEnvironmentVariable("RICHTEXTBOX_HOST_EXE");
        if (!string.IsNullOrEmpty(overridePath) && File.Exists(overridePath))
            return overridePath;

        if (Directory.Exists(binDir))
        {
            var candidate = Directory
                .EnumerateFiles(binDir, "RichTextBox.IntegrationTestHost.exe", SearchOption.AllDirectories)
                .Where(p => p.Replace('\\', '/').Contains($"/{tfm}/", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
            if (candidate is not null)
                return candidate;
        }

        throw new FileNotFoundException(
            $"No built host for '{tfm}' under {binDir}. The WinUI 3 head must be built with msbuild first, e.g.: " +
            $"msbuild {HostProjectPathStatic} -p:TargetFramework={tfm} -p:Platform=x64 -p:Configuration=Debug");
    }

    static readonly string HostProjectPathStatic = LocateHostProject();

    static string LocateHostProject()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            var candidate = Path.Combine(dir, "tests", "RichTextBox.IntegrationTestHost", "RichTextBox.IntegrationTestHost.csproj");
            if (File.Exists(candidate)) return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        throw new FileNotFoundException("Could not locate tests/RichTextBox.IntegrationTestHost/RichTextBox.IntegrationTestHost.csproj by walking up from " + AppContext.BaseDirectory);
    }
}

[CollectionDefinition("RichTextBox app")]
public sealed class RichTextBoxAppCollection : ICollectionFixture<RichTextBoxAppFixture> { }
