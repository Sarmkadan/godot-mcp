# CLAUDE.md

MCP server (.NET 10, C#) that reads and edits Godot 4 projects by parsing `project.godot`, `.tscn`, `.tres` text files directly - no Godot install or engine assemblies required.

## Build

```sh
dotnet build                      # solution: GodotMcp.slnx
dotnet build -c Release
dotnet pack src/GodotMcp.Server -c Release -o artifacts   # dotnet tool "godot-mcp", PackageId GodotMcp
dotnet run --project src/GodotMcp.Server -- /path/to/godot/project
```

Project path resolution order: first CLI arg -> `GODOT_PROJECT` env var -> current directory. Every tool also accepts an explicit `projectPath` parameter.

## Test

```sh
dotnet test                                   # all tests (xunit 2.9)
dotnet test --filter "FullyQualifiedName~SceneEditorTests"
dotnet test -c Release --no-build             # what CI runs after build
```

CI: `.github/workflows/ci.yml` - build, test, pack on push to master/main and PRs.

## Lint / format

No analyzers, `.editorconfig` or format step configured. `Nullable` and `ImplicitUsings` are enabled, `LangVersion` latest - keep code warning-free under nullable.

## Layout

```
GodotMcp.slnx                 solution (src/ + tests/ folders)
src/GodotMcp.Core/            library, no MCP dependency
  Parsing/    TscnParser, TscnDocument, GodotValue, GodotValueReader - tokenizer/parser for Godot resource text
  Model/      SceneTree, NodeInfo, NodeProperty, ResourceRef, ProjectInfo, GodotVersion - immutable data
  Scenes/     SceneTreeBuilder (document -> tree), SceneEditor (mutations + Save), SceneQuery,
              SceneValidator, SceneLinter, SceneDiagram (Mermaid/ASCII)
  Project/    GodotProject (root, LoadScene, file enumeration), PathResolver (res:// <-> disk), ProjectAnalyzer
  Editor/     GodotExecutable, ExecutableAllowlist, HeadlessRunner - locating and running the godot binary
src/GodotMcp.Server/          console host, stdio MCP transport
  Program.cs                  entry point: ExecutableConfig -> allowlist init -> Host + AddMcpServer().WithToolsFromAssembly()
  ProjectLocator.cs           resolves projectPath -> GodotProject per call
  Contracts.cs                result DTOs (records) returned by tools
  Tools/                      ProjectTools, SceneTools, ResourceTools, RunTools, AnalysisTools
tests/GodotMcp.Tests/         xunit tests, one file per Core/Server class
docs/                         SceneQuery.md, NodeMatch.md, AnalysisTools.md
examples/sample-project/      minimal Godot project used in tests/docs
```

Stray files not part of the build: `src/GodotMcp.Tests/ProjectToolsTests.cs` (not in solution), `test_path_resolver.cs`, `src/GodotMcp.Core/Scenes/SceneEditor.cs.bak`, `aider_buildcmd.py`. Do not treat them as source of truth.

## Conventions

- Tools: `[McpServerToolType]` sealed class with primary constructor `(ProjectLocator locator)`; each method `[McpServerTool(Name = "godot_<verb>_<thing>")]` plus `[Description]` on method and non-obvious parameters. Tool names are snake_case with `godot_` prefix; keep README tool table in sync.
- Tool method pattern: `locator.Resolve(projectPath).LoadScene(scenePath)` for reads; `new SceneEditor(project, scenePath)` -> mutate -> `Save()` for writes, returning `MutationResult(true, scenePath, message)`.
- Return types are `record` DTOs from `Contracts.cs` (`NodeResult.From(...)`), never Core model types directly.
- Scene paths accepted as `res://...` or project-relative; node paths use `/` with `.` for root.
- Property values use Godot text syntax (`Vector2(10, 20)`, `ExtResource("1_icon")`) parsed via `GodotValueReader`.
- Core must stay free of MCP/hosting dependencies; Server references Core, tests reference both.
- Editing `.tscn` must preserve unknown constructs and round-trip untouched content (see `RoundTripPreservationTests`, `UnknownConstructsTests`).
- Executable runs go through `ExecutableAllowlist`; server fails closed when no allowlist is configured.
- Tests: class per subject, inline `const string Scene = """..."""` fixtures, temp dir per test via `IDisposable`, `[Fact]`/`[Theory]`.
- Logging goes to stderr only (stdout is the MCP transport).
- Commit messages: conventional commits (`feat(SceneEditor): ...`, `docs: ...`, `chore: ...`).
