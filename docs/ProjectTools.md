# ProjectTools

The `ProjectTools` class serves as the primary interface for querying and inspecting Godot engine projects within the `godot-mcp` framework. It provides methods to retrieve high-level project information, enumerate project assets including scenes, resources, and scripts, and perform detailed inspection of individual script files.

## API

### ProjectInfo(string projectPath)
Retrieves metadata and configuration information for the Godot project located at the specified path.
- **Parameters:**
  - `projectPath`: The filesystem path to the Godot project directory.
- **Returns:** A `ProjectInfoResult` containing the project's details.

### ListScenes()
Enumerates all scene files (`.tscn`) discovered within the project scope.
- **Returns:** An `IReadOnlyList<string>` containing the relative paths to all found scene files.

### ListResources()
Enumerates all resource files (`.tres`) discovered within the project scope.
- **Returns:** An `IReadOnlyList<string>` containing the relative paths to all found resource files.

### ListScripts()
Enumerates all script files (`.gd`, `.cs`) discovered within the project scope.
- **Returns:** An `IReadOnlyList<string>` containing the relative paths to all found script files.

### InspectScript(string scriptPath)
Performs an analysis on a specific script file and returns structural or metadata information.
- **Parameters:**
  - `scriptPath`: The relative or absolute path to the script file to inspect.
- **Returns:** A `ScriptResult` containing the inspection output.

## Usage

```csharp
// Example 1: Retrieving project information
var tools = new ProjectTools();
var projectInfo = tools.ProjectInfo("C:/Projects/MyGodotGame");
Console.WriteLine($"Project Name: {projectInfo.Name}");

// Example 2: Enumerating and inspecting scripts
var tools = new ProjectTools();
var scripts = tools.ListScripts();
foreach (var scriptPath in scripts)
{
    var result = tools.InspectScript(scriptPath);
    Console.WriteLine($"Inspected {scriptPath}: {result.Status}");
}
```

## Notes

- **Thread Safety:** The methods within `ProjectTools` are designed to be thread-safe for concurrent read operations. However, if the underlying project files are modified externally during the execution of these methods, the results may not reflect the latest state.
- **Error Handling:** Methods may throw exceptions if the provided paths are invalid, inaccessible, or if the target directory is not a recognized Godot project. Consumers should wrap these calls in appropriate try-catch blocks to handle filesystem-related errors.
- **Path Resolution:** Relative paths are resolved based on the context in which the `ProjectTools` instance was initialized.
