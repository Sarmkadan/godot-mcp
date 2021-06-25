using GodotMcp.Core.Model;
using GodotMcp.Core.Parsing;

namespace GodotMcp.Core.Scenes;

public static class SceneTreeBuilder
{
    static readonly HashSet<string> HeaderKeys = ["name", "type", "parent", "instance", "instance_placeholder", "owner", "index", "groups", "node_paths"];

    public static SceneTree Build(TscnDocument document, string scenePath)
    {
        var externals = document.ExtResources
            .Select(s => new ResourceRef(
                s.GetAttributeString("id") ?? "",
                s.GetAttributeString("type") ?? "",
                s.GetAttributeString("path") ?? "",
                s.GetAttributeString("uid")))
            .ToList();
        var subs = document.SubResources
            .Select(s => new SubResourceInfo(
                s.GetAttributeString("id") ?? "",
                s.GetAttributeString("type") ?? "",
                s.Properties.Select(p => new NodeProperty(p.Key, p.Value)).ToList()))
            .ToList();
        NodeInfo? root = null;
        var byPath = new Dictionary<string, NodeInfo>();
        foreach (var section in document.Nodes)
        {
            var node = BuildNode(section);
            if (node.Parent is null)
            {
                root = node;
                byPath["."] = node;
                continue;
            }
            byPath[node.Path] = node;
            if (byPath.TryGetValue(node.Parent, out var parent)) parent.Children.Add(node);
            else root?.FindByPath(node.Parent)?.Children.Add(node);
        }
        var connections = document.Connections
            .Select(s => new SignalConnection(
                s.GetAttributeString("signal") ?? "",
                s.GetAttributeString("from") ?? "",
                s.GetAttributeString("to") ?? "",
                s.GetAttributeString("method") ?? "",
                s.GetAttribute("binds") is GodotArray a ? a.Items.Select(v => v.ToTscnString()).ToList() : []))
            .ToList();
        return new SceneTree
        {
            ScenePath = scenePath,
            Uid = document.Uid,
            Format = document.Format,
            Root = root,
            ExternalResources = externals,
            SubResources = subs,
            Connections = connections
        };
    }

    static NodeInfo BuildNode(TscnSection section)
    {
        var groups = section.GetAttribute("groups") is GodotArray g
            ? g.Items.OfType<GodotString>().Select(s => s.Value).ToList()
            : (IReadOnlyList<string>)[];
        var node = new NodeInfo
        {
            Name = section.GetAttributeString("name") ?? "",
            Type = section.GetAttributeString("type"),
            Parent = section.GetAttributeString("parent"),
            InstanceId = section.GetAttribute("instance") is GodotConstructor { IsExtResource: true } inst ? inst.ReferenceId : null,
            ScriptId = section.GetProperty("script") is GodotConstructor { IsExtResource: true } script ? script.ReferenceId : null,
            Groups = groups
        };
        foreach (var (key, value) in section.Properties)
            node.Properties.Add(new NodeProperty(key, value));
        return node;
    }

    public static TscnSection? FindNodeSection(TscnDocument document, string nodePath)
    {
        foreach (var section in document.Nodes)
        {
            var parent = section.GetAttributeString("parent");
            var name = section.GetAttributeString("name") ?? "";
            var path = parent switch
            {
                null => ".",
                "." => name,
                _ => $"{parent}/{name}"
            };
            if (path == nodePath) return section;
        }
        return null;
    }

    public static bool IsHeaderKey(string key) => HeaderKeys.Contains(key);
}
