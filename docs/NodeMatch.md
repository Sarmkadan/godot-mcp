# NodeMatch

`NodeMatch` represents a node in a Godot scene or script that has been located or matched during analysis operations. It provides information about the matched node including its path, type, and other metadata useful for refactoring and analysis tools.

## API

### `public sealed record NodeMatch`

A record representing a matched node with its path and type information.

### `public sealed record ScriptUsage`

A record representing a script usage found during analysis, containing the script path and the node path where it is used.

### `public sealed partial class ProjectAnalyzer`

A partial class providing project-wide analysis capabilities for Godot projects.

#### `public GodotProject Project`

Gets the Godot project being analyzed.

**Type:** `GodotProject`

**Remarks:** This property is read-only and represents the project context for all analysis operations.

#### `public IReadOnlyList<NodeMatch> FindNodes()`

Finds all nodes in the project that match certain criteria.

**Returns:** An `IReadOnlyList<NodeMatch>` containing all matched nodes.

**Remarks:**
- The criteria for matching nodes depends on the implementation of the derived partial class.
- The returned list is immutable and thread-safe for read operations.

#### `public IReadOnlyList<ScriptUsage> FindScriptUsages()`

Finds all script usages in the project.

**Returns:** An `IReadOnlyList<ScriptUsage>` containing all found script usages.

**Remarks:**
- Each `ScriptUsage` contains the path to the script and the path to the node where it is used.
- The returned list is immutable and thread-safe for read operations.

#### `public IReadOnlyList<string> FindOrphanResources()`

Finds all resources in the project that are not referenced by any scene or script.

**Returns:** An `IReadOnlyList<string>` containing paths to orphaned resources.

**Remarks:**
- Resource paths are returned as strings relative to the project root.
- The returned list is immutable and thread-safe for read operations.

#### `public IReadOnlyList<ResourceRef> GetSceneDependencies(string scenePath)`

Gets all resource references for a given scene file.

**Parameters:**
- `scenePath` (string): The path to the scene file to analyze.

**Returns:** An `IReadOnlyList<ResourceRef>` containing all resource references found in the scene.

**Throws:**
- `ArgumentNullException`: If `scenePath` is null.
- `FileNotFoundException`: If the scene file does not exist.
- `InvalidOperationException`: If the scene file is not a valid Godot scene.

**Remarks:**
- Each `ResourceRef` contains the resource path and the property path where it is referenced.
- The returned list is immutable and thread-safe for read operations.

## Usage

### Finding all nodes in a project

```csharp
var analyzer = new ProjectAnalyzer(project);
var nodes = analyzer.FindNodes();

foreach (var node in nodes)
{
    Console.WriteLine($"Found node: {node.Path} of type {node.TypeName}");
}
```

### Finding and reporting orphaned resources

```csharp
var analyzer = new ProjectAnalyzer(project);
var orphanedResources = analyzer.FindOrphanResources();

if (orphanedResources.Count > 0)
{
    Console.WriteLine("Orphaned resources found:");
    foreach (var resource in orphanedResources)
    {
        Console.WriteLine($"- {resource}");
    }
}
```

## Notes

- All members return immutable collections (`IReadOnlyList<T>`) which are safe for concurrent reads.
- The analysis operations are not thread-safe; concurrent calls to analysis methods should be synchronized at a higher level.
- Paths returned by these methods are relative to the project root and use forward slashes as separators.
- `FindNodes()` and `FindScriptUsages()` may return large collections for complex projects; consider processing results in batches if memory is constrained.
- `GetSceneDependencies()` performs file I/O and may throw file-related exceptions for invalid or inaccessible scene files.
