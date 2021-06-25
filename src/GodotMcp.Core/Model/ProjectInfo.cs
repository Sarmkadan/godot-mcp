namespace GodotMcp.Core.Model;

public sealed record ProjectInfo
{
    public required string RootPath { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public string? MainScene { get; init; }
    public long ConfigVersion { get; init; }
    public IReadOnlyList<string> Features { get; init; } = [];
    public IReadOnlyList<string> AutoloadSingletons { get; init; } = [];
    public GodotVersion? EngineVersion { get; init; }
    public bool UsesCSharp => Features.Contains("C#");
}

public sealed record ScriptInfo(string Path, string Language, string? ClassName, string? BaseType);
