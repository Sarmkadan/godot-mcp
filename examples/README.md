# Examples

## Try the server against the sample project

`sample-project/` is a minimal but valid Godot 4 project (a `project.godot` plus one scene). Point the server at it to poke around without touching a real game:

```json
{
  "mcpServers": {
    "godot": {
      "command": "godot-mcp",
      "args": ["/absolute/path/to/examples/sample-project"]
    }
  }
}
```

Drop that block into your MCP client config ([`mcp-config.json`](mcp-config.json) has the same thing for a real project), then ask the client things like:

- "show the scene tree of `res://main.tscn`"
- "add a `Camera2D` named `Cam` under `Player`"
- "set `Hud/Score` text to `100`"

Edits are written back as standard `.tscn` text, so `git diff` in the sample project shows exactly what changed.
