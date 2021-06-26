# SceneTools

`SceneTools` provides a programmatic interface for interacting with and manipulating Godot scenes and their node hierarchies within the `godot-mcp` ecosystem. It acts as the primary service for querying scene structures, retrieving node information, and executing structural or data mutations on scene files, such as creating or removing nodes and modifying node properties.

## API

### GetSceneTree
Retrieves the full node hierarchy of a specified scene.

*   **Parameters:**
    *   `scenePath` (string): The filesystem path to the `.tscn` file.
*   **Returns:** `SceneTreeResult` containing the hierarchical structure of the scene.
*   **Exceptions:** Throws if the file does not exist, is inaccessible, or fails to parse.

### GetNode
Retrieves detailed information for a specific node within a scene.

*   **Parameters:**
    *   `scenePath` (string): The filesystem path to the `.tscn` file.
    *   `nodePath` (string): The path to the specific node (e.g., "Main/Player").
*   **Returns:** `NodeResult?` containing the node data if found; otherwise `null`.

### CreateNode
Adds a new node to the specified scene.

*   **Parameters:**
    *   `scenePath` (string): The filesystem path to the `.tscn` file.
    *   `name` (string): The name of the new node.
    *   `parentPath` (string): The path to the parent node under which to add the new node.
    *   `type` (string): The type of the new node (e.g., "Node2D").
*   **Returns:** `MutationResult` indicating the success or failure of the operation.

### RemoveNode
Removes a specified node and its subtree from the scene.

*   **Parameters:**
    *   `scenePath` (string): The filesystem path to the `.tscn` file.
    *   `nodePath` (string): The path to the node to remove.
*   **Returns:** `MutationResult` indicating the success or failure of the operation.

### SetNodeProperty
Sets a property value on a specific node.

*   **Parameters:**
    *   `scenePath` (string): The filesystem path to the `.tscn` file.
    *   `nodePath` (string): The path to the target node.
    *   `property` (string): The name of the property to modify.
    *   `value` (object): The new value for the property.
*   **Returns:** `MutationResult` indicating the success or failure of the operation.

### GetNodeProperty
Retrieves the current value of a property from a specific node.

*   **Parameters:**
    *   `scenePath` (string): The filesystem path to the `.tscn` file.
    *   `nodePath` (string): The path to the target node.
    *   `property` (string): The name of the property to query.
*   **Returns:** `string?` representing the value if found; otherwise `null`.

## Usage

### Example 1: Querying a scene structure and node properties

```csharp
var sceneTools = new SceneTools();
string scenePath = "res://scenes/Main.tscn";

// Retrieve the scene hierarchy
var tree = sceneTools.GetSceneTree(scenePath);

// Retrieve a specific node's position property
var position = sceneTools.GetNodeProperty(scenePath, "Main/Player", "position");
Console.WriteLine($"Player position: {position}");
```

### Example 2: Mutating the scene tree

```csharp
var sceneTools = new SceneTools();
string scenePath = "res://scenes/Level.tscn";

// Create a new child node
var creationResult = sceneTools.CreateNode(scenePath, "NewSprite", "Main/Entities", "Sprite2D");

if (creationResult.Success)
{
    // Modify a property of the newly created node
    sceneTools.SetNodeProperty(scenePath, "Main/Entities/NewSprite", "visible", "true");
}
```

## Notes

*   **Thread Safety:** `SceneTools` is not inherently thread-safe. Operations involving file I/O or scene parsing should be synchronized if accessed from multiple threads concurrently. Furthermore, because these tools operate on Godot scene files directly, they do not bypass Godot's internal requirements; ensure that operations do not conflict with active editor instances or running engine instances that may have the target scene file locked.
*   **Pathing:** All `nodePath` parameters expect valid Godot node paths relative to the root of the scene. Invalid paths will typically result in `null` returns for queries or failure results for mutations.
*   **Property Types:** When using `SetNodeProperty`, values are generally handled as strings that are then serialized to the Godot text format. Ensure the provided value is compatible with the target property type to avoid serialization or runtime errors.
