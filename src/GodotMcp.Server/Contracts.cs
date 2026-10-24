using GodotMcp.Core.Model;

namespace GodotMcp.Server;

public sealed record ProjectInfoResult(string RootPath, string Name, string? MainScene, long ConfigVersion, IReadOnlyList<string> Features, IReadOnlyList<string> Autoloads, string? EngineVersion, bool UsesCSharp)
{
    public static ProjectInfoResult From(ProjectInfo info) => new(
        info.RootPath, info.Name, info.MainScene, info.ConfigVersion,
        info.Features, info.AutoloadSingletons, info.EngineVersion?.ToString(), info.UsesCSharp);
}

public sealed record NodeResult(string Path, string Name, string? Type, string? Parent, string? InstanceId, string? ScriptId, IReadOnlyList<string> Groups, Dictionary<string, string> Properties, IReadOnlyList<NodeResult> Children)
{
    public static NodeResult From(NodeInfo node, bool recurse) => new(
        node.Path, node.Name, node.Type, node.Parent, node.InstanceId, node.ScriptId, node.Groups,
        node.Properties.ToDictionary(p => p.Name, p => p.ValueText),
        recurse ? node.Children.Select(c => From(c, true)).ToList() : []);
}

public sealed record SceneTreeResult(string ScenePath, string? Uid, long Format, int NodeCount, NodeResult? Root, IReadOnlyList<ExternalResourceResult> ExternalResources, IReadOnlyList<ConnectionResult> Connections);

public sealed record ExternalResourceResult(string Id, string Type, string Path, string? Uid);

/// <summary>A signal connection as returned to MCP clients.</summary>
/// <param name="Signal">Name of the emitted signal.</param>
/// <param name="From">Path of the emitting node.</param>
/// <param name="To">Path of the receiving node.</param>
/// <param name="Method">Name of the handler method on the receiving node.</param>
/// <param name="Flags">Godot <c>ConnectFlags</c> bitmask (1 = deferred, 2 = persist, 4 = one-shot, 8 = reference-counted); 0 when the scene does not set it.</param>
/// <param name="Binds">Bound extra arguments in Godot text syntax; empty when the scene does not set them.</param>
public sealed record ConnectionResult(string Signal, string From, string To, string Method, int Flags, IReadOnlyList<string> Binds);

public sealed record ScriptResult(string Path, string Language, string? ClassName, string? BaseType);

public sealed record RunResult(int ExitCode, bool Succeeded, bool TimedOut, double DurationSeconds, string Stdout, string Stderr);

public sealed record MutationResult(bool Changed, string ScenePath, string Detail);

public sealed record NodeMatchResult(string ScenePath, string NodePath, string Name, string? Type);

public sealed record ScriptUsageResult(string ScenePath, string NodePath, string NodeName);

public sealed record IssueResult(string Severity, string Code, string Message);

public sealed record ValidationResult(string ScenePath, bool Valid, int Errors, int Warnings, IReadOnlyList<IssueResult> Issues)
{
    public static ValidationResult From(string scenePath, IReadOnlyList<Core.Scenes.SceneIssue> issues) => new(
        scenePath,
        issues.All(i => i.Severity != Core.Scenes.IssueSeverity.Error),
        issues.Count(i => i.Severity == Core.Scenes.IssueSeverity.Error),
        issues.Count(i => i.Severity == Core.Scenes.IssueSeverity.Warning),
        issues.Select(i => new IssueResult(i.Severity.ToString(), i.Code, i.Message)).ToList());
}
