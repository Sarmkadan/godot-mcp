# ProjectInfoResult

The `ProjectInfoResult` and associated records provide a structured data model for representing Godot project information, scene hierarchies, and the results of various operations within the `godot-mcp` system. These types facilitate the consistent parsing and handling of project data, enabling robust communication between the MCP server and the Godot engine's project structures.

## API

### ProjectInfoResult
`public sealed record ProjectInfoResult`
Represents the top-level configuration and metadata of a Godot project.

*   `public static ProjectInfoResult From(...)`
    A static factory method to construct a `ProjectInfoResult` from project-specific inputs.

### NodeResult
`public sealed record NodeResult`
Represents a single node instance within a Godot scene tree.

*   `public static NodeResult From(...)`
    A static factory method to construct a `NodeResult` from source data.

### SceneTreeResult
`public sealed record SceneTreeResult`
Encapsulates the hierarchical structure of a Godot scene.

### ExternalResourceResult
`public sealed record ExternalResourceResult`
Defines metadata and references for resources external to the scene file.

### ConnectionResult
`public sealed record ConnectionResult`
Represents a signal connection between nodes within a scene.

### ScriptResult
`public sealed record ScriptResult`
Encapsulates data related to a script attached to a node or resource.

### RunResult
`public sealed record RunResult`
Represents the output or status resulting from an executed command or scene run operation.

### MutationResult
`public sealed record MutationResult`
Represents the outcome of a modification operation performed on a project or scene element.

## Usage

### Example 1: Loading Project Information
```csharp
// Load project data using the static factory method
string projectPath = "/projects/my_godot_game/project.godot";
ProjectInfoResult projectInfo = ProjectInfoResult.From(projectPath);

// Access project-level details
Console.WriteLine("Project loaded successfully.");
```

### Example 2: Parsing Scene Node Data
```csharp
// Assume 'nodeData' is raw input from the project parser
NodeResult sceneNode = NodeResult.From(nodeData);

// Interact with the structured node data
if (sceneNode != null)
{
    // Process the node information
}
```

## Notes

*   **Immutability:** All types listed above are defined as C# `sealed record` types. Instances are immutable once constructed, ensuring that data integrity is maintained throughout the application lifecycle.
*   **Thread Safety:** Because these records are immutable, instances are thread-safe and can be safely shared across multiple threads without explicit synchronization.
*   **Usage Context:** These records are designed as data transfer objects (DTOs) and should not contain business logic. Mutations to the underlying Godot project should be channeled through appropriate service layers, resulting in new `MutationResult` instances.
