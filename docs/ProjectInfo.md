# ProjectInfo

The `ProjectInfo` class serves as the primary data model representing the configuration and metadata of a Godot engine project. It encapsulates critical information parsed from the project's configuration files, enabling automated tools to inspect, validate, and manage Godot projects programmatically.

## API

*   **`RootPath`** (`public required string`)
    The absolute or relative file system path to the root directory of the Godot project. This member is required during initialization.
*   **`Name`** (`public string`)
    The display name of the Godot project as defined in the project configuration.
*   **`Description`** (`public string?`)
    An optional description of the project, providing additional context. Returns `null` if no description is defined.
*   **`MainScene`** (`public string?`)
    The path to the project's designated main scene. Returns `null` if a main scene has not been configured.
*   **`ConfigVersion`** (`public long`)
    The version number of the `project.godot` configuration format.
*   **`Features`** (`public IReadOnlyList<string>`)
    A read-only collection of project features enabled for this project.
*   **`AutoloadSingletons`** (`public IReadOnlyList<string>`)
    A read-only collection of paths to scripts or scenes configured as Autoload (Singleton) nodes.
*   **`EngineVersion`** (`public GodotVersion?`)
    The specific version of the Godot engine required by this project. Returns `null` if the version information is unavailable or undefined.
*   **`ScriptInfo`** (`public sealed record`)
    A nested record type containing metadata and configuration details specific to scripts within the project.

## Usage

### Retrieving Project Configuration
This example demonstrates how to load a project configuration and access its core properties.

```csharp
using GodotMcp.Core;

// Load the project information from a specific directory
ProjectInfo project = ProjectLoader.LoadProject(@"C:\Projects\MyGame");

Console.WriteLine($"Project: {project.Name}");
Console.WriteLine($"Version: {project.ConfigVersion}");

if (project.MainScene != null)
{
    Console.WriteLine($"Main Scene: {project.MainScene}");
}
```

### Inspecting Project Features
This example demonstrates checking for specific features or Autoload singletons configured in the project.

```csharp
using GodotMcp.Core;

ProjectInfo project = ProjectLoader.LoadProject("./");

// Check if the project uses a specific feature
if (project.Features.Contains("GDScript"))
{
    Console.WriteLine("This project utilizes GDScript.");
}

// Iterate over configured Autoload singletons
foreach (var singleton in project.AutoloadSingletons)
{
    Console.WriteLine($"Configured Autoload: {singleton}");
}
```

## Notes

*   **Nullability**: Members marked with `?` (e.g., `Description`, `MainScene`, `EngineVersion`) may return `null` if the underlying `project.godot` file lacks these fields or if the configuration is incomplete. Always perform null checks before accessing these properties to avoid `NullReferenceException`.
*   **Thread Safety**: `ProjectInfo` is designed as an immutable data container. Once initialized, its properties cannot be modified. Consequently, it is safe to access `ProjectInfo` instances concurrently across multiple threads without explicit synchronization.
*   **Path Validation**: The `RootPath` string is not automatically validated for existence on the file system upon initialization of the `ProjectInfo` object. Consumers should verify the path's validity if performing file I/O operations based on this member.
