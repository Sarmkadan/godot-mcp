using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GodotMcp.Core.Scenes;
using GodotMcp.Server;
using GodotMcp.Server.Tools;
using Xunit;

namespace GodotMcp.Tests
{
    /// <summary>
    /// End‑to‑end tests for the public MCP tool surface defined in <c>AnalysisTools.cs</c>.
    /// The tests instantiate the tool class directly (in‑process) and verify that the
    /// returned DTOs can be serialized to JSON with the expected shape.
    /// </summary>
    public sealed class AnalysisToolsTests : IDisposable
    {
        private readonly string _sampleProjectPath;
        private readonly AnalysisTools _tools;

        public AnalysisToolsTests()
        {
            // The repository contains a minimal sample project under examples/sample-project.
            // Resolve it to an absolute path so the tests work regardless of the current
            // working directory.
            var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
            _sampleProjectPath = Path.Combine(repoRoot, "examples", "sample-project");

            if (!Directory.Exists(_sampleProjectPath))
                throw new InvalidOperationException($"Sample project not found at '{_sampleProjectPath}'");

            var locator = new ProjectLocator();
            _tools = new AnalysisTools(locator);
        }

        public void Dispose()
        {
            // No unmanaged resources to clean up – the class exists only to satisfy the
            // IDisposable pattern used by other test classes in the suite.
        }

        private static JsonDocument ParseJson(object obj)
        {
            var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
            return JsonDocument.Parse(json);
        }

        [Fact]
        public void FindNodes_Returns_Valid_Json()
        {
            // Find all nodes of type "Node2D" in the sample project.
            var result = _tools.FindNodes(type: "Node2D", projectPath: _sampleProjectPath);
            var doc = ParseJson(result);

            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                Assert.True(element.TryGetProperty("ScenePath", out _));
                Assert.True(element.TryGetProperty("NodePath", out _));
                Assert.True(element.TryGetProperty("Name", out _));
                Assert.True(element.TryGetProperty("Type", out _));
            }
        }

        [Fact]
        public void ValidateScene_Returns_Valid_Json()
        {
            // Pick the first scene in the sample project.
            var project = new ProjectLocator().Resolve(_sampleProjectPath);
            var scenePath = project.FindScenes().FirstOrDefault()
                ?? throw new InvalidOperationException("No .tscn files in sample project");

            var result = _tools.ValidateScene(scenePath, _sampleProjectPath);
            var doc = ParseJson(result);

            Assert.True(doc.RootElement.TryGetProperty("ScenePath", out _));
            Assert.True(doc.RootElement.TryGetProperty("Issues", out var issues));
            Assert.Equal(JsonValueKind.Array, issues.ValueKind);
        }

        [Fact]
        public void ValidateProject_Returns_Valid_Json()
        {
            var result = _tools.ValidateProject(_sampleProjectPath);
            var doc = ParseJson(result);

            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                Assert.True(item.TryGetProperty("ScenePath", out _));
                Assert.True(item.TryGetProperty("Issues", out var issues));
                Assert.Equal(JsonValueKind.Array, issues.ValueKind);
            }
        }

        [Fact]
        public void LintScene_Returns_Valid_Json()
        {
            var project = new ProjectLocator().Resolve(_sampleProjectPath);
            var scenePath = project.FindScenes().FirstOrDefault()
                ?? throw new InvalidOperationException("No .tscn files in sample project");

            var result = _tools.LintScene(scenePath, projectPath: _sampleProjectPath);
            var doc = ParseJson(result);

            Assert.True(doc.RootElement.TryGetProperty("ScenePath", out _));
            Assert.True(doc.RootElement.TryGetProperty("Issues", out var issues));
            Assert.Equal(JsonValueKind.Array, issues.ValueKind);
        }

        [Fact]
        public void FindOrphanResources_Returns_Valid_Json()
        {
            var result = _tools.FindOrphanResources(_sampleProjectPath);
            var doc = ParseJson(result);

            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                Assert.Equal(JsonValueKind.String, element.ValueKind);
            }
        }

        [Fact]
        public void FindScriptUsages_Returns_Valid_Json()
        {
            // Use a script that is guaranteed to exist in the sample project.
            var project = new ProjectLocator().Resolve(_sampleProjectPath);
            var scriptPath = project.FindScripts().FirstOrDefault()
                ?? throw new InvalidOperationException("No scripts in sample project");

            var result = _tools.FindScriptUsages(scriptPath, _sampleProjectPath);
            var doc = ParseJson(result);

            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                Assert.True(element.TryGetProperty("ScenePath", out _));
                Assert.True(element.TryGetProperty("NodePath", out _));
                Assert.True(element.TryGetProperty("NodeName", out _));
            }
        }

        [Fact]
        public void SceneDiagram_Returns_Valid_Json()
        {
            var project = new ProjectLocator().Resolve(_sampleProjectPath);
            var scenePath = project.FindScenes().FirstOrDefault()
                ?? throw new InvalidOperationException("No .tscn files in sample project");

            var mermaid = _tools.SceneDiagram(scenePath, format: "mermaid", projectPath: _sampleProjectPath);
            var ascii = _tools.SceneDiagram(scenePath, format: "ascii", projectPath: _sampleProjectPath);

            Assert.False(string.IsNullOrWhiteSpace(mermaid));
            Assert.False(string.IsNullOrWhiteSpace(ascii));
        }

        [Fact]
        public void SceneDependencies_Returns_Valid_Json()
        {
            var project = new ProjectLocator().Resolve(_sampleProjectPath);
            var scenePath = project.FindScenes().FirstOrDefault()
                ?? throw new InvalidOperationException("No .tscn files in sample project");

            var result = _tools.SceneDependencies(scenePath, _sampleProjectPath);
            var doc = ParseJson(result);

            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                Assert.True(element.TryGetProperty("Id", out _));
                Assert.True(element.TryGetProperty("Type", out _));
                Assert.True(element.TryGetProperty("Path", out _));
                Assert.True(element.TryGetProperty("Uid", out _));
            }
        }

        [Fact]
        public void NonExistentProjectPath_Throws()
        {
            var bogusPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var ex = Assert.Throws<ArgumentException>(() => _tools.FindNodes(projectPath: bogusPath));
            Assert.Contains("project", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void PathOutsideAllowedRoot_Throws()
        {
            // The server does not impose a specific allowlist for project paths, but we
            // still verify that a clearly invalid absolute path is rejected by the
            // underlying ProjectLocator.
            var outsidePath = Path.GetPathRoot(Environment.CurrentDirectory) ?? "/";
            var ex = Assert.Throws<ArgumentException>(() => _tools.ValidateProject(outsidePath));
            Assert.Contains("project", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
