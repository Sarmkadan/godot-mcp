# Contributing to GodotMcp

Thanks for helping out. This is a .NET MCP server that reads and edits Godot 4 projects by parsing their text files directly. See [README.md](README.md) for what it does and [CLAUDE.md](CLAUDE.md) for the layout of the code.

## Prerequisites

- **.NET 10 SDK** (`net10.0`). The repo has no `global.json`; CI installs `10.0.x` via `actions/setup-dotnet`.
- **Godot 4** is optional. Building and the unit tests do not need it. Only `godot_run_headless`, `godot_import_resources`, `godot_binary_info` and the `HeadlessRunnerIntegrationTests` use a real `godot` binary. Those tests are skipped unless Godot is on `PATH` or set via `GODOT_BIN` / `GODOT`. No specific Godot version is pinned; the sample project uses the Godot 4 file format (`config_version=5`).

## Building

```sh
dotnet build GodotMcp.slnx               # Debug
dotnet build GodotMcp.slnx -c Release    # Release, as CI runs it
```

## Running tests

```sh
dotnet test GodotMcp.slnx                          # all tests (xunit)
dotnet test --filter "FullyQualifiedName~SceneEditorTests"
dotnet test GodotMcp.slnx -c Release --no-build    # what CI runs after build
```

Add tests for behaviour you change. Parser and editor changes should keep the round-trip tests (`RoundTripPreservationTests`, `UnknownConstructsTests`) passing.

## Coding conventions

- **Nullable reference types are enabled** in all projects. Keep new code free of nullable warnings.
- **Warnings:** keep the build warning-free. `TreatWarningsAsErrors` is *not* set in this repo, so CI will not fail on a warning by itself. Fix warnings in code you touch anyway.
- **Formatting:** there is no `.editorconfig` and no format step. Match the style of the surrounding code.
- **Tools** live in `src/GodotMcp.Server/Tools/` as `[McpServerToolType]` classes. Tool names are snake_case with the `godot_` prefix. If you add or rename a tool, update the tool table in `README.md`.
- **Return types** are `record` DTOs from `Contracts.cs`, not Core model types.
- **Core stays free of MCP/hosting dependencies.** Only `GodotMcp.Server` references the MCP SDK.
- **Logging** goes to stderr only. stdout is the MCP transport.
- **Commit messages** use Conventional Commits, e.g. `feat(SceneEditor): ...`, `fix: ...`, `docs: ...`, `chore: ...`.

## Branching and pull request process

1. Branch from `master` (the default branch). Use a short descriptive name, e.g. `feat/resource-move-tool`.
2. Keep each PR small and focused on one change. Split refactors from behaviour changes.
3. In the PR description, explain what changed and why, and how you tested it. Mention any tool or README table updates.
4. CI (`.github/workflows/ci.yml`) runs build, test and pack on pushes and pull requests to `master`/`main`. It must pass before merge.

## Reporting bugs

The repo has no issue templates yet. Open an issue at <https://github.com/Sarmkadan/godot-mcp/issues> and include:

- the GodotMcp version or commit,
- your .NET SDK and OS,
- the MCP client you used,
- the tool name, its arguments, and the output or error,
- a minimal Godot project or `.tscn` snippet that reproduces the problem, if you can share one.
