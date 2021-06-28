# GodotProject

The `GodotProject` class provides a structured interface for interacting with a Godot engine project on the file system. It serves as the primary entry point for discovering project contents, loading configuration data, parsing scene files, and performing static analysis on scripts.

## API

### Properties

*   **`public string RootPath { get; }`**
    Gets the absolute system path to the root directory of the Godot project.

### Methods

*   **`public static GodotProject Open(string path)`**
    Opens a Godot project located at the specified system path. Throws `ArgumentException` if the directory does not exist or does not contain a valid `project.godot` file.

*   **`public ProjectInfo LoadInfo()`**
    Parses and returns the `ProjectInfo` object derived from the project's `project.godot` configuration file.

*   **`public string ResolveResPath(string resPath)`**
    Converts a Godot-style `res://` path into an absolute system file path. Throws an exception if the `res://` path cannot be mapped.

*   **`public string ToResPath(string absolutePath)`**
    Converts an absolute system file path into a Godot-style `res://` path. Throws an exception if the file is outside the project root.

*   **`public IEnumerable<string> EnumerateFiles(string pattern = "*")`**
    Returns a sequence of all file paths within the project, optionally filtered by a glob pattern.

*   **`public IEnumerable<string> FindScenes()`**
    Returns a sequence of all `.tscn` scene files found within the project.

*   **`public IEnumerable<string> FindResources()`**
    Returns a sequence of all resource file paths (e.g., `.tres`, `.res`) within the project.

*   **`public IEnumerable<string> FindScripts()`**
    Returns a sequence of all script file paths (e.g., `.gd`, `.cs`) within the project.

*   **`public TscnDocument LoadDocument(string resPath)`**
    Loads and parses the specified scene file into a `TscnDocument` model for manipulation or inspection. Throws `FileNotFoundException` if the path is invalid.

*   **`public void SaveDocument(TscnDocument doc, string resPath)`**
    Serializes and saves a `TscnDocument` instance back to the project file system at the provided `resPath`.

*   **`public SceneTree LoadScene(string resPath)`**
    Loads a scene file and reconstructs its node hierarchy into a `SceneTree` structure.

*   **`public ScriptInfo InspectScript(string resPath)`**
    Performs static analysis on the specified script file and returns its `ScriptInfo`, including metadata, exports, and signals.

## Usage

### Example 1: Listing All Scenes in a Project

```csharp
using Godot.MCP;

// Open the project from a local directory
var project = GodotProject.Open("/path/to/my/godot_project");

// Retrieve and output all scene paths
foreach (var scenePath in project.FindScenes())
{
    Console.WriteLine($"Found scene: {scenePath}");
}
```

### Example 2: Loading and Modifying a Scene

```csharp
using Godot.MCP;

var project = GodotProject.Open("/path/to/my/godot_project");
string sceneResPath = "res://scenes/main.tscn";

// Load the document representation
TscnDocument doc = project.LoadDocument(sceneResPath);

// Perform modifications on the document structure here
// ...

// Save the changes back to the file system
project.SaveDocument(doc, sceneResPath);
```

## Notes

*   **File Access:** This class relies on standard file system I/O. Operations may throw `IOException` or `UnauthorizedAccessException` depending on file permissions or locking by the Godot Editor.
*   **Path Resolution:** All methods expecting `res://` paths assume the path is correctly formatted relative to the project root. Providing malformed paths will result in exceptions.
*   **Thread Safety:** Instances of `GodotProject` are generally designed for single-threaded usage. Concurrent modifications to the same `TscnDocument` or simultaneous writes to the same project file are not supported and may lead to data corruption.
*   **Performance:** `EnumerateFiles`, `FindScenes`, `FindResources`, and `FindScripts` perform synchronous file system scanning. For large projects, these operations may be I/O intensive.
