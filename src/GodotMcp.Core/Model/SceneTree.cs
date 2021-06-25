namespace GodotMcp.Core.Model;

public sealed record SignalConnection(string Signal, string From, string To, string Method, IReadOnlyList<string> Binds);

public sealed class SceneTree
{
    public required string ScenePath { get; init; }
    public string? Uid { get; init; }
    public long Format { get; init; }
    public NodeInfo? Root { get; init; }
    public IReadOnlyList<ResourceRef> ExternalResources { get; init; } = [];
    public IReadOnlyList<SubResourceInfo> SubResources { get; init; } = [];
    public IReadOnlyList<SignalConnection> Connections { get; init; } = [];

    public int NodeCount => Root is null ? 0 : 1 + Root.Descendants().Count();

    public NodeInfo? FindNode(string path) => path is "." or "" ? Root : Root?.FindByPath(path);

    public ResourceRef? FindExternalResource(string id) => ExternalResources.FirstOrDefault(r => r.Id == id);
}

public sealed record SubResourceInfo(string Id, string Type, IReadOnlyList<NodeProperty> Properties);
