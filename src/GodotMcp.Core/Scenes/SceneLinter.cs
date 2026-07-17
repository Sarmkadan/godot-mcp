using System.Text.RegularExpressions;
using GodotMcp.Core.Model;

namespace GodotMcp.Core.Scenes;

public static partial class SceneLinter
{
    public const int DefaultMaxDepth = 10;

    public static IReadOnlyList<SceneIssue> Lint(SceneTree tree, int maxDepth = DefaultMaxDepth)
    {
        ArgumentNullException.ThrowIfNull(tree);
        var issues = new List<SceneIssue>();
        if (tree.Root is null)
        {
            issues.Add(new SceneIssue(IssueSeverity.Error, "empty-scene", $"'{tree.ScenePath}' contains no nodes"));
            return issues;
        }
        LintNode(tree.Root, depth: 0, maxDepth, issues);
        var scriptless = tree.Connections
            .Select(c => tree.FindNode(c.To))
            .Where(n => n is { ScriptId: null, InstanceId: null })
            .Select(n => n!.Path)
            .Distinct()
            .ToList();
        foreach (var path in scriptless)
            issues.Add(new SceneIssue(IssueSeverity.Warning, "handler-without-script", $"node '{path}' receives signal connections but has no script attached"));
        return issues;
    }

    static void LintNode(NodeInfo node, int depth, int maxDepth, List<SceneIssue> issues)
    {
        if (node.Name.Contains(' '))
            issues.Add(new SceneIssue(IssueSeverity.Warning, "name-with-spaces", $"node '{node.Path}' has spaces in its name; NodePath lookups become awkward"));
        if (node.Name.StartsWith('@'))
            issues.Add(new SceneIssue(IssueSeverity.Warning, "auto-generated-name", $"node '{node.Path}' has an auto-generated name; rename it for stable NodePaths"));
        if (node.Type is { } type && DefaultNameRegex(type).IsMatch(node.Name))
            issues.Add(new SceneIssue(IssueSeverity.Warning, "default-name", $"node '{node.Path}' still has the default editor name for type '{type}'"));
        if (depth == maxDepth)
            issues.Add(new SceneIssue(IssueSeverity.Warning, "deep-nesting", $"node '{node.Path}' is nested {depth} levels deep; consider splitting into sub-scenes"));
        var duplicates = node.Children.GroupBy(c => c.Name).Where(g => g.Count() > 1);
        foreach (var group in duplicates)
            issues.Add(new SceneIssue(IssueSeverity.Error, "duplicate-sibling-name", $"node '{node.Path}' has {group.Count()} children named '{group.Key}'"));
        foreach (var child in node.Children)
            LintNode(child, depth + 1, maxDepth, issues);
    }

    static Regex DefaultNameRegex(string type) => new($"^{Regex.Escape(type)}\\d*$");
}
