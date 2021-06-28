# SignalConnection

The `SignalConnection` record encapsulates the configuration data for signal connections extracted from a Godot scene file (`.tscn`). It provides a structured representation of the scene's hierarchy, including its node tree, external and internal resource references, and any nested signal connections defined within the context of that scene file.

## API

*   `ScenePath` (string): The file system path to the Godot scene file associated with this connection. This property is required.
*   `Uid` (string?): The unique identifier (UID) for the scene resource, if defined.
*   `Format` (long): The version format identifier of the parsed scene file.
*   `Root` (NodeInfo?): Information regarding the root node of the scene.
*   `ExternalResources` (IReadOnlyList<ResourceRef>): A collection of references to resources external to the scene file, such as scripts or imported assets.
*   `SubResources` (IReadOnlyList<SubResourceInfo>): A collection of internal sub-resource definitions contained within the scene file.
*   `Connections` (IReadOnlyList<SignalConnection>): A collection of nested `SignalConnection` instances that represent sub-connections defined within the current scene context.
*   `FindNode` (NodeInfo?): A helper property providing access to a specific node within the scene's hierarchy, facilitating lookup operations.
*   `FindExternalResource` (ResourceRef?): A helper property providing access to a specific external resource reference within this scene.

### Associated Types

*   `SubResourceInfo`: Represents an internal sub-resource definition utilized within the `SubResources` collection.

## Usage

### Accessing Scene Connection Metadata

This example demonstrates how to extract core information from a `SignalConnection` instance.

```csharp
public void PrintConnectionInfo(SignalConnection connection)
{
    Console.WriteLine($"Scene Path: {connection.ScenePath}");
    Console.WriteLine($"Format Version: {connection.Format}");
    
    if (connection.Uid != null)
    {
        Console.WriteLine($"UID: {connection.Uid}");
    }
}
```

### Iterating Over Nested Connections

This example illustrates traversing the hierarchy of nested connections within a `SignalConnection` object.

```csharp
public void ProcessConnections(SignalConnection connection)
{
    foreach (var nestedConnection in connection.Connections)
    {
        // Process each nested signal connection
        Console.WriteLine($"Nested Scene: {nestedConnection.ScenePath}");
        
        // Recursively handle deeper connections if necessary
        ProcessConnections(nestedConnection);
    }
}
```

## Notes

*   **Immutability**: As a `sealed record`, `SignalConnection` provides immutability for the record instance itself. The contents of the `IReadOnlyList<T>` properties are protected against direct modification through the interface, ensuring the integrity of the connection configuration once parsed.
*   **Thread Safety**: Since `SignalConnection` is immutable and its collection properties expose only read-only interfaces, instances are inherently thread-safe for concurrent read operations.
*   **Nullability**: Several properties, including `Uid`, `Root`, `FindNode`, and `FindExternalResource`, are nullable. Consumers must perform null checks before accessing these members to avoid `NullReferenceException`.
*   **Performance**: The `FindNode` and `FindExternalResource` properties may involve search operations depending on the implementation; their usage in performance-critical paths should be evaluated accordingly.
