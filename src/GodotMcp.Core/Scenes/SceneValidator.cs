using GodotMcp.Core.Parsing;
using GodotMcp.Core.Project;

namespace GodotMcp.Core.Scenes;

public enum IssueSeverity { Error, Warning }

public sealed record SceneIssue(IssueSeverity Severity, string Code, string Message)
{
    public override string ToString() => $"[{Severity}] {Code}: {Message}";
}

public static class SceneValidator
{
    public static IReadOnlyList<SceneIssue> Validate(GodotProject project, string scenePath)
    {
        ArgumentNullException.ThrowIfNull(project);
        var document = project.LoadDocument(scenePath);
        var issues = new List<SceneIssue>();
        ValidateExtResources(project, document, issues);
        ValidateSubResources(document, issues);
        ValidateNodes(document, issues);
        ValidateConnections(project, document, issues);
        return issues;
    }

    static void ValidateExtResources(GodotProject project, TscnDocument document, List<SceneIssue> issues)
    {
        var referenced = CollectExtResourceReferences(document);
        foreach (var resource in document.ExtResources)
        {
            var id = resource.GetAttributeString("id") ?? "";
            var path = resource.GetAttributeString("path") ?? "";
            if (path.StartsWith("res://") && !File.Exists(project.ResolveResPath(path)))
                issues.Add(new SceneIssue(IssueSeverity.Error, "missing-file", $"ext_resource '{id}' points to missing file '{path}'"));
            if (!referenced.Contains(id))
                issues.Add(new SceneIssue(IssueSeverity.Warning, "unused-ext-resource", $"ext_resource '{id}' ('{path}') is never referenced"));
        }
        var seenIds = new HashSet<string>();
        foreach (var resource in document.ExtResources)
        {
            var id = resource.GetAttributeString("id") ?? "";
            if (!seenIds.Add(id))
                issues.Add(new SceneIssue(IssueSeverity.Error, "duplicate-resource-id", $"ext_resource id '{id}' is declared more than once"));
        }
    }

    static void ValidateSubResources(TscnDocument document, List<SceneIssue> issues)
    {
        var referenced = CollectSubResourceReferences(document);
        foreach (var resource in document.SubResources)
        {
            var id = resource.GetAttributeString("id") ?? "";
            if (!referenced.Contains(id))
                issues.Add(new SceneIssue(IssueSeverity.Warning, "orphan-sub-resource", $"sub_resource '{id}' ({resource.GetAttributeString("type")}) is never referenced"));
        }
        foreach (var id in referenced)
        {
            if (document.FindSubResource(id) is null)
                issues.Add(new SceneIssue(IssueSeverity.Error, "missing-sub-resource", $"SubResource(\"{id}\") is referenced but not declared"));
        }
    }

    static void ValidateNodes(TscnDocument document, List<SceneIssue> issues)
    {
        var paths = new HashSet<string>();
        foreach (var node in document.Nodes)
        {
            var path = NodePathOf(node);
            var name = node.GetAttributeString("name") ?? "";
            if (!paths.Add(path))
                issues.Add(new SceneIssue(IssueSeverity.Error, "duplicate-node-path", $"more than one node at path '{path}'"));
            if (node.GetAttributeString("type") is null && node.GetAttribute("instance") is null && node.GetAttributeString("parent") is not null && node.GetAttribute("index") is null)
                issues.Add(new SceneIssue(IssueSeverity.Error, "untyped-node", $"node '{path}' has neither a type nor an instance"));
            if (node.GetAttributeString("parent") is { } parent && parent != "." && !paths.Contains(parent) && FindByPath(document, parent) is null)
                issues.Add(new SceneIssue(IssueSeverity.Error, "missing-parent", $"node '{name}' declares parent '{parent}' which does not exist"));
            foreach (var (_, value) in node.Properties)
                CheckExtRefs(document, value, path, issues);
        }
    }

    static void ValidateConnections(GodotProject project, TscnDocument document, List<SceneIssue> issues)
    {
        foreach (var connection in document.Connections)
        {
            var signal = connection.GetAttributeString("signal") ?? "";
            foreach (var key in (string[])["from", "to"])
            {
                var endpoint = connection.GetAttributeString(key);
                if (endpoint is not null && FindByPath(document, endpoint) is null && !HasInstancedAncestor(document, endpoint))
                    issues.Add(new SceneIssue(IssueSeverity.Error, "dangling-connection", $"connection '{signal}' has {key}='{endpoint}' but no such node exists"));
            }
            var method = connection.GetAttributeString("method") ?? "";
            var target = connection.GetAttributeString("to") is { } to ? FindByPath(document, to) : null;
            if (target?.GetProperty("script") is GodotConstructor { IsExtResource: true } scriptRef &&
                scriptRef.ReferenceId is { } scriptId &&
                document.FindExtResource(scriptId)?.GetAttributeString("path") is { } scriptPath &&
                scriptPath.EndsWith(".gd") &&
                File.Exists(project.ResolveResPath(scriptPath)))
            {
                var text = File.ReadAllText(project.ResolveResPath(scriptPath));
                if (!text.Contains($"func {method}(") && !text.Contains($"func {method} ("))
                    issues.Add(new SceneIssue(IssueSeverity.Warning, "missing-handler", $"connection '{signal}' targets method '{method}' which is not defined in '{scriptPath}'"));
            }
        }
    }

    internal static HashSet<string> CollectExtResourceReferences(TscnDocument document)
    {
        var ids = new HashSet<string>();
        foreach (var section in document.Sections)
        {
            if (section.Name == "ext_resource") continue;
            foreach (var (_, value) in section.Properties) Collect(value, "ExtResource", ids);
            foreach (var (_, value) in section.Attributes) Collect(value, "ExtResource", ids);
        }
        return ids;
    }

    internal static HashSet<string> CollectSubResourceReferences(TscnDocument document)
    {
        var ids = new HashSet<string>();
        foreach (var section in document.Sections)
        {
            foreach (var (_, value) in section.Properties) Collect(value, "SubResource", ids);
            foreach (var (_, value) in section.Attributes) Collect(value, "SubResource", ids);
        }
        return ids;
    }

    static void Collect(GodotValue value, string constructorName, HashSet<string> ids)
    {
        switch (value)
        {
            case GodotConstructor c when c.Name == constructorName && c.ReferenceId is { } id:
                ids.Add(id);
                break;
            case GodotConstructor c:
                foreach (var arg in c.Arguments) Collect(arg, constructorName, ids);
                break;
            case GodotArray a:
                foreach (var item in a.Items) Collect(item, constructorName, ids);
                break;
            case GodotDictionary d:
                foreach (var (k, v) in d.Entries)
                {
                    Collect(k, constructorName, ids);
                    Collect(v, constructorName, ids);
                }
                break;
        }
    }

    static void CheckExtRefs(TscnDocument document, GodotValue value, string nodePath, List<SceneIssue> issues)
    {
        var ids = new HashSet<string>();
        Collect(value, "ExtResource", ids);
        foreach (var id in ids)
        {
            if (document.FindExtResource(id) is null)
                issues.Add(new SceneIssue(IssueSeverity.Error, "missing-ext-resource", $"node '{nodePath}' references ExtResource(\"{id}\") which is not declared"));
        }
    }

    static bool HasInstancedAncestor(TscnDocument document, string path)
    {
        // Connections may target children that live inside an instanced scene and
        // therefore have no node section of their own in this file.
        var current = path;
        while (true)
        {
            var slash = current.LastIndexOf('/');
            current = slash < 0 ? "." : current[..slash];
            if (FindByPath(document, current) is { } ancestor && ancestor.GetAttribute("instance") is not null) return true;
            if (current == ".") return false;
        }
    }

    static string NodePathOf(TscnSection node)
    {
        var name = node.GetAttributeString("name") ?? "";
        return node.GetAttributeString("parent") switch
        {
            null => ".",
            "." => name,
            { } parent => $"{parent}/{name}"
        };
    }

    static TscnSection? FindByPath(TscnDocument document, string path) => SceneTreeBuilder.FindNodeSection(document, path);
}
