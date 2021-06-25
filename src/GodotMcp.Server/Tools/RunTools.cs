using System.ComponentModel;
using GodotMcp.Core.Editor;
using ModelContextProtocol.Server;

namespace GodotMcp.Server.Tools;

[McpServerToolType]
public sealed class RunTools(ProjectLocator locator)
{
    [McpServerTool(Name = "godot_binary_info"), Description("Locate the installed godot binary and report its path and version, if any.")]
    public async Task<Dictionary<string, string?>> BinaryInfo(CancellationToken cancellationToken)
    {
        var executable = GodotExecutable.Locate();
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
        var runner = new HeadlessRunner(locator.Resolve(projectPath), executable)
        {
            DefaultTimeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 1, 600))
        };
        var result = await runner.RunProjectAsync(scene, null, null, cancellationToken);
        return new RunResult(result.ExitCode, result.Succeeded, result.TimedOut, result.Duration.TotalSeconds, result.Stdout, result.Stderr);
    }

    [McpServerTool(Name = "godot_import_resources"), Description("Run 'godot --headless --import' to (re)import project resources.")]
    public async Task<RunResult> ImportResources(string? projectPath = null, CancellationToken cancellationToken = default)
    {
        var executable = GodotExecutable.Locate()
            ?? throw new InvalidOperationException("godot binary not found; set GODOT_BIN or add godot to PATH");
        var runner = new HeadlessRunner(locator.Resolve(projectPath), executable);
        var result = await runner.ImportResourcesAsync(cancellationToken);
        return new RunResult(result.ExitCode, result.Succeeded, result.TimedOut, result.Duration.TotalSeconds, result.Stdout, result.Stderr);
    }
}
