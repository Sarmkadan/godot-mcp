namespace GodotMcp.Core.Model;

/// <summary>A signal connection from a <c>[connection]</c> section.</summary>
/// <param name="Signal">Name of the emitted signal.</param>
/// <param name="From">Path of the emitting node.</param>
/// <param name="To">Path of the receiving node.</param>
/// <param name="Method">Name of the handler method on the receiving node.</param>
/// <param name="Flags">Godot <c>ConnectFlags</c> bitmask (1 = deferred, 2 = persist, 4 = one-shot, 8 = reference-counted); 0 when absent.</param>
/// <param name="Binds">Bound extra arguments, each in Godot text syntax; empty when absent.</param>
public sealed record SignalConnection(string Signal, string From, string To, string Method, int Flags, IReadOnlyList<string> Binds);

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
