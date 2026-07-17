using System.Text.RegularExpressions;
using GodotMcp.Core.Model;
using GodotMcp.Core.Parsing;
using GodotMcp.Core.Scenes;

namespace GodotMcp.Core.Project;

public sealed record NodeMatch(string ScenePath, string NodePath, string Name, string? Type);

public sealed record ScriptUsage(string ScenePath, string NodePath, string NodeName);

public sealed partial class ProjectAnalyzer(GodotProject project)
{
    public GodotProject Project { get; } = project ?? throw new ArgumentNullException(nameof(project));

    /// <summary>Find nodes across every scene in the project, by exact type name or wildcard name pattern.</summary>
    public IReadOnlyList<NodeMatch> FindNodes(string? type = null, string? namePattern = null, string? group = null)
    {
        var matches = new List<NodeMatch>();
        foreach (var scenePath in Project.FindScenes())
        {
            SceneTree tree;
            try { tree = Project.LoadScene(scenePath); }
            catch (GodotParseException) { continue; }
            foreach (var node in SceneQuery.AllNodes(tree))
            {
                if (type is not null && !string.Equals(node.Type, type, StringComparison.OrdinalIgnoreCase)) continue;
                if (namePattern is not null && !SceneQuery.MatchesWildcard(node.Name, namePattern)) continue;
                if (group is not null && !node.Groups.Contains(group, StringComparer.Ordinal)) continue;
                matches.Add(new NodeMatch(scenePath, node.Path, node.Name, node.Type));
            }
        }
        return matches;
    }

    /// <summary>Find every scene node that has the given script attached.</summary>
    public IReadOnlyList<ScriptUsage> FindScriptUsages(string scriptResPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptResPath);
        var usages = new List<ScriptUsage>();
        foreach (var scenePath in Project.FindScenes())
        {
            SceneTree tree;
            try { tree = Project.LoadScene(scenePath); }
            catch (GodotParseException) { continue; }
            var scriptIds = tree.ExternalResources
                .Where(r => r.Path == scriptResPath)
                .Select(r => r.Id)
                .ToHashSet();
            if (scriptIds.Count == 0) continue;
            foreach (var node in SceneQuery.AllNodes(tree))
            {
                if (node.ScriptId is { } id && scriptIds.Contains(id))
                    usages.Add(new ScriptUsage(scenePath, node.Path, node.Name));
            }
        }
        return usages;
    }

    /// <summary>
    /// Resources (.tres, .tscn, shaders, scripts) not referenced by any scene, resource,
    /// script text, or project.godot. Dynamic loads are detected by scanning script
    /// sources for res:// string literals, so a reported orphan is a strong candidate
    /// for deletion but not a guarantee.
    /// </summary>
    public IReadOnlyList<string> FindOrphanResources()
    {
        var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var info = Project.LoadInfo();
        if (info.MainScene is { } main) referenced.Add(Normalize(main));
        foreach (var section in LoadProjectSections())
        {
            foreach (var (_, value) in section.Properties)
                foreach (var path in ExtractResPaths(value.ToTscnString())) referenced.Add(path);
        }
        foreach (var textResource in Project.FindScenes().Concat(Project.FindResources().Where(IsTextResource)))
        {
            TscnDocument document;
            try { document = Project.LoadDocument(textResource); }
            catch (GodotParseException) { continue; }
            foreach (var ext in document.ExtResources)
            {
                if (ext.GetAttributeString("path") is { } path) referenced.Add(Normalize(path));
            }
        }
        foreach (var script in Project.FindScripts())
        {
            var absolute = Project.ResolveResPath(script);
            if (!File.Exists(absolute)) continue;
            foreach (var path in ExtractResPaths(File.ReadAllText(absolute))) referenced.Add(path);
        }
        var candidates = Project.FindScenes()
            .Concat(Project.FindResources())
            .Concat(Project.FindScripts());
        return candidates
            .Where(c => !referenced.Contains(Normalize(c)))
            .Where(c => Normalize(c) != Normalize(info.MainScene ?? ""))
            .Order()
            .ToList();
    }

    /// <summary>Which scenes and resources (transitively non-recursive: direct references only) a scene depends on.</summary>
    public IReadOnlyList<ResourceRef> GetSceneDependencies(string scenePath) =>
        Project.LoadScene(scenePath).ExternalResources.ToList();

    IEnumerable<TscnSection> LoadProjectSections()
    {
        if (!Project.Exists) yield break;
        TscnDocument document;
        try { document = TscnParser.Parse("[gd_project]\n" + File.ReadAllText(Project.ProjectFilePath)); }
        catch (GodotParseException) { yield break; }
        yield return document.Descriptor;
        foreach (var section in document.Sections) yield return section;
    }

    static IEnumerable<string> ExtractResPaths(string text)
    {
        foreach (Match match in ResPathRegex().Matches(text))
            yield return match.Value.TrimEnd('.');
    }

    static bool IsTextResource(string resPath) =>
        Path.GetExtension(resPath) is ".tres" or ".tscn";

    static string Normalize(string resPath) => resPath.Trim();

    [GeneratedRegex(@"res://[\w\-./ ]+\.\w+")]
    private static partial Regex ResPathRegex();
}
