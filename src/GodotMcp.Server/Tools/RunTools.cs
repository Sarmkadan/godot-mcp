using System.ComponentModel;
using GodotMcp.Core.Editor;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace GodotMcp.Server.Tools;

[McpServerToolType]
public sealed class RunTools(ProjectLocator locator, ILogger<RunTools> logger)
{
    [McpServerTool(Name = "godot_binary_info"), Description("Locate the installed godot binary and report its path and version, if any.")]
    public async Task<Dictionary<string, string?>> BinaryInfo(CancellationToken cancellationToken)
    {
        var executable = GodotExecutable.Locate();
        if (executable is null)
        {
            logger.LogWarning("Godot binary not found");
        }
        else
        {
            logger.LogInformation("Godot binary located: {Path}", executable.Path);
        }
        return new Dictionary<string, string?>
        {
            ["path"] = executable?.Path,
            ["version"] = executable is null ? null : await executable.GetVersionAsync(cancellationToken)
        };
    }

    [McpServerTool(Name = "godot_run_headless"), Description("Run the project headlessly with the godot binary and return exit code plus captured output.")]
    public async Task<RunResult> RunHeadless([Description("Optional scene to run instead of the main scene")] string? scene = null, [Description("Timeout in seconds")] int timeoutSeconds = 60, string? projectPath = null, CancellationToken cancellationToken = default)
    {
        var executable = GodotExecutable.Locate()
            ?? throw new InvalidOperationException("godot binary not found; set GODOT_BIN or add godot to PATH");
        var resolvedPath = locator.Resolve(projectPath);
        var timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 1, 600));
        logger.LogInformation("Starting headless run: Scene={Scene}, ProjectPath={ProjectPath}, Timeout={TimeoutSeconds}s", scene ?? "main scene", resolvedPath, timeout.TotalSeconds);
        var runner = new HeadlessRunner(resolvedPath, executable)
        {
            DefaultTimeout = timeout
        };
        var result = await runner.RunProjectAsync(scene, null, null, cancellationToken);
        if (result.TimedOut || result.ExitCode != 0)
        {
            logger.LogWarning("Headless run finished: ExitCode={ExitCode}, TimedOut={TimedOut}, Duration={DurationSeconds}s", result.ExitCode, result.TimedOut, result.Duration.TotalSeconds);
        }
        else
        {
            logger.LogInformation("Headless run finished: ExitCode={ExitCode}, TimedOut={TimedOut}, Duration={DurationSeconds}s", result.ExitCode, result.TimedOut, result.Duration.TotalSeconds);
        }
        return new RunResult(result.ExitCode, result.Succeeded, result.TimedOut, result.Duration.TotalSeconds, result.Stdout, result.Stderr);
    }

    [McpServerTool(Name = "godot_import_resources"), Description("Run 'godot --headless --import' to (re)import project resources.")]
    public async Task<RunResult> ImportResources(string? projectPath = null, CancellationToken cancellationToken = default)
    {
        var executable = GodotExecutable.Locate()
            ?? throw new InvalidOperationException("godot binary not found; set GODOT_BIN or add godot to PATH");
        var resolvedPath = locator.Resolve(projectPath);
        logger.LogInformation("Starting resource import: ProjectPath={ProjectPath}", resolvedPath);
        var runner = new HeadlessRunner(resolvedPath, executable);
        var result = await runner.ImportResourcesAsync(cancellationToken);
        if (result.TimedOut || result.ExitCode != 0)
        {
            logger.LogWarning("Resource import finished: ExitCode={ExitCode}, TimedOut={TimedOut}, Duration={DurationSeconds}s", result.ExitCode, result.TimedOut, result.Duration.TotalSeconds);
        }
        else
        {
            logger.LogInformation("Resource import finished: ExitCode={ExitCode}, TimedOut={TimedOut}, Duration={DurationSeconds}s", result.ExitCode, result.TimedOut, result.Duration.TotalSeconds);
        }
        return new RunResult(result.ExitCode, result.Succeeded, result.TimedOut, result.Duration.TotalSeconds, result.Stdout, result.Stderr);
    }
}
