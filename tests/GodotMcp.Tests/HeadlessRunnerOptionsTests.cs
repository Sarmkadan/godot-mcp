using GodotMcp.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace GodotMcp.Tests;

/// <summary>
/// Startup validation of HeadlessRunner options. Each test starts a real host so that ValidateOnStart is exercised.
/// </summary>
public class HeadlessRunnerOptionsTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _projectRoot;

    public HeadlessRunnerOptionsTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "godot-mcp-options-tests-" + Guid.NewGuid().ToString("N"));
        _projectRoot = Path.Combine(_tempRoot, "Project");
        Directory.CreateDirectory(_projectRoot);
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

    [Fact]
    public async Task Start_WithDefaults_UsesDefaultProjectRootAndTimeout()
    {
        var options = await StartHostAsync(new Dictionary<string, string?>(), _projectRoot);

        Assert.Null(options.GodotPath);
        Assert.Equal(120, options.TimeoutSeconds);
        Assert.Equal(10 * 1024 * 1024, options.MaxOutputBytes);
        Assert.Equal([_projectRoot], options.AllowedProjectRoots);
    }

    [Fact]
    public async Task Start_WithConfiguredProjectRoots_DoesNotAddDefaultRoot()
    {
        var otherRoot = Path.Combine(_tempRoot, "Other");
        var options = await StartHostAsync(new Dictionary<string, string?>
        {
            ["HeadlessRunner:AllowedProjectRoots:0"] = otherRoot
        }, _projectRoot);

        Assert.Equal([otherRoot], options.AllowedProjectRoots);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    public async Task Start_WithNonPositiveTimeout_Fails(string timeout)
    {
        var ex = await Assert.ThrowsAsync<OptionsValidationException>(() => StartHostAsync(new Dictionary<string, string?>
        {
            ["HeadlessRunner:TimeoutSeconds"] = timeout
        }, _projectRoot));

        Assert.Contains(ex.Failures, f => f.Contains("TimeoutSeconds", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task Start_WithNonPositiveMaxOutputBytes_Fails(string maxOutput)
    {
        var ex = await Assert.ThrowsAsync<OptionsValidationException>(() => StartHostAsync(new Dictionary<string, string?>
        {
            ["HeadlessRunner:MaxOutputBytes"] = maxOutput
        }, _projectRoot));

        Assert.Contains(ex.Failures, f => f.Contains("MaxOutputBytes", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Start_WithBlankProjectRoot_Fails()
    {
        var ex = await Assert.ThrowsAsync<OptionsValidationException>(() => StartHostAsync(new Dictionary<string, string?>
        {
            ["HeadlessRunner:AllowedProjectRoots:0"] = "   "
        }, _projectRoot));

        Assert.Contains(ex.Failures, f => f.Contains("AllowedProjectRoots[0]", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WithEmptyAllowedProjectRoots_Fails()
    {
        // The default root fills an empty list at startup, so exercise the DataAnnotations rule directly.
        var options = new HeadlessRunnerOptions { AllowedProjectRoots = [] };

        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var valid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            options,
            new System.ComponentModel.DataAnnotations.ValidationContext(options),
            results,
            validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("at least one project root", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Start_WithMissingGodotBinary_Fails()
    {
        var missing = Path.Combine(_tempRoot, "no-such-godot");

        var ex = await Assert.ThrowsAsync<OptionsValidationException>(() => StartHostAsync(new Dictionary<string, string?>
        {
            ["HeadlessRunner:GodotPath"] = missing
        }, _projectRoot));

        Assert.Contains(ex.Failures, f => f.Contains("does not exist", StringComparison.Ordinal) && f.Contains(missing, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Start_WithGodotPathPointingAtDirectory_Fails()
    {
        var ex = await Assert.ThrowsAsync<OptionsValidationException>(() => StartHostAsync(new Dictionary<string, string?>
        {
            ["HeadlessRunner:GodotPath"] = _projectRoot
        }, _projectRoot));

        Assert.Contains(ex.Failures, f => f.Contains("GodotPath", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Start_WithRelativeGodotPath_Fails()
    {
        var ex = await Assert.ThrowsAsync<OptionsValidationException>(() => StartHostAsync(new Dictionary<string, string?>
        {
            ["HeadlessRunner:GodotPath"] = "godot"
        }, _projectRoot));

        Assert.Contains(ex.Failures, f => f.Contains("must be an absolute path", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Start_WithBlankGodotPath_Fails()
    {
        var ex = await Assert.ThrowsAsync<OptionsValidationException>(() => StartHostAsync(new Dictionary<string, string?>
        {
            ["HeadlessRunner:GodotPath"] = " "
        }, _projectRoot));

        Assert.Contains(ex.Failures, f => f.Contains("set but blank", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Start_WithNonExecutableGodotBinary_Fails()
    {
        if (OperatingSystem.IsWindows())
        {
            return; // Windows has no execute bit; existence is the only check there.
        }

        var godot = CreateFile("godot", UnixFileMode.UserRead | UnixFileMode.UserWrite);

        var ex = await Assert.ThrowsAsync<OptionsValidationException>(() => StartHostAsync(new Dictionary<string, string?>
        {
            ["HeadlessRunner:GodotPath"] = godot
        }, _projectRoot));

        Assert.Contains(ex.Failures, f => f.Contains("not executable", StringComparison.Ordinal) && f.Contains(godot, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Start_WithExecutableGodotBinary_Succeeds()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var godot = CreateFile("godot", UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

        var options = await StartHostAsync(new Dictionary<string, string?>
        {
            ["HeadlessRunner:GodotPath"] = godot
        }, _projectRoot);

        Assert.Equal(godot, options.GodotPath);
    }

    [Fact]
    public async Task Start_WithSeveralViolations_ReportsAllOfThem()
    {
        var missing = Path.Combine(_tempRoot, "no-such-godot");

        var ex = await Assert.ThrowsAsync<OptionsValidationException>(() => StartHostAsync(new Dictionary<string, string?>
        {
            ["HeadlessRunner:TimeoutSeconds"] = "0",
            ["HeadlessRunner:MaxOutputBytes"] = "0",
            ["HeadlessRunner:GodotPath"] = missing,
            ["HeadlessRunner:AllowedProjectRoots:0"] = ""
        }, _projectRoot));

        Assert.Equal(4, ex.Failures.Count());
        Assert.Contains(ex.Failures, f => f.Contains("TimeoutSeconds", StringComparison.Ordinal));
        Assert.Contains(ex.Failures, f => f.Contains("MaxOutputBytes", StringComparison.Ordinal));
        Assert.Contains(ex.Failures, f => f.Contains("GodotPath", StringComparison.Ordinal));
        Assert.Contains(ex.Failures, f => f.Contains("AllowedProjectRoots[0]", StringComparison.Ordinal));
    }

    [System.Runtime.Versioning.UnsupportedOSPlatform("windows")]
    private string CreateFile(string name, UnixFileMode mode)
    {
        var path = Path.Combine(_tempRoot, name);
        File.WriteAllText(path, "#!/bin/sh\n");
        File.SetUnixFileMode(path, mode);
        return path;
    }

    private static async Task<HeadlessRunnerOptions> StartHostAsync(IDictionary<string, string?> settings, string defaultProjectRoot)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        using var host = new HostBuilder()
            .ConfigureServices(services => services.AddHeadlessRunnerOptions(configuration, defaultProjectRoot))
            .Build();

        await host.StartAsync();
        return host.Services.GetRequiredService<IOptions<HeadlessRunnerOptions>>().Value;
    }
}
