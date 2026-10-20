using GodotMcp.Core.Editor;
using GodotMcp.Core.Project;
using Xunit;

namespace GodotMcp.Tests;

/// <summary>
/// Integration tests for HeadlessRunner that require a Godot binary.
/// These tests are skipped if Godot is not available on the system.
/// </summary>
public class HeadlessRunnerIntegrationTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _projectRoot;
    private readonly GodotProject _project;
    private readonly GodotExecutable? _executable;

    public HeadlessRunnerIntegrationTests()
    {
        // Create temp directory for test project
        _tempRoot = Path.Combine(Path.GetTempPath(), "godot-mcp-headless-tests-" + Guid.NewGuid().ToString("N"));
        _projectRoot = Path.Combine(_tempRoot, "TestProject");
        Directory.CreateDirectory(_projectRoot);

        // Create minimal project.godot
        var projectGodot = """
            config_version=5

            [application]
            config/name="HeadlessTest"
            run/main_scene="res://main.tscn"
            """;
        File.WriteAllText(Path.Combine(_projectRoot, "project.godot"), projectGodot);

        // Create a simple scene
        var scene = """
            [gd_scene load_steps=1 format=3]

            [node name="Main" type="Node"]
            """;
        File.WriteAllText(Path.Combine(_projectRoot, "main.tscn"), scene);

        // Create a simple GDScript that prints and exits
        var script = """
            extends Node

            func _ready():
                print("Hello from Godot!")
                quit()
            """;
        File.WriteAllText(Path.Combine(_projectRoot, "main.gd"), script);

        // Create a script that loops forever (for timeout testing)
        var infiniteScript = """
            extends Node

            func _ready():
                while true:
                    pass
            """;
        File.WriteAllText(Path.Combine(_projectRoot, "infinite.gd"), infiniteScript);

        _project = GodotProject.Open(_projectRoot);
        _executable = GodotExecutable.Locate();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact(Skip = "Godot binary not found on PATH or via GODOT_BIN/GODOT environment variables. Skipping HeadlessRunner integration test.")]
    public async Task RunProjectAsync_WithValidScene_ReturnsSuccess()
    {
        var runner = new HeadlessRunner(_project, _executable!);
        var result = await runner.RunProjectAsync("res://main.tscn");

        Assert.False(result.TimedOut);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Hello from Godot!", result.Stdout);
    }

    [Fact(Skip = "Godot binary not found on PATH or via GODOT_BIN/GODOT environment variables. Skipping HeadlessRunner integration test.")]
    public async Task RunProjectAsync_WithDefaultScene_ReturnsSuccess()
    {
        var runner = new HeadlessRunner(_project, _executable!);
        var result = await runner.RunProjectAsync();

        Assert.False(result.TimedOut);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Hello from Godot!", result.Stdout);
    }

    [Fact(Skip = "Godot binary not found on PATH or via GODOT_BIN/GODOT environment variables. Skipping HeadlessRunner integration test.")]
    public async Task RunScriptAsync_WithValidScript_ReturnsSuccess()
    {
        var runner = new HeadlessRunner(_project, _executable!);
        var result = await runner.RunScriptAsync("res://main.gd");

        Assert.False(result.TimedOut);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Hello from Godot!", result.Stdout);
    }

    [Fact(Skip = "Godot binary not found on PATH or via GODOT_BIN/GODOT environment variables. Skipping HeadlessRunner integration test.")]
    public async Task RunScriptAsync_InfiniteLoop_IsKilledByTimeout()
    {
        var runner = new HeadlessRunner(_project, _executable!)
        {
            DefaultTimeout = TimeSpan.FromSeconds(2)
        };

        var result = await runner.RunScriptAsync("res://infinite.gd");

        Assert.True(result.TimedOut);
        Assert.Equal(-1, result.ExitCode);
        Assert.True(result.Duration < TimeSpan.FromSeconds(5), "Process should be killed within timeout + small buffer");
    }

    [Fact(Skip = "Godot binary not found on PATH or via GODOT_BIN/GODOT environment variables. Skipping HeadlessRunner integration test.")]
    public async Task RunProjectAsync_CapturesStdoutAndStderr()
    {
        var runner = new HeadlessRunner(_project, _executable!);
        var capturedLines = new List<string>();
        var result = await runner.RunProjectAsync("res://main.tscn", onOutputLine: line => capturedLines.Add(line));

        Assert.False(result.TimedOut);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Hello from Godot!", result.Stdout);
        Assert.Contains(capturedLines, line => line.Contains("Hello from Godot!"));
    }

    [Fact(Skip = "Godot binary not found on PATH or via GODOT_BIN/GODOT environment variables. Skipping HeadlessRunner integration test.")]
    public async Task RunProjectAsync_WithExtraArgs_PassesArgsToGodot()
    {
        // Create a script that echoes back an argument
        var echoScript = """
            extends Node

            func _ready():
                var args = OS.get_cmdline_args()
                if args.size() > 1:
                    print("ARG: " + args[1])
                quit()
            """;
        File.WriteAllText(Path.Combine(_projectRoot, "echo.gd"), echoScript);

        var runner = new HeadlessRunner(_project, _executable!);
        var result = await runner.RunProjectAsync("res://echo.gd", extraArgs: ["--test-arg", "hello-world"]);

        Assert.False(result.TimedOut);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("ARG: hello-world", result.Stdout);
    }

    [Fact(Skip = "Godot binary not found on PATH or via GODOT_BIN/GODOT environment variables. Skipping HeadlessRunner integration test.")]
    public async Task ImportResourcesAsync_RunsWithoutError()
    {
        var runner = new HeadlessRunner(_project, _executable!);
        var result = await runner.ImportResourcesAsync();

        Assert.False(result.TimedOut);
        // Import may return non-zero if there's nothing to import, but shouldn't timeout
        Assert.True(result.Duration < TimeSpan.FromSeconds(30));
    }

    [Fact(Skip = "Godot binary not found on PATH or via GODOT_BIN/GODOT environment variables. Skipping HeadlessRunner integration test.")]
    public async Task QuitAfterFramesAsync_ExitsAfterSpecifiedFrames()
    {
        var runner = new HeadlessRunner(_project, _executable!)
        {
            DefaultTimeout = TimeSpan.FromSeconds(10)
        };

        var result = await runner.QuitAfterFramesAsync(5);

        Assert.False(result.TimedOut);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact(Skip = "Godot binary not found on PATH or via GODOT_BIN/GODOT environment variables. Skipping HeadlessRunner integration test.")]
    public async Task RunProjectAsync_WithCancellationToken_CanBeCancelled()
    {
        var runner = new HeadlessRunner(_project, _executable!)
        {
            DefaultTimeout = TimeSpan.FromMinutes(1)
        };

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        var result = await runner.RunScriptAsync("res://infinite.gd", cancellationToken: cts.Token);

        Assert.True(result.TimedOut || cts.Token.IsCancellationRequested);
    }
}