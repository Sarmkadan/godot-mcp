using GodotMcp.Core.Project;
using GodotMcp.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace GodotMcp.Tests;

/// <summary>
/// ProjectAnalyzer option validation and the scan limits it drives. Host-level tests start a real host so that ValidateOnStart is exercised.
/// </summary>
public class ProjectAnalyzerOptionsTests : IDisposable
{
    readonly string _root;

    public ProjectAnalyzerOptionsTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "godot-mcp-analyzer-options-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        WriteScene("a.tscn", "Node2D");
        WriteScene("sub/b.tscn", "Node2D");
        WriteScene("sub/deeper/c.tscn", "Sprite2D");
        WriteScene(".godot/d.tscn", "Sprite2D");
        WriteScene(".import/e.tscn", "Sprite2D");
        WriteScene(".git/f.tscn", "Sprite2D");
        WriteScene("custom/g.tscn", "Sprite2D");
        File.WriteAllText(Path.Combine(_root, "x.tres"), "[gd_resource type=\"Resource\" format=3]\n");
        File.WriteAllText(Path.Combine(_root, "project.godot"), "config_version=5\n");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void Defaults_AreValid()
    {
        var result = Validate(new ProjectAnalyzerOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Defaults_IgnoreGitGodotAndImportDirectories()
    {
        var options = new ProjectAnalyzerOptions();

        Assert.Equal([".git", ".godot", ".import"], options.IgnoredDirectories);
        Assert.Empty(options.ExtensionFilters);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Validate_RejectsNegativeMaxScanDepth(int depth)
    {
        var result = Validate(new ProjectAnalyzerOptions { MaxScanDepth = depth });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("MaxScanDepth"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Validate_RejectsNegativeMaxFileCount(int count)
    {
        var result = Validate(new ProjectAnalyzerOptions { MaxFileCount = count });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("MaxFileCount"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void Validate_AcceptsZeroAndPositiveLimits(int limit)
    {
        var result = Validate(new ProjectAnalyzerOptions { MaxScanDepth = limit, MaxFileCount = limit });

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("sub/deeper")]
    [InlineData("sub\\deeper")]
    [InlineData("/.godot")]
    [InlineData(".git/")]
    public void Validate_RejectsIgnoredDirectoryEntriesWithPathSeparators(string entry)
    {
        var result = Validate(new ProjectAnalyzerOptions { IgnoredDirectories = [entry] });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("path separator") && f.Contains("IgnoredDirectories[0]"));
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("*")]
    [InlineData("build?")]
    public void Validate_RejectsIgnoredDirectoryEntriesThatAreNotNames(string entry)
    {
        var result = Validate(new ProjectAnalyzerOptions { IgnoredDirectories = [entry] });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("IgnoredDirectories[0]"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_RejectsBlankIgnoredDirectoryEntries(string entry)
    {
        var result = Validate(new ProjectAnalyzerOptions { IgnoredDirectories = [entry] });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("is blank"));
    }

    [Fact]
    public void Validate_RejectsDuplicateIgnoredDirectoriesCaseInsensitively()
    {
        var result = Validate(new ProjectAnalyzerOptions { IgnoredDirectories = [".git", ".GIT"] });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("IgnoredDirectories[1]") && f.Contains("duplicate"));
    }

    [Theory]
    [InlineData("tscn")]
    [InlineData("*.tscn")]
    [InlineData(".")]
    [InlineData(".ts*")]
    [InlineData("./tscn")]
    [InlineData(".tscn/")]
    [InlineData(".tar.gz")]
    public void Validate_RejectsInvalidExtensionFilters(string extension)
    {
        var result = Validate(new ProjectAnalyzerOptions { ExtensionFilters = [extension] });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("ExtensionFilters[0]") && f.Contains("not a valid extension"));
    }

    [Fact]
    public void Validate_RejectsDuplicateExtensionFiltersCaseInsensitively()
    {
        var result = Validate(new ProjectAnalyzerOptions { ExtensionFilters = [".tscn", ".TSCN"] });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("ExtensionFilters[1]") && f.Contains("duplicate"));
    }

    [Fact]
    public void Validate_ReportsEveryViolationAtOnce()
    {
        var result = Validate(new ProjectAnalyzerOptions
        {
            MaxScanDepth = -1,
            MaxFileCount = -5,
            IgnoredDirectories = ["a/b", ""],
            ExtensionFilters = ["tscn", ".tscn", ".TSCN"]
        });

        Assert.True(result.Failed);
        Assert.Equal(6, result.Failures!.Count());
    }

    [Fact]
    public async Task Host_WithValidConfiguration_BindsValues()
    {
        var options = await StartHostAsync(new Dictionary<string, string?>
        {
            ["ProjectAnalyzer:MaxScanDepth"] = "4",
            ["ProjectAnalyzer:MaxFileCount"] = "250",
            ["ProjectAnalyzer:ExtensionFilters:0"] = ".tscn"
        });

        Assert.Equal(4, options.MaxScanDepth);
        Assert.Equal(250, options.MaxFileCount);
        Assert.Equal([".tscn"], options.ExtensionFilters);
    }

    [Fact]
    public async Task Host_WithConfiguredIgnoredDirectories_ReplacesDefaults()
    {
        var options = await StartHostAsync(new Dictionary<string, string?>
        {
            ["ProjectAnalyzer:IgnoredDirectories:0"] = "custom"
        });

        Assert.Equal(["custom"], options.IgnoredDirectories);
    }

    [Fact]
    public async Task Host_WithShippedAppSettings_StartsWithDocumentedDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(ShippedAppSettingsPath(), optional: false)
            .Build();

        var options = await StartHostAsync(configuration);

        Assert.Equal(new ProjectAnalyzerOptions().MaxScanDepth, options.MaxScanDepth);
        Assert.Equal(new ProjectAnalyzerOptions().MaxFileCount, options.MaxFileCount);
        Assert.Equal([".git", ".godot", ".import"], options.IgnoredDirectories);
        Assert.Empty(options.ExtensionFilters);
    }

    [Fact]
    public async Task Host_WithInvalidConfiguration_FailsOnStart()
    {
        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => StartHostAsync(new Dictionary<string, string?>
        {
            ["ProjectAnalyzer:MaxFileCount"] = "-1",
            ["ProjectAnalyzer:IgnoredDirectories:0"] = "sub/dir",
            ["ProjectAnalyzer:ExtensionFilters:0"] = "*.tscn"
        }));

        Assert.Equal(3, exception.Failures.Count());
    }

    [Fact]
    public void EnumerateFiles_Defaults_SkipsIgnoredDirectories()
    {
        var files = Enumerate(new ProjectAnalyzerOptions(), ".tscn");

        Assert.Equal(["res://a.tscn", "res://custom/g.tscn", "res://sub/b.tscn", "res://sub/deeper/c.tscn"], files);
    }

    [Fact]
    public void EnumerateFiles_MaxScanDepthOne_IncludesRootAndImmediateSubdirectoriesOnly()
    {
        var files = Enumerate(new ProjectAnalyzerOptions { MaxScanDepth = 1 }, ".tscn");

        Assert.Equal(["res://a.tscn", "res://custom/g.tscn", "res://sub/b.tscn"], files);
    }

    [Fact]
    public void EnumerateFiles_MaxScanDepthZero_IsUnlimited()
    {
        var files = Enumerate(new ProjectAnalyzerOptions { MaxScanDepth = 0 }, ".tscn");

        Assert.Contains("res://sub/deeper/c.tscn", files);
    }

    [Fact]
    public void EnumerateFiles_IgnoredDirectoriesAreMatchedByNameAtAnyDepth()
    {
        WriteScene("sub/.git/h.tscn", "Sprite2D");

        var files = Enumerate(new ProjectAnalyzerOptions(), ".tscn");

        Assert.DoesNotContain("res://sub/.git/h.tscn", files);
    }

    [Fact]
    public void EnumerateFiles_ExtensionFilters_RestrictEveryQuery()
    {
        var restricted = new ProjectAnalyzerOptions { ExtensionFilters = [".tres"] };

        Assert.Empty(Enumerate(restricted, ".tscn"));
        Assert.Equal(["res://x.tres"], Enumerate(restricted, ".tscn", ".tres"));
    }

    [Fact]
    public void EnumerateFiles_MaxFileCountExceeded_FailsInsteadOfTruncating()
    {
        var project = GodotProject.Open(_root);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            project.EnumerateFiles(new ProjectAnalyzerOptions { MaxFileCount = 2 }, ".tscn").ToList());

        Assert.Contains("MaxFileCount", exception.Message);
    }

    [Fact]
    public void EnumerateFiles_MaxFileCountZero_IsUnlimited()
    {
        var files = Enumerate(new ProjectAnalyzerOptions { MaxFileCount = 0 }, ".tscn");

        Assert.Equal(4, files.Count);
    }

    [Fact]
    public void ProjectAnalyzer_AppliesScanOptions()
    {
        var analyzer = new ProjectAnalyzer(GodotProject.Open(_root), new ProjectAnalyzerOptions { MaxScanDepth = 1 });

        var matches = analyzer.FindNodes(type: "Sprite2D");

        Assert.Equal(["res://custom/g.tscn"], matches.Select(m => m.ScenePath));
    }

    [Fact]
    public void ProjectAnalyzer_DefaultOptions_ScanDeepScenes()
    {
        var analyzer = new ProjectAnalyzer(GodotProject.Open(_root));

        var matches = analyzer.FindNodes(type: "Sprite2D");

        Assert.Equal(["res://custom/g.tscn", "res://sub/deeper/c.tscn"], matches.Select(m => m.ScenePath).Order());
    }

    void WriteScene(string relativePath, string rootType)
    {
        var path = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, $"""
            [gd_scene format=3]

            [node name="Root" type="{rootType}"]
            """);
    }

    List<string> Enumerate(ProjectAnalyzerOptions options, params string[] extensions) =>
        GodotProject.Open(_root).EnumerateFiles(options, extensions).Order().ToList();

    static ValidateOptionsResult Validate(ProjectAnalyzerOptions options) =>
        new ProjectAnalyzerOptionsValidator().Validate(null, options);

    static Task<ProjectAnalyzerOptions> StartHostAsync(IDictionary<string, string?> settings) =>
        StartHostAsync(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());

    static string ShippedAppSettingsPath()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "GodotMcp.Server", "appsettings.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }
        throw new FileNotFoundException("Could not locate src/GodotMcp.Server/appsettings.json from the test output directory.");
    }

    static async Task<ProjectAnalyzerOptions> StartHostAsync(IConfiguration configuration)
    {
        using var host = new HostBuilder()
            .ConfigureServices(services => services.AddProjectAnalyzerOptions(configuration))
            .Build();

        await host.StartAsync();
        return host.Services.GetRequiredService<IOptions<ProjectAnalyzerOptions>>().Value;
    }
}
