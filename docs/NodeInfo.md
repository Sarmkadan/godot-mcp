# NodeInfo

The `NodeInfo` class encapsulates the structural and configuration data for a Godot node as processed by the `godot-mcp` library. It provides a hierarchical representation of a scene, including node identification, type information, associated properties, and the parent-child relationships, facilitating inspection and manipulation of Godot project structures.

## API

*   `public required string Name`: The mandatory name of the node.
*   `public string? Type`: The optional Godot class type (e.g., "Node2D", "Sprite2D").
*   `public string? Parent`: The identifier or path of the parent node, if applicable.
*   `public string? InstanceId`: The unique identifier for this specific instance of the node.
*   `public string? ScriptId`: The identifier for the script attached to the node, if any.
*   `public IReadOnlyList<string> Groups`: A read-only collection of names of the groups to which this node belongs.
*   `public List<NodeProperty> Properties`: A list containing the properties defined on the node.
*   `public List<NodeInfo> Children`: A list of immediate child `NodeInfo` instances.
*   `public NodeInfo? FindChild`: Retrieves a specific direct child node by its identifier.
*   `public NodeInfo? FindByPath`: Retrieves a descendant node by its path.
*   `public IEnumerable<NodeInfo> Descendants`: An enumerable collection of all recursive descendants of this node.

## Usage

### Traversing the Hierarchy

```csharp
void PrintNodeStructure(NodeInfo node, int depth = 0)
{
    string indent = new string(' ', depth * 2);
    Console.WriteLine($"{indent}- {node.Name} ({node.Type})");
    foreach (var child in node.Children)
    {
        PrintNodeStructure(child, depth + 1);
    }
}
```

### Accessing Node Properties

```csharp
void LogNodeDetails(NodeInfo node)
{
    Console.WriteLine($"Node: {node.Name}");
    if (node.Groups.Count > 0)
    {
        Console.WriteLine($"Groups: {string.Join(", ", node.Groups)}");
    }
    foreach (var prop in node.Properties)
    {
        Console.WriteLine($"  Property: {prop.Name}");
    }
}
```

## Notes

*   **Data Integrity**: Properties like `Type`, `Parent`, `InstanceId`, and `ScriptId` are nullable and may be `null` if the data is not present in the serialized Godot scene or project file.
*   **Mutability**: The `Properties` and `Children` lists are mutable. Modifications to these lists directly affect the `NodeInfo` instance.
*   **Thread Safety**: `NodeInfo` instances are not inherently thread-safe. Concurrent access for reading and writing, especially modifying the `Properties` or `Children` collections, should be protected by external synchronization mechanisms.
*   **Recursive Traversal**: The `Descendants` property provides a convenient way to iterate through the entire subtree, but it does not guarantee a specific order of traversal.
