using System.ComponentModel;
using GodotMcp.Core.Project;
using GodotMcp.Core.Scenes;
using ModelContextProtocol.Server;

namespace GodotMcp.Server.Tools;

[McpServerToolType]
public sealed class AnalysisTools(ProjectLocator locator)
{
    [McpServerTool(Name = "godot_find_nodes"), Description("Find nodes across all scenes in the project by type, wildcard name pattern (* and ?), and/or group. At least one filter is required.")]
    public IReadOnlyList<NodeMatchResult> FindNodes(
        [Description("Exact Godot node type, e.g. Sprite2D, CollisionShape2D")] string? type = null,
        [Description("Wildcard name pattern, e.g. 'Enemy*' or '*Button'")] string? namePattern = null,
        [Description("Godot group name the node must belong to")] string? group = null,
        string? projectPath = null)
    {
        if (type is null && namePattern is null && group is null)
            throw new ArgumentException("Provide at least one of: type, namePattern, group");
        var analyzer = new ProjectAnalyzer(locator.Resolve(projectPath));
        return analyzer.FindNodes(type, namePattern, group)
            .Select(m => new NodeMatchResult(m.ScenePath, m.NodePath, m.Name, m.Type))
            .ToList();
    }

    [McpServerTool(Name = "godot_validate_scene"), Description("Validate a scene: missing resource files, unused/undeclared ext and sub resources, duplicate node paths, dangling signal connections, missing GDScript handlers.")]
    public ValidationResult ValidateScene([Description("Scene path as res:// or project-relative")] string scenePath, string? projectPath = null)
    {
        var issues = SceneValidator.Validate(locator.Resolve(projectPath), scenePath);
        return ValidationResult.From(scenePath, issues);
    }

    [McpServerTool(Name = "godot_validate_project"), Description("Validate every scene in the project and return all issues, grouped by scene.")]
    public IReadOnlyList<ValidationResult> ValidateProject(string? projectPath = null)
    {
        var project = locator.Resolve(projectPath);
        var results = new List<ValidationResult>();
        foreach (var scenePath in project.FindScenes().Order())
        {
            IReadOnlyList<SceneIssue> issues;
            try { issues = SceneValidator.Validate(project, scenePath); }
            catch (Exception e) { issues = [new SceneIssue(IssueSeverity.Error, "parse-error", e.Message)]; }
            if (issues.Count > 0) results.Add(ValidationResult.From(scenePath, issues));
        }
        return results;
    }

    [McpServerTool(Name = "godot_lint_scene"), Description("Check a scene for common anti-patterns: default editor names, auto-generated names, names with spaces, duplicate sibling names, deep nesting, signal handlers on script-less nodes.")]
    public ValidationResult LintScene(string scenePath, [Description("Depth at which nesting is flagged")] int maxDepth = SceneLinter.DefaultMaxDepth, string? projectPath = null)
    {
        var tree = locator.Resolve(projectPath).LoadScene(scenePath);
        return ValidationResult.From(scenePath, SceneLinter.Lint(tree, maxDepth));
    }

    [McpServerTool(Name = "godot_find_orphan_resources"), Description("List scenes, resources and scripts not referenced by any scene, resource, script source (res:// literals) or project.godot. Strong deletion candidates, but verify dynamic loads manually.")]
    public IReadOnlyList<string> FindOrphanResources(string? projectPath = null) =>
        new ProjectAnalyzer(locator.Resolve(projectPath)).FindOrphanResources();

    [McpServerTool(Name = "godot_find_script_usages"), Description("Find every scene node that has the given script attached.")]
    public IReadOnlyList<ScriptUsageResult> FindScriptUsages([Description("Script path as res://, e.g. res://player.gd")] string scriptPath, string? projectPath = null) =>
        new ProjectAnalyzer(locator.Resolve(projectPath)).FindScriptUsages(scriptPath)
            .Select(u => new ScriptUsageResult(u.ScenePath, u.NodePath, u.NodeName))
            .ToList();

    [McpServerTool(Name = "godot_scene_diagram"), Description("Export a scene's node tree as a diagram: 'mermaid' (graph with signal connections as dashed edges) or 'ascii' (indented tree with types, scripts and groups).")]
    public string SceneDiagram(string scenePath, [Description("Diagram format: mermaid or ascii")] string format = "mermaid", string? projectPath = null)
    {
        var tree = locator.Resolve(projectPath).LoadScene(scenePath);
        return format.ToLowerInvariant() switch
        {
            "mermaid" => Core.Scenes.SceneDiagram.ToMermaid(tree),
            "ascii" => Core.Scenes.SceneDiagram.ToAsciiTree(tree),
            _ => throw new ArgumentException($"Unknown format '{format}'; use 'mermaid' or 'ascii'")
        };
    }

    [McpServerTool(Name = "godot_scene_dependencies"), Description("List the external resources (scenes, scripts, textures, ...) a scene directly depends on.")]
    public IReadOnlyList<ExternalResourceResult> SceneDependencies(string scenePath, string? projectPath = null) =>
        new ProjectAnalyzer(locator.Resolve(projectPath)).GetSceneDependencies(scenePath)
            .Select(r => new ExternalResourceResult(r.Id, r.Type, r.Path, r.Uid))
            .ToList();
}
