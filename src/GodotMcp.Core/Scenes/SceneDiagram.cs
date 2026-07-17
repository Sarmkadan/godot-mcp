using System.Text;
using GodotMcp.Core.Model;

namespace GodotMcp.Core.Scenes;

public static class SceneDiagram
{
    public static string ToMermaid(SceneTree tree)
    {
        ArgumentNullException.ThrowIfNull(tree);
        var sb = new StringBuilder();
        sb.Append("graph TD\n");
        if (tree.Root is null) return sb.ToString();
        var ids = new Dictionary<NodeInfo, string>();
        var counter = 0;
        foreach (var node in Walk(tree.Root)) ids[node] = $"n{counter++}";
        foreach (var node in Walk(tree.Root))
        {
            var type = node.Type ?? (node.InstanceId is not null
                ? tree.FindExternalResource(node.InstanceId)?.Path is { } p ? $"instance of {System.IO.Path.GetFileName(p)}" : "instance"
                : "Node");
            sb.Append("    ").Append(ids[node]).Append("[\"").Append(EscapeLabel(node.Name)).Append("<br/><i>").Append(EscapeLabel(type)).Append("</i>\"]\n");
        }
        foreach (var node in Walk(tree.Root))
        {
            foreach (var child in node.Children)
                sb.Append("    ").Append(ids[node]).Append(" --> ").Append(ids[child]).Append('\n');
        }
        foreach (var connection in tree.Connections)
        {
            var from = ResolveByPath(tree, connection.From);
            var to = ResolveByPath(tree, connection.To);
            if (from is null || to is null || !ids.ContainsKey(from) || !ids.ContainsKey(to)) continue;
            sb.Append("    ").Append(ids[from]).Append(" -. ").Append(EscapeLabel($"{connection.Signal} → {connection.Method}")).Append(" .-> ").Append(ids[to]).Append('\n');
        }
        return sb.ToString();
    }

    public static string ToAsciiTree(SceneTree tree)
    {
        ArgumentNullException.ThrowIfNull(tree);
        var sb = new StringBuilder();
        if (tree.Root is null) return "";
        AppendAscii(sb, tree.Root, "", isLast: true, isRoot: true);
        return sb.ToString();
    }

    static void AppendAscii(StringBuilder sb, NodeInfo node, string indent, bool isLast, bool isRoot)
    {
        if (isRoot) sb.Append(node.Name);
        else
        {
            sb.Append(indent).Append(isLast ? "└── " : "├── ").Append(node.Name);
        }
        if (node.Type is not null) sb.Append(" (").Append(node.Type).Append(')');
        if (node.ScriptId is not null) sb.Append(" [script]");
        if (node.Groups.Count > 0) sb.Append(" {").Append(string.Join(", ", node.Groups)).Append('}');
        sb.Append('\n');
        for (var i = 0; i < node.Children.Count; i++)
        {
            var childIndent = isRoot ? "" : indent + (isLast ? "    " : "│   ");
            AppendAscii(sb, node.Children[i], childIndent, i == node.Children.Count - 1, isRoot: false);
        }
    }

    static IEnumerable<NodeInfo> Walk(NodeInfo root)
    {
        yield return root;
        foreach (var d in root.Descendants()) yield return d;
    }

    static NodeInfo? ResolveByPath(SceneTree tree, string path) => tree.FindNode(path);

    static string EscapeLabel(string text) => text.Replace("\"", "#quot;").Replace("[", "#91;").Replace("]", "#93;");
}
