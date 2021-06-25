namespace GodotMcp.Core.Model;

public sealed class NodeInfo
{
    public required string Name { get; init; }
    public string? Type { get; init; }
    public string? Parent { get; init; }
    public string? InstanceId { get; init; }
    public string? ScriptId { get; init; }
    public IReadOnlyList<string> Groups { get; init; } = [];
    public List<NodeProperty> Properties { get; } = [];
    public List<NodeInfo> Children { get; } = [];

    public string Path => Parent switch
    {
        null => ".",
        "." => Name,
        _ => $"{Parent}/{Name}"
    };

    public NodeInfo? FindChild(string name) => Children.FirstOrDefault(c => c.Name == name);

    public NodeInfo? FindByPath(string path)
    {
        if (path is "." or "") return this;
        var current = this;
        foreach (var segment in path.Split('/'))
        {
            current = current.FindChild(segment);
            if (current is null) return null;
        }
        return current;
    }

    public IEnumerable<NodeInfo> Descendants()
    {
        foreach (var child in Children)
        {
            yield return child;
            foreach (var d in child.Descendants()) yield return d;
        }
    }
}
