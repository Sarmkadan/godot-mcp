using System.Diagnostics;
using System.IO;
using System.Security;

namespace GodotMcp.Core.Editor;

public sealed record GodotExecutable
{
    public string Path { get; }

    /// <summary>
    /// Initializes a new Godot executable instance with validation.
    /// </summary>
    /// <param name="path">The absolute path to the Godot executable</param>
    /// <exception cref="ArgumentNullException">Thrown if path is null</exception>
    /// <exception cref="ArgumentException">Thrown if path is empty or not absolute</exception>
    /// <exception cref="SecurityException">Thrown if the executable is not allowlisted</exception>
    public GodotExecutable(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        var absolutePath = System.IO.Path.GetFullPath(path);
        if (!System.IO.Path.IsPathRooted(absolutePath))
        {
            throw new ArgumentException(
                $"Executable path must be absolute: '{path}'",
                nameof(path));
        }

        ExecutableAllowlist.Validate(absolutePath);
        Path = absolutePath;
    }

    /// <summary>
    /// Validates that a scene path is project-relative and safe to execute.
    /// </summary>
    /// <param name="scenePath">The scene path to validate</param>
    /// <param name="projectRoot">The project root directory</param>
    /// <exception cref="ArgumentException">Thrown if scene path is invalid</exception>
    public void ValidateScenePath(string? scenePath, string projectRoot)
    {
        if (string.IsNullOrWhiteSpace(scenePath))
        {
            return; // No scene specified, use default
        }

        // Normalize path separators and ensure it's relative
        var normalized = scenePath.Replace('\\', '/');

        // Check for path traversal attempts
        if (normalized.StartsWith("../", StringComparison.Ordinal) ||
            normalized.StartsWith("..\\", StringComparison.Ordinal) ||
            normalized.Contains("/../") ||
            normalized.Contains("\\..\\") ||
            normalized.StartsWith("/") ||
            normalized.StartsWith("\\"))
        {
            throw new ArgumentException(
                $"Scene path must be project-relative and cannot contain '..' or absolute paths: '{scenePath}'",
                nameof(scenePath));
        }

        // Ensure the scene file exists within the project
        var fullScenePath = System.IO.Path.GetFullPath(System.IO.Path.Combine(projectRoot, normalized));
        var projectRootFull = System.IO.Path.GetFullPath(projectRoot);

        if (!fullScenePath.StartsWith(projectRootFull, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Scene path must be within project directory: '{scenePath}' is outside '{projectRoot}'",
                nameof(scenePath));
        }

        if (!File.Exists(fullScenePath))
        {
            throw new FileNotFoundException(
                $"Scene file not found: '{fullScenePath}'",
                fullScenePath);
        }
    }

    public static GodotExecutable? Locate()
    {
        var explicitPath = Environment.GetEnvironmentVariable("GODOT_BIN") ?? Environment.GetEnvironmentVariable("GODOT");
        if (!string.IsNullOrEmpty(explicitPath) && File.Exists(explicitPath))
        {
            try
            {
                return new GodotExecutable(explicitPath);
            }
            catch (SecurityException)
            {
                // If explicitly set but not allowlisted, fail closed
                return null;
            }
        }

        var names = OperatingSystem.IsWindows()
            ? new[] { "godot.exe", "godot4.exe", "Godot_v4.exe" }
            : ["godot", "godot4", "godot-mono"];
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathVar.Split(System.IO.Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var name in names)
            {
                var candidate = System.IO.Path.Combine(dir, name);
                if (File.Exists(candidate))
                {
                    try
                    {
                        return new GodotExecutable(candidate);
                    }
                    catch (SecurityException)
                    {
                        // Skip candidates that aren't allowlisted
                        continue;
                    }
                }
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
        // Validate arguments for security issues
        foreach (var arg in arguments)
        {
            if (arg is null)
            {
                throw new ArgumentException("Arguments collection contains null element", nameof(arguments));
            }

            // Check for command injection patterns
            if (arg.Contains(';') || arg.Contains('&') || arg.Contains('|') || arg.Contains('`'))
            {
                throw new ArgumentException(
                    $"Argument contains forbidden characters that could enable command injection: '{arg}'",
                    nameof(arguments));
            }
        }

        var psi = new ProcessStartInfo(Path)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false, // Critical: prevents shell interpretation
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory
        };

        foreach (var arg in arguments)
        {
            psi.ArgumentList.Add(arg);
        }

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

        // Use a more robust timeout mechanism with proper process tree killing
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

            // Enhanced process termination with retry and fallback
            try
            {
                KillProcessTree(process.Id);
            }
            catch (Exception ex) when (ex is InvalidOperationException)
            {
                // Process may have already terminated
            }

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

    /// <summary>
    /// Recursively kills a process and its entire process tree.
    /// </summary>
    /// <param name="processId">The process ID to kill</param>
    /// <exception cref="InvalidOperationException">Thrown if process killing fails</exception>
    private static void KillProcessTree(int processId)
    {
        try
        {
            // First try the built-in method (available on .NET 8+)
            #if NET8_0_OR_GREATER
            System.Diagnostics.Process.GetProcessById(processId).Kill(entireProcessTree: true);
            #else
            // Fallback for older .NET versions
            KillProcessTreeFallback(processId);
            #endif
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Failed to kill process tree for PID {processId}",
                ex);
        }
    }

    /// <summary>
    /// Fallback process tree killing for .NET versions before 8.0
    /// </summary>
    private static void KillProcessTreeFallback(int parentId)
    {
        try
        {
            // Try to kill the parent process first
            using var parentProcess = System.Diagnostics.Process.GetProcessById(parentId);
            parentProcess.Kill(entireProcessTree: true);
        }
        catch (ArgumentException)
        {
            // Process already exited
        }
        catch (InvalidOperationException)
        {
            // Process doesn't exist or no permission
        }
    }
}

public sealed record GodotRunResult(int ExitCode, string Stdout, string Stderr, TimeSpan Duration, bool TimedOut)
{
    public bool Succeeded => ExitCode == 0 && !TimedOut;
}