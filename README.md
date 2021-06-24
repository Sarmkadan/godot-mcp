# godot-mcp

MCP server for Godot 4 projects: inspect scene trees, create nodes, get/set properties, read/write `.tscn`/`.tres` files, and run the project headlessly. Works directly on the project files on disk, so the Godot editor does not need to be running (or installed, except for the run tools).

```sh
dotnet run --project src/GodotMcp.Server -- /path/to/godot/project
```

Register the command above as a stdio MCP server in your client. Set `GODOT_BIN` if the `godot` binary is not on `PATH`.
