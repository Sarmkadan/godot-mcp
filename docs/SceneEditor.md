# SceneEditor

The `SceneEditor` class provides a high-level API for programmatically inspecting and modifying Godot scene files (`.tscn`). By wrapping a `TscnDocument` and associating it with a `GodotProject`, this class facilitates the manipulation of scene nodes, their properties, and external resource references, ensuring changes can be validated and persisted back to the filesystem.

## API

### Properties

*   **`GodotProject Project`**
    The `GodotProject` instance associated with this editor, used to resolve project-relative paths and configurations.
*   **`string ScenePath`**
    The absolute or project-relative filesystem path to the `.tscn` file currently being edited.
*   **`TscnDocument Document`**
    The parsed `TscnDocument` model representing the structure and content of the scene. Direct modification of this document is possible but should generally be performed through the `SceneEditor` methods to maintain consistency.

### Methods

*   **`TscnSection AddNode(string name, string type, string parentPath)`**
    Creates and adds a new node (`TscnSection`) to the scene hierarchy.
    *   `name`: The name of the new node.
    *   `type`: The Godot class type of the node (e.g., "Node2D").
    *   `parentPath`: The path to the parent node under which the new node will be added.
    *   Returns the newly created `TscnSection`.
*   **`bool RemoveNode(string nodePath)`**
    Removes a node and its children from the scene.
    *   `nodePath`: The path to the node to remove.
    *   Returns `true` if the node was found and removed, otherwise `false`.
*   **`void SetNodeProperty(string nodePath, string property, GodotValue value)`**
    Sets or updates a property value for the specified node.
    *   `nodePath`: The path to the node.
    *   `property`: The name of the property to set.
    *   `value`: The `GodotValue` to assign.
*   **`bool RemoveNodeProperty(string nodePath, string property)`**
    Removes a specific property from a node.
    *   `nodePath`: The path to the node.
    *   `property`: The name of the property to remove.
    *   Returns `true` if the property existed and was removed, otherwise `false`.
*   **`GodotValue? GetNodeProperty(string nodePath, string property)`**
    Retrieves the current `GodotValue` of a property on a node.
    *   `nodePath`: The path to the node.
    *   `property`: The name of the property to retrieve.
    *   Returns the `GodotValue` if found, otherwise `null`.
*   **`string AddExtResource(string path, string type)`**
    Registers an external resource (e.g., a `.tres` or `.png` file) into the scene's external resource list.
    *   `path`: The path to the external resource.
    *   `type`: The type of the resource.
    *   Returns the unique resource identifier string used within the scene.
*   **`void Save()`**
    Serializes the current state of the `TscnDocument` and writes it to the file system at `ScenePath`. Throws an `IOException` if the file cannot be written.

## Usage

### Modifying a Node Property

```csharp
// Load the scene and update a Sprite2D's visibility
var editor = new SceneEditor(project, "res://player.tscn");
var playerNodePath = "Player/Sprite2D";

// Get current value and toggle it
var isVisible = (bool?)editor.GetNodeProperty(playerNodePath, "visible") ?? true;
editor.SetNodeProperty(playerNodePath, "visible", new GodotValue(!isVisible));

// Persist the changes
editor.Save();
```

### Adding a New Node to the Scene

```csharp
// Add a new Timer node to the scene root
var editor = new SceneEditor(project, "res://level.tscn");

var timerNode = editor.AddNode("AutoSaveTimer", "Timer", "root");
editor.SetNodeProperty("root/AutoSaveTimer", "wait_time", new GodotValue(60.0f));
editor.SetNodeProperty("root/AutoSaveTimer", "autostart", new GodotValue(true));

editor.Save();
```

## Notes

*   **Thread Safety:** The `SceneEditor` class is not thread-safe. Modifications to the `Document` and subsequent calls to `Save()` must be synchronized if accessed from multiple threads.
*   **Path Resolution:** When providing node paths, ensure they are formatted correctly according to the Godot scene tree structure (e.g., "Parent/Child/Grandchild").
*   **File I/O:** The `Save()` method performs blocking file I/O operations. It is recommended to perform save operations asynchronously when used in interactive or GUI-based applications to avoid blocking the main thread.
*   **Validation:** Methods do not perform strict validation against the Godot engine's requirements (e.g., node type compatibility). Ensure inputs match the expected structure of a valid `.tscn` file to avoid generating broken scenes.
