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
| `godot_set_node_property` | Set a node property using Godot text syntax (`Vector2(10, 20)`, `true`, ...) |
| `godot_get_node_property` | Get a single node property value in Godot text syntax |
| `godot_read_resource` | Read a `.tres`/`.tscn` file as raw text |
| `godot_write_resource` | Write a `.tres`/`.tscn` file, validated as Godot resource text before saving |
| `godot_resource_summary` | Structure of a resource file: descriptor, sections, external references |
| `godot_binary_info` | Locate the installed `godot` binary and report its version |
| `godot_run_headless` | Run the project (or a scene) headlessly, return exit code and output |
| `godot_import_resources` | Run `godot --headless --import` to (re)import project resources |

## How it works

`project.godot`, `.tscn` and `.tres` are parsed and written as plain text - no engine embedding, no editor plugin, no running Godot instance. The server shells out to the `godot` binary only for the three optional run tools; set `GODOT_BIN` if it is not on `PATH`.

## Requirements

- .NET 10 SDK (`net10.0`)
- A Godot 4 project on disk
- `godot` binary: optional, only for headless run/import tools

## License

MIT
