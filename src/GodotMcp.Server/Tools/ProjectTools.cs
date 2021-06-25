using System.ComponentModel;
using ModelContextProtocol.Server;

namespace GodotMcp.Server.Tools;

[McpServerToolType]
public sealed class ProjectTools(ProjectLocator locator)
{
    [McpServerTool(Name = "godot_project_info"), Description("Read project.godot and return project metadata: name, main scene, features, autoloads, engine version.")]
    public ProjectInfoResult ProjectInfo([Description("Godot project root; omit for the configured default")] string? projectPath = null) =>
        ProjectInfoResult.From(locator.Resolve(projectPath).LoadInfo());

    [McpServerTool(Name = "godot_list_scenes"), Description("List all .tscn scene files in the project as res:// paths.")]
    public IReadOnlyList<string> ListScenes(string? projectPath = null) =>
        locator.Resolve(projectPath).FindScenes().Order().ToList();

    [McpServerTool(Name = "godot_list_resources"), Description("List resource files (.tres, .res, .gdshader, .material) as res:// paths.")]
    public IReadOnlyList<string> ListResources(string? projectPath = null) =>
        locator.Resolve(projectPath).FindResources().Order().ToList();

    [McpServerTool(Name = "godot_list_scripts"), Description("List script files (.cs, .gd) as res:// paths.")]
    public IReadOnlyList<string> ListScripts(string? projectPath = null) =>
        locator.Resolve(projectPath).FindScripts().Order().ToList();

    [McpServerTool(Name = "godot_inspect_script"), Description("Inspect a script file and return its language, class name and base type.")]
    public ScriptResult InspectScript([Description("Script path as res:// or project-relative")] string scriptPath, string? projectPath = null)
    {
        var info = locator.Resolve(projectPath).InspectScript(scriptPath);
        return new ScriptResult(info.Path, info.Language, info.ClassName, info.BaseType);
    }
}
