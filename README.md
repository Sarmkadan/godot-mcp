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

## How it works

`project.godot`, `.tscn` and `.tres` are parsed and written as plain text - no engine embedding, no editor plugin, no running Godot instance. The server shells out to the `godot` binary only for the three optional run tools; set `GODOT_BIN` if it is not on `PATH`.

## Requirements

- .NET 10 SDK (`net10.0`)
- A Godot 4 project on disk
- `godot` binary: optional, only for headless run/import tools

## License

MIT
