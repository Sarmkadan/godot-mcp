using System.Diagnostics;

namespace GodotMcp.Core.Editor;

public sealed record GodotExecutable(string Path)
{
    public static GodotExecutable? Locate()
    {
        var explicitPath = Environment.GetEnvironmentVariable("GODOT_BIN") ?? Environment.GetEnvironmentVariable("GODOT");
        if (!string.IsNullOrEmpty(explicitPath) && File.Exists(explicitPath)) return new GodotExecutable(explicitPath);
        var names = OperatingSystem.IsWindows()
            ? new[] { "godot.exe", "godot4.exe", "Godot_v4.exe" }
            : ["godot", "godot4", "godot-mono"];
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathVar.Split(System.IO.Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var name in names)
            {
                var candidate = System.IO.Path.Combine(dir, name);
                if (File.Exists(candidate)) return new GodotExecutable(candidate);
            }
        }
        return null;
    }

    public async Task<string?> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(["--version"], workingDirectory: null, timeout: TimeSpan.FromSeconds(30), null, cancellationToken);
        return result.ExitCode == 0 ? result.Stdout.Trim() : null;
    }

    public async Task<GodotRunResult> RunAsync(
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        TimeSpan timeout,
        Action<string>? onOutputLine = null,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo(Path)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory
        };
        foreach (var arg in arguments) psi.ArgumentList.Add(arg);
        using var process = new Process { StartInfo = psi };
        var stdout = new List<string>();
        var stderr = new List<string>();
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            stdout.Add(e.Data);
            onOutputLine?.Invoke(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            stderr.Add(e.Data);
            onOutputLine?.Invoke(e.Data);
        };
        var stopwatch = Stopwatch.StartNew();
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        var timedOut = false;
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            timedOut = !cancellationToken.IsCancellationRequested;
            try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            cancellationToken.ThrowIfCancellationRequested();
        }
        stopwatch.Stop();
        return new GodotRunResult(
            timedOut ? -1 : process.ExitCode,
            string.Join('\n', stdout),
            string.Join('\n', stderr),
            stopwatch.Elapsed,
            timedOut);
    }
}

public sealed record GodotRunResult(int ExitCode, string Stdout, string Stderr, TimeSpan Duration, bool TimedOut)
{
    public bool Succeeded => ExitCode == 0 && !TimedOut;
}
