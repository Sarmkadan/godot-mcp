# godot-mcp

An MCP server that lets any MCP client (Claude Code, Cursor, or your own agent) read and edit Godot 4 projects: scene trees, nodes, properties, resources, scripts, headless runs.

Unity has a popular MCP bridge with over 12k stars; Godot's C# side has had nothing comparable. The design difference here: godot-mcp works on the project files directly (`project.godot`, `.tscn`, `.tres`), so it needs no Godot installation and no engine assemblies - it runs anywhere, including CI.

## Install

```sh
dotnet tool install -g GodotMcp
```

## Quickstart

Add to your MCP client config (Claude Code, Cursor, or any stdio MCP client):

```json
{
  "mcpServers": {
    "godot": {
      "command": "godot-mcp",
      "args": ["/path/to/your/godot/project"]
    }
  }
}
```

The argument is the project root (the folder containing `project.godot`). Alternatively set the `GODOT_PROJECT` environment variable, or omit both to use the current directory. Every tool also accepts an explicit `projectPath` parameter, so one server can serve multiple projects.

## Getting Started

Step by step, from a clean checkout to a working MCP connection. The commands were run on Linux against this repository; the Claude Desktop config and the Windows paths were not exercised.

### 1. Build

Requires the .NET 10 SDK.

```sh
git clone https://github.com/Sarmkadan/godot-mcp.git
cd godot-mcp
dotnet build -c Release
dotnet test -c Release --no-build
```

Nine integration tests that need a real `godot` binary are marked `Skip` in `HeadlessRunnerIntegrationTests.cs`; the remaining tests should pass (134 passed, 9 skipped at the time of writing).

The server binary is the apphost produced by the build:

```
src/GodotMcp.Server/bin/Release/net10.0/GodotMcp.Server        (Linux/macOS)
src/GodotMcp.Server/bin/Release/net10.0/GodotMcp.Server.exe    (Windows)
```

Optional: pack it as a local dotnet tool, which gives you a `godot-mcp` command instead of a path:

```sh
dotnet pack src/GodotMcp.Server -c Release -o artifacts
dotnet tool install --tool-path "$HOME/.godot-mcp-tool" --add-source artifacts GodotMcp
"$HOME/.godot-mcp-tool/godot-mcp" /path/to/your/godot/project
```

### 2. Run

MCP clients start the server for you, so you normally never run it by hand. To check it starts, run it against a project; it logs to stderr and then waits for MCP messages on stdin (press Ctrl+C to stop):

```sh
dotnet run --project src/GodotMcp.Server -c Release -- /path/to/your/godot/project
```

The `WARNING: No executable allowlist configured` line is expected. The allowlist only gates the run tools (`godot_run_headless`, `godot_import_resources`); the analysis tools do not need it. To enable those, pass `--allowlist-executable=/path/to/godot`.

### 3. Connect a client

Use absolute paths in client configs. Replace `/absolute/path/to/godot-mcp` and `/absolute/path/to/your/godot/project` with your own.

**Claude Desktop** - add to `claude_desktop_config.json` (macOS: `~/Library/Application Support/Claude/`, Windows: `%APPDATA%\Claude\`), then restart the app:

```json
{
  "mcpServers": {
    "godot": {
      "command": "/absolute/path/to/godot-mcp/src/GodotMcp.Server/bin/Release/net10.0/GodotMcp.Server",
      "args": ["/absolute/path/to/your/godot/project"]
    }
  }
}
```

On Windows, point `command` at `GodotMcp.Server.exe` and escape backslashes in the JSON (`"C:\\src\\godot-mcp\\..."`).

**Claude Code** - run this once from any directory; it writes a `.mcp.json` into the current directory, so run it from your Godot project root to share the config with your team:

```sh
claude mcp add --scope project godot -- /absolute/path/to/godot-mcp/src/GodotMcp.Server/bin/Release/net10.0/GodotMcp.Server /absolute/path/to/your/godot/project
```

Resulting `.mcp.json` (verified with `claude mcp add` in a scratch directory):

```json
{
  "mcpServers": {
    "godot": {
      "type": "stdio",
      "command": "/absolute/path/to/godot-mcp/src/GodotMcp.Server/bin/Release/net10.0/GodotMcp.Server",
      "args": [
        "/absolute/path/to/your/godot/project"
      ],
      "env": {}
    }
  }
}
```

Use `--scope user` instead of `--scope project` to make the server available in every project. If you packed the tool (above), use `godot-mcp` as the command instead of the apphost path and keep the project path argument.

### 4. Analysis tool walkthroughs

The examples use the small demo project below. Copy the files into a folder (the scene files are plain text, so no Godot install is needed), then point the server at that folder. `art/missing.png` is intentionally absent so the validation examples have something to report.

<details>
<summary>Demo project files</summary>

```
demo/
├── project.godot
├── main.tscn
├── ui/
│   ├── hud.tscn
│   └── hud.gd
└── scripts/
    ├── player.gd
    └── unused_helper.gd
```

`project.godot`

```ini
config_version=5

[application]

config/name="Demo"
run/main_scene="res://main.tscn"
config/features=PackedStringArray("4.4")
```

`main.tscn`

```ini
[gd_scene load_steps=4 format=3]

[ext_resource type="Script" path="res://scripts/player.gd" id="1_ply"]
[ext_resource type="PackedScene" path="res://ui/hud.tscn" id="2_hud"]
[ext_resource type="Texture2D" path="res://art/missing.png" id="3_tex"]

[node name="Main" type="Node2D"]

[node name="Player" type="CharacterBody2D" parent="." groups=["enemies"]]
script = ExtResource("1_ply")

[node name="Sprite2D" type="Sprite2D" parent="Player"]
texture = ExtResource("3_tex")

[node name="CollisionShape2D" type="CollisionShape2D" parent="Player"]

[node name="Hud" parent="." instance=ExtResource("2_hud")]
```

`ui/hud.tscn`

```ini
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://ui/hud.gd" id="1_hud"]

[node name="Hud" type="CanvasLayer"]
script = ExtResource("1_hud")

[node name="StartButton" type="Button" parent="."]

[node name="Button2" type="Button" parent="."]

[node name="Label Score" type="Label" parent="."]

[connection signal="pressed" from="StartButton" to="." method="_on_start_pressed"]
```

`ui/hud.gd` (deliberately has no `_on_start_pressed`)

```gdscript
extends CanvasLayer

func _ready() -> void:
	pass
```

`scripts/player.gd`

```gdscript
extends CharacterBody2D

const SPEED := 200.0
```

`scripts/unused_helper.gd` (nothing references it)

```gdscript
extends Node

func help() -> void:
	pass
```

</details>

Each example shows the arguments object an MCP client sends for the tool call (every tool also accepts an optional `projectPath`, omitted here), followed by the response. Responses are real server output, pretty-printed and trimmed where noted.

#### `godot_find_nodes`

Find every `CollisionShape2D` in the project. Other filters: `namePattern` (`*` and `?` wildcards, e.g. `"Button*"`) and `group`. At least one is required.

```json
{ "type": "CollisionShape2D" }
```

```json
[
  {
    "scenePath": "res://main.tscn",
    "nodePath": "Player/CollisionShape2D",
    "name": "CollisionShape2D",
    "type": "CollisionShape2D"
  }
]
```

#### `godot_validate_scene`

Check one scene for missing files, unused or undeclared resources, duplicate paths, dangling connections and missing GDScript handlers.

```json
{ "scenePath": "res://ui/hud.tscn" }
```

```json
{
  "scenePath": "res://ui/hud.tscn",
  "valid": true,
  "errors": 0,
  "warnings": 1,
  "issues": [
    {
      "severity": "Warning",
      "code": "missing-handler",
      "message": "connection 'pressed' targets method '_on_start_pressed' which is not defined in 'res://ui/hud.gd'"
    }
  ]
}
```

`valid` is `false` only when there are errors; warnings do not fail validation.

#### `godot_validate_project`

Validate every scene and return only the scenes that have issues. Takes no arguments.

```json
{}
```

```json
[
  {
    "scenePath": "res://main.tscn",
    "valid": false,
    "errors": 1,
    "warnings": 0,
    "issues": [
      {
        "severity": "Error",
        "code": "missing-file",
        "message": "ext_resource '3_tex' points to missing file 'res://art/missing.png'"
      }
    ]
  },
  {
    "scenePath": "res://ui/hud.tscn",
    "valid": true,
    "errors": 0,
    "warnings": 1,
    "issues": [
      {
        "severity": "Warning",
        "code": "missing-handler",
        "message": "connection 'pressed' targets method '_on_start_pressed' which is not defined in 'res://ui/hud.gd'"
      }
    ]
  }
]
```

#### `godot_lint_scene`

Check one scene for style anti-patterns. `maxDepth` (default `10`) sets the nesting depth that gets flagged.

```json
{ "scenePath": "res://ui/hud.tscn" }
```

```json
{
  "scenePath": "res://ui/hud.tscn",
  "valid": true,
  "errors": 0,
  "warnings": 2,
  "issues": [
    {
      "severity": "Warning",
      "code": "default-name",
      "message": "node 'Button2' still has the default editor name for type 'Button'"
    },
    {
      "severity": "Warning",
      "code": "name-with-spaces",
      "message": "node 'Label Score' has spaces in its name; NodePath lookups become awkward"
    }
  ]
}
```

#### `godot_find_orphan_resources`

List scenes, resources and scripts that nothing references. Treat the result as deletion candidates: dynamic `load()` calls built at runtime are invisible to the scan.

```json
{}
```

```json
[
  "res://scripts/unused_helper.gd"
]
```

#### `godot_find_script_usages`

Find every node, in every scene, that has a given script attached.

```json
{ "scriptPath": "res://scripts/player.gd" }
```

```json
[
  {
    "scenePath": "res://main.tscn",
    "nodePath": "Player",
    "nodeName": "Player"
  }
]
```

#### `godot_scene_diagram`

Render a scene's node tree. `format` is `"mermaid"` (the default; a graph with signal connections as dashed edges) or `"ascii"`. The response is plain text, not JSON. Trimmed trailing blank lines.

```json
{ "scenePath": "res://main.tscn", "format": "ascii" }
```

```
Main (Node2D)
├── Player (CharacterBody2D) [script] {enemies}
│   ├── Sprite2D (Sprite2D)
│   └── CollisionShape2D (CollisionShape2D)
└── Hud
```

#### `godot_scene_dependencies`

List the external resources a scene directly references (not transitive dependencies).

```json
{ "scenePath": "res://main.tscn" }
```

```json
[
  { "id": "1_ply", "type": "Script", "path": "res://scripts/player.gd" },
  { "id": "2_hud", "type": "PackedScene", "path": "res://ui/hud.tscn" },
  { "id": "3_tex", "type": "Texture2D", "path": "res://art/missing.png" }
]
```

## Tools

| Tool | Description |
| --- | --- |
| `godot_project_info` | Read `project.godot`: name, main scene, features, autoloads, engine version |
| `godot_list_scenes` | List all `.tscn` scene files as `res://` paths |
| `godot_list_resources` | List resource files (`.tres`, `.res`, `.gdshader`, `.material`) |
| `godot_list_scripts` | List script files (`.cs`, `.gd`) |
| `godot_inspect_script` | Get a script's language, class name and base type |
| `godot_get_scene_tree` | Full node tree of a scene with types, groups and properties |
| `godot_get_node` | Get a single node by scene-tree path, with its properties |
| `godot_create_node` | Add a node under a given parent and save the scene |
| `godot_remove_node` | Remove a node and its descendants from a scene |
| `godot_rename_node` | Rename a node; parent paths of descendants and signal connections are rewritten |
| `godot_set_node_property` | Set a node property using Godot text syntax (`Vector2(10, 20)`, `true`, ...) |
| `godot_get_node_property` | Get a single node property value in Godot text syntax |
| `godot_connect_signal` | Add a `[connection]` between two nodes (idempotent) |
| `godot_disconnect_signal` | Remove a signal connection |
| `godot_read_resource` | Read a `.tres`/`.tscn` file as raw text |
| `godot_write_resource` | Write a `.tres`/`.tscn` file, validated as Godot resource text before saving |
| `godot_resource_summary` | Structure of a resource file: descriptor, sections, external references |
| `godot_binary_info` | Locate the installed `godot` binary and report its version |
| `godot_run_headless` | Run the project (or a scene) headlessly, return exit code and output |
| `godot_import_resources` | Run `godot --headless --import` to (re)import project resources |

### Analysis tools

| Tool | Description |
| --- | --- |
| `godot_find_nodes` | Find nodes across every scene by type, wildcard name pattern and/or group |
| `godot_validate_scene` | Missing resource files, unused/undeclared ext and sub resources, duplicate node paths, dangling connections, missing GDScript handlers |
| `godot_validate_project` | Run scene validation over the whole project |
| `godot_lint_scene` | Anti-patterns: default editor names, auto-generated names, names with spaces, duplicate siblings, deep nesting, handlers on script-less nodes |
| `godot_find_orphan_resources` | Scenes/resources/scripts referenced by nothing (including `res://` literals in script sources) |
| `godot_find_script_usages` | Every node in every scene with a given script attached |
| `godot_scene_dependencies` | External resources a scene directly depends on |
| `godot_scene_diagram` | Export a scene as a Mermaid graph (with signal edges) or ASCII tree |

## Examples

Ask your MCP client things like:

> *"Which nodes in my project are CollisionShape2D? Do any of them still have default names?"*

`godot_find_nodes` with `type=CollisionShape2D`, then `godot_lint_scene` per scene:

```json
[
  { "scenePath": "res://levels/cave.tscn", "nodePath": "Player/CollisionShape2D", "name": "CollisionShape2D", "type": "CollisionShape2D" }
]
```

> *"Rename the `Player` node to `Hero` in `main.tscn` without breaking anything."*

`godot_rename_node` rewrites the node's `name`, every descendant's `parent` path, and the `from`/`to` endpoints of all `[connection]` sections in one save - the usual failure mode of hand-editing `.tscn` files.

> *"Is anything wrong with my scenes after that merge?"*

`godot_validate_project` returns per-scene issues:

```json
{
  "scenePath": "res://ui/hud.tscn",
  "valid": false,
  "errors": 1,
  "warnings": 1,
  "issues": [
    { "severity": "Error", "code": "missing-file", "message": "ext_resource '2_font' points to missing file 'res://fonts/hud.ttf'" },
    { "severity": "Warning", "code": "missing-handler", "message": "connection 'pressed' targets method '_on_start_pressed' which is not defined in 'res://ui/hud.gd'" }
  ]
}
```

> *"What can I safely delete?"*

`godot_find_orphan_resources` lists files referenced by no scene, resource, `project.godot` entry, or `res://` literal in any script. Dynamic `load()` calls built from string concatenation cannot be detected, so treat the list as candidates.

> *"Show me the structure of the boss scene."*

`godot_scene_diagram` with `format=ascii`:

```
Boss (CharacterBody2D) [script] {enemies}
├── Sprite (AnimatedSprite2D)
├── Hitbox (Area2D)
│   └── CollisionShape2D (CollisionShape2D)
└── HealthBar (ProgressBar)
```

or `format=mermaid` for a paste-ready graph where signal connections appear as dashed labeled edges.

## Headless execution

`godot_run_headless`, `godot_import_resources` and `godot_binary_info` launch the real `godot` binary with `--headless`. Nothing else in the server needs it. The same code is available to .NET callers as `HeadlessRunner`.

### How the binary is located

`GodotExecutable.Locate()` tries these in order and returns the first match that is allowlisted:

1. The `GODOT_BIN` environment variable, then `GODOT`. If either points at an existing file, that file is used. If it is set but not allowlisted, the search stops there and does **not** fall back to `PATH`.
2. A `PATH` search for `godot`, `godot4` or `godot-mono` (Windows: `godot.exe`, `godot4.exe`, `Godot_v4.exe`).

There is no config file. The only setting is the executable allowlist, passed as a server argument and repeatable:

```
--allowlist-executable=/absolute/path/to/godot
--allowlist-executable /absolute/path/to/godot      # equivalent
```

The server fails closed: without an allowlist, `Locate()` returns nothing, so `godot_binary_info` reports `path: null` and the run tools fail, even when `godot` is installed. The allowlisted path must be absolute, must exist at startup (otherwise the server exits with code 1), and must match the path `Locate()` finds character for character. Symlinks are not resolved, so if `which godot` prints `/usr/bin/godot`, allowlist `/usr/bin/godot`, not the file it links to.

Check what the server sees with `godot_binary_info`, or run `which godot` in the same shell that starts the server.

### Example: MCP client config

Allowlist the binary and point `GODOT_BIN` at the same path:

```json
{
  "mcpServers": {
    "godot": {
      "command": "godot-mcp",
      "args": [
        "/absolute/path/to/your/godot/project",
        "--allowlist-executable=/usr/local/bin/godot"
      ],
      "env": {
        "GODOT_BIN": "/usr/local/bin/godot"
      }
    }
  }
}
```

### Example: run a scene and a script

Scene paths are **project-relative** (`main.tscn`, `ui/hud.tscn`). Do not use `res://`: the server resolves paths against the project folder, so `res://main.tscn` is treated as a file that does not exist. Omit `scene` to run the project's main scene.

Run a scene from an MCP client:

```json
{ "scene": "ui/hud.tscn", "timeoutSeconds": 30 }
```

Run the main scene with the default timeout:

```json
{}
```

Both calls return `ExitCode`, `Succeeded`, `TimedOut`, `DurationSeconds`, `Stdout` and `Stderr`.

No MCP tool runs a script yet. From .NET, `HeadlessRunner.RunScriptAsync` runs a `--script` file. Create `scripts/headless_check.gd` in your project:

```gdscript
extends SceneTree

func _init() -> void:
	print("headless check: ", ProjectSettings.get_setting("application/config/name"))
	quit(0)
```

Then call it from a console app that references the `GodotMcp.Core` project:

```csharp
using GodotMcp.Core.Editor;
using GodotMcp.Core.Project;

var godotPath = "/usr/local/bin/godot";
ExecutableAllowlist.Initialize([godotPath]);

var project = GodotProject.Open("/absolute/path/to/your/godot/project");
var runner = new HeadlessRunner(project, new GodotExecutable(godotPath))
{
    DefaultTimeout = TimeSpan.FromSeconds(30)
};

var script = await runner.RunScriptAsync("scripts/headless_check.gd", Console.WriteLine);
Console.WriteLine($"script: exit={script.ExitCode} timedOut={script.TimedOut}");

var scene = await runner.RunProjectAsync("main.tscn");
Console.WriteLine($"scene: exit={scene.ExitCode} succeeded={scene.Succeeded}");
```

`RunScriptAsync` and `RunProjectAsync` also take a cancellation token. Cancelling throws `OperationCanceledException` instead of returning a result.

### Timeouts

| Entry point | How to set the timeout | Default |
| --- | --- | --- |
| `godot_run_headless` | `timeoutSeconds` argument | 60 s, clamped to 1-600 |
| `godot_import_resources` | not configurable | 120 s |
| `HeadlessRunner` (.NET) | `DefaultTimeout` property | 2 min |

When a run times out, the server kills the whole process tree. The result has `TimedOut: true` and `ExitCode: -1`, and `Stdout`/`Stderr` contain the output captured before the kill. The `ExecutableConfig.DefaultTimeoutSeconds` value is not read by anything, so it does not change the timeout.

### Troubleshooting

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `godot_run_headless` fails with `godot binary not found; set GODOT_BIN or add godot to PATH`. `godot_binary_info` returns `path: null`. | No `godot` on `PATH` and `GODOT_BIN` unset, **or** the binary is found but not allowlisted. The second case is silent, so the message is misleading. | Run `which godot`. Set `GODOT_BIN` to that path and pass `--allowlist-executable=<same path>`. Check the server's stderr for `WARNING: No executable allowlist configured`. |
| Non-zero exit: `Succeeded: false`, `ExitCode` is not 0. No exception is thrown. | The script or scene failed, or it called `quit(n)` with `n != 0`. | Read `Stderr`, where Godot prints parse and runtime errors. Reproduce by hand: `"$GODOT_BIN" --headless --path /absolute/path/to/your/godot/project --script res://scripts/headless_check.gd`. The MCP wrapper takes project-relative paths, but the Godot CLI expects `res://`. |
| Timeout: `TimedOut: true`, `ExitCode: -1`. | The run went past the timeout, usually a loop that never calls `quit()`, or a scene that waits for input. | Make scripts call `quit()` when they finish. Raise `timeoutSeconds` (max 600). Check the partial `Stdout` to see how far it got. |
| `Scene file not found` (`FileNotFoundException`), or `Scene path must be project-relative` (`ArgumentException`). | The path is `res://...`, starts with `/`, contains `..`, or does not exist in the project. | Use a project-relative path such as `main.tscn`, or omit `scene` to run the main scene. |
| The server exits with code 1 at startup: `Allowlisted executable not found`. | The `--allowlist-executable` path does not exist. | Fix the path. It must be an existing absolute path. |

## How it works

`project.godot`, `.tscn` and `.tres` are parsed and written as plain text - no engine embedding, no editor plugin, no running Godot instance. The server shells out to the `godot` binary only for the three optional run tools; see [Headless execution](#headless-execution) for how it is located and allowlisted.

## Requirements

- .NET 10 SDK (`net10.0`)
- A Godot 4 project on disk
- `godot` binary: optional, only for headless run/import tools

## License

MIT
