using System.ComponentModel;
using GodotMcp.Core.Parsing;
using ModelContextProtocol.Server;

namespace GodotMcp.Server.Tools;

[McpServerToolType]
public sealed class ResourceTools(ProjectLocator locator)
{
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
