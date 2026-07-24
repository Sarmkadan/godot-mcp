# AnalysisTools

Utility class providing scene analysis, validation, linting, and dependency discovery features for Godot projects. Designed for integration into tooling pipelines and editor extensions.

## API

### `IReadOnlyList<NodeMatchResult> FindNodes(string pattern, Node parent = null)`

Finds nodes in the scene tree matching the specified pattern. The pattern supports Godot's node path syntax including wildcards and recursive search.

- **Parameters**
  - `pattern`: Search pattern string (e.g., `"%Camera3D"`, `"UI/*"`, `"**/*Timer"`).
  - `parent`: Optional root node to start search from; defaults to the root of the current scene.
- **Return value**: Read-only list of `NodeMatchResult` objects containing matched nodes and their paths.
- **Exceptions**: Throws `ArgumentNullException` if `pattern` is null. Throws `InvalidOperationException` if called outside a valid scene context.

---

### `ValidationResult ValidateScene([Description("Optional scene path to validate")] string scenePath = null)`

Validates the specified scene file against project configuration and best practices. Performs structural checks, resource validation, and configuration consistency.

- **Parameters**
  - `scenePath`: Optional path to the `.tscn` or `.scn` file; defaults to the currently open scene.
- **Return value**: `ValidationResult` containing validation status and list of issues found.
- **Exceptions**: Throws `FileNotFoundException` if `scenePath` does not exist. Throws `UnauthorizedAccessException` on file access errors.

---

### `IReadOnlyList<ValidationResult> ValidateProject()`

Performs comprehensive validation across the entire Godot project. Includes scene validation, resource integrity checks, and project configuration review.

- **Return value**: Read-only list of `ValidationResult` objects, one per validated scene or resource.
- **Exceptions**: Throws `DirectoryNotFoundException` if project root cannot be located. Throws `InvalidOperationException` if project metadata is corrupted.

---
### `ValidationResult LintScene(string scenePath, [Description("Optional linting rules profile")] string profile = null)`

Analyzes scene structure and scripting for style, performance, and maintainability issues using configurable linting rules.

- **Parameters**
  - `scenePath`: Path to the scene file to lint.
  - `profile`: Optional name of linting profile (e.g., `"strict"`, `"performance"`); defaults to `"default"`.
- **Return value**: `ValidationResult` with linting issues categorized by severity.
- **Exceptions**: Throws `ArgumentException` if `profile` is invalid. Throws `FileNotFoundException` if `scenePath` does not exist.

---
### `IReadOnlyList<string> FindOrphanResources()`

Identifies resource files referenced in scenes that no longer exist on disk or are unreachable from the project.

- **Return value**: Read-only list of absolute file paths to orphaned resources.
- **Exceptions**: Throws `DirectoryNotFoundException` if project resources directory is inaccessible.

---
### `IReadOnlyList<ScriptUsageResult> FindScriptUsages([Description("Script file path to search for usages")] string scriptPath)`

Locates all references to a specific script file within the project, including nodes, signals, and code annotations.

- **Parameters**
  - `scriptPath`: Absolute or project-relative path to the `.gd` or `.cs` script file.
- **Return value**: Read-only list of `ScriptUsageResult` objects detailing usage locations and context.
- **Exceptions**: Throws `ArgumentException` if `scriptPath` is not a valid script file. Throws `FileNotFoundException` if the file does not exist.

---
### `string SceneDiagram(string scenePath, [Description("Output format: 'dot' or 'mermaid'")] string format = "dot")`

Generates a textual representation of the scene hierarchy suitable for visualization tools.

- **Parameters**
  - `scenePath`: Path to the scene file.
  - `format`: Output format identifier (`"dot"` for Graphviz, `"mermaid"` for Mermaid.js); defaults to `"dot"`.
- **Return value**: String containing the diagram in the requested format.
- **Exceptions**: Throws `ArgumentException` if `format` is unsupported. Throws `FileNotFoundException` if `scenePath` does not exist.

---
### `IReadOnlyList<ExternalResourceResult> SceneDependencies(string scenePath)`

Enumerates external resources (textures, meshes, fonts, etc.) directly referenced by the specified scene.

- **Parameters**
  - `scenePath`: Path to the scene file.
- **Return value**: Read-only list of `ExternalResourceResult` objects with resource paths and usage context.
- **Exceptions**: Throws `FileNotFoundException` if `scenePath` does not exist.

## Usage
