using System.ComponentModel;
using GodotMcp.Core.Parsing;
using GodotMcp.Core.Project;
using GodotMcp.Server;
using ModelContextProtocol.Server;

namespace GodotMcp.Server.Tools;

/// <summary>
/// Tools for managing project resources, including moving and renaming with reference updates.
/// </summary>
[McpServerToolType]
public sealed class ResourceTools(ProjectLocator locator)
{
    /// <summary>
    /// Moves or renames a scene, script, or resource file and updates all references across the project.
    /// </summary>
    /// <param name="projectPath">Path to the Godot project root.</param>
    /// <param name="oldPath">Current res:// path of the resource.</param>
    /// <param name="newPath">New res:// path for the resource.</param>
    /// <returns>Result containing success status, new path, and message.</returns>
    /// <exception cref="ArgumentException">Thrown when oldPath or newPath is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the resource at oldPath does not exist.</exception>
    [McpServerTool(Name = "godot_move_resource")]
    [Description("Moves or renames a scene, script, or resource file and updates all references across the project.")]
    public async Task<MutationResult> MoveResourceAsync(
        string? projectPath,
        string oldPath,
        string newPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(oldPath);
        ArgumentException.ThrowIfNullOrEmpty(newPath);

        var project = locator.Resolve(projectPath);
        var projectRoot = Path.GetFullPath(projectPath ?? ".");
        
        var oldAbsolute = project.ResolveResPath(oldPath);
        var newAbsolute = project.ResolveResPath(newPath);

        if (!File.Exists(oldAbsolute))
        {
            throw new FileNotFoundException($"Resource not found: {oldPath}", oldPath);
        }

        var touchedFiles = new List<string>();

        foreach (var file in Directory.EnumerateFiles(projectRoot, "*", SearchOption.AllDirectories))
        {
            if (!file.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase) &&
                !file.EndsWith(".tres", StringComparison.OrdinalIgnoreCase) &&
                !file.EndsWith("project.godot", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var content = await File.ReadAllTextAsync(file);
            var updatedContent = content.Replace(oldPath, newPath);

            if (updatedContent != content)
            {
                await File.WriteAllTextAsync(file, updatedContent);
                touchedFiles.Add(project.ToResPath(file));
            }
        }

        var newDirectory = Path.GetDirectoryName(newAbsolute);
        if (!string.IsNullOrEmpty(newDirectory) && !Directory.Exists(newDirectory))
        {
            Directory.CreateDirectory(newDirectory);
        }
        
        File.Move(oldAbsolute, newAbsolute, overwrite: true);

        return new MutationResult(true, newPath, $"Moved resource to {newPath}, updated {touchedFiles.Count} file(s).");
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
