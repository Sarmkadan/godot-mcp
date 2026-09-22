using System.ComponentModel;
using GodotMcp.Core.Parsing;
using GodotMcp.Core.Project;
using ModelContextProtocol.Server;

namespace GodotMcp.Server.Tools;

[McpServerToolType]
public sealed class ResourceTools(ProjectLocator locator)
{
    /// <summary>Moves a resource and updates project-wide references to its path.</summary>
    /// <param name="oldPath">The existing resource path.</param>
    /// <param name="newPath">The destination resource path.</param>
    /// <param name="projectPath">An optional project root override.</param>
    /// <returns>The move details and list of files changed.</returns>
    /// <exception cref="ArgumentException">Thrown when either resource path is empty or invalid.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the source resource does not exist.</exception>
    /// <exception cref="IOException">Thrown when the destination already exists or the move cannot be completed.</exception>
    [McpServerTool(Name = "godot_move_resource"), Description("Move or rename a scene, script, or other resource and update its references in all .tscn/.tres files and project.godot. Stable UID references are preserved.")]
    public MoveResourceResult MoveResource(
        [Description("Existing resource path as res:// or project-relative")] string oldPath,
        [Description("Destination resource path as res:// or project-relative")] string newPath,
        string? projectPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(oldPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPath);

        var result = ResourceMover.Move(locator.Resolve(projectPath), oldPath, newPath);
        return new MoveResourceResult(result.OldPath, result.NewPath, result.TouchedFiles);
    }

    [McpServerTool(Name = "godot_read_resource"), Description("Read a .tres/.tscn file and return its raw text content.")]
    public string ReadResource([Description("Resource path as res:// or project-relative")] string resourcePath, string? projectPath = null) =>
        File.ReadAllText(locator.Resolve(projectPath).ResolveResPath(resourcePath));

    [McpServerTool(Name = "godot_write_resource"), Description("Write text content to a .tres/.tscn file; the content is validated as Godot resource text before saving.")]
    public MutationResult WriteResource(string resourcePath, [Description("Full file content in Godot text-resource format")] string content, string? projectPath = null)
    {
        var project = locator.Resolve(projectPath);
        var document = TscnParser.Parse(content);
        var absolute = project.ResolveResPath(resourcePath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
        File.WriteAllText(absolute, content);
        return new MutationResult(true, resourcePath, $"Wrote {document.Sections.Count + 1} sections ({document.Descriptor.Name})");
    }

    [McpServerTool(Name = "godot_resource_summary"), Description("Parse a .tres/.tscn file and return its structure: descriptor, section names and counts, external references.")]
    public Dictionary<string, object?> ResourceSummary(string resourcePath, string? projectPath = null)
    {
        var document = locator.Resolve(projectPath).LoadDocument(resourcePath);
        return new Dictionary<string, object?>
        {
            ["kind"] = document.Descriptor.Name,
            ["uid"] = document.Uid,
            ["format"] = document.Format,
            ["type"] = document.Descriptor.GetAttributeString("type"),
            ["sections"] = document.Sections.GroupBy(s => s.Name).ToDictionary(g => g.Key, g => g.Count()),
            ["externalResources"] = document.ExtResources
                .Select(s => new ExternalResourceResult(
                    s.GetAttributeString("id") ?? "",
                    s.GetAttributeString("type") ?? "",
                    s.GetAttributeString("path") ?? "",
                    s.GetAttributeString("uid")))
                .ToList()
        };
    }
}
