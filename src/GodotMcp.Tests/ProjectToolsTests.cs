using System;
using System.IO;
using System.Linq;
using GodotMcp.Server;
using GodotMcp.Server.Tools;
using ModelContextProtocol.Server;
using Xunit;

namespace GodotMcp.Tests;

public sealed class ProjectToolsTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ProjectTools _tools;

    public ProjectToolsTests()
    {
        // Create an isolated temporary project directory
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        // Minimal project.godot file – enough for GodotProject to load basic metadata
        File.WriteAllText(
            Path.Combine(_tempDir, "project.godot"),
            "[application]\nconfig/name=\"TestProject\"\n"
        );

        // Dummy assets that the ProjectTools methods will enumerate
        File.WriteAllText(Path.Combine(_tempDir, "scene.tscn"), string.Empty);
        File.WriteAllText(Path.Combine(_tempDir, "resource.tres"), string.Empty);
        File.WriteAllText(Path.Combine(_tempDir, "script.cs"), "public class Test {}");

        // The real ProjectLocator is used – it resolves the temporary directory to a GodotProject
        var locator = new ProjectLocator();
        _tools = new ProjectTools(locator);
    }

    public void Dispose()
    {
        // Clean up the temporary directory after each test run
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // Ignored – test runner will clean up on process exit if needed
        }
    }

    [Fact]
    public void ProjectInfo_ReturnsProjectName()
    {
        var result = _tools.ProjectInfo(_tempDir);
        Assert.NotNull(result);

        // ProjectInfoResult is expected to expose the project name; use reflection to avoid compile‑time coupling.
        var nameProp = result.GetType().GetProperty("Name");
        Assert.NotNull(nameProp);
        var name = nameProp.GetValue(result) as string;
        Assert.NotNull(name);
        Assert.Contains("TestProject", name, StringComparison.Ordinal);
    }

    [Fact]
    public void ListScenes_ReturnsSingleScene()
    {
        var scenes = _tools.ListScenes(_tempDir);
        Assert.NotNull(scenes);
        Assert.Single(scenes);
        Assert.EndsWith(".tscn", scenes[0], StringComparison.Ordinal);
    }

    [Fact]
    public void ListResources_ReturnsSingleResource()
    {
        var resources = _tools.ListResources(_tempDir);
        Assert.NotNull(resources);
        Assert.Single(resources);
        Assert.EndsWith(".tres", resources[0], StringComparison.Ordinal);
    }

    [Fact]
    public void ListScripts_ReturnsSingleScript()
    {
        var scripts = _tools.ListScripts(_tempDir);
        Assert.NotNull(scripts);
        Assert.Single(scripts);
        Assert.EndsWith(".cs", scripts[0], StringComparison.Ordinal);
    }

    [Fact]
    public void InspectScript_ReturnsResultWithPath()
    {
        var result = _tools.InspectScript("res://script.cs", _tempDir);
        Assert.NotNull(result);

        // ScriptResult is expected to expose the script path; use reflection for safety.
        var pathProp = result.GetType().GetProperty("Path");
        Assert.NotNull(pathProp);
        var path = pathProp.GetValue(result) as string;
        Assert.NotNull(path);
        Assert.EndsWith("script.cs", path, StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyProject_ReturnsEmptyCollections()
    {
        // Set up a fresh temporary project with no assets.
        var emptyDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(emptyDir);
        File.WriteAllText(
            Path.Combine(emptyDir, "project.godot"),
            "[application]\nconfig/name=\"EmptyProject\"\n"
        );

        var locator = new ProjectLocator();
        var tools = new ProjectTools(locator);

        var scenes = tools.ListScenes(emptyDir);
        var resources = tools.ListResources(emptyDir);
        var scripts = tools.ListScripts(emptyDir);

        Assert.Empty(scenes);
        Assert.Empty(resources);
        Assert.Empty(scripts);

        // Clean up
        Directory.Delete(emptyDir, recursive: true);
    }
}
