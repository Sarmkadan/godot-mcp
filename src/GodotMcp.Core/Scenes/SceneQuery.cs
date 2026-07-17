using GodotMcp.Core.Model;

namespace GodotMcp.Core.Scenes;

public static class SceneQuery
{
    public static IEnumerable<NodeInfo> AllNodes(SceneTree tree)
    {
        if (tree.Root is null) yield break;
        yield return tree.Root;
        foreach (var node in tree.Root.Descendants()) yield return node;
    }

    public static IEnumerable<NodeInfo> FindByType(SceneTree tree, string type, bool exact = true) =>
        AllNodes(tree).Where(n => n.Type is { } t && (exact
            ? string.Equals(t, type, StringComparison.OrdinalIgnoreCase)
            : t.Contains(type, StringComparison.OrdinalIgnoreCase)));

    public static IEnumerable<NodeInfo> FindByName(SceneTree tree, string pattern) =>
        AllNodes(tree).Where(n => MatchesWildcard(n.Name, pattern));

    public static IEnumerable<NodeInfo> FindInGroup(SceneTree tree, string group) =>
        AllNodes(tree).Where(n => n.Groups.Contains(group, StringComparer.Ordinal));

    public static IEnumerable<NodeInfo> FindWithScript(SceneTree tree, string extResourceId) =>
        AllNodes(tree).Where(n => n.ScriptId == extResourceId);

    public static bool MatchesWildcard(string text, string pattern)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentException.ThrowIfNullOrEmpty(pattern);
        return MatchesWildcard(text.AsSpan(), pattern.AsSpan());
    }

    static bool MatchesWildcard(ReadOnlySpan<char> text, ReadOnlySpan<char> pattern)
    {
        while (true)
        {
            if (pattern.IsEmpty) return text.IsEmpty;
            if (pattern[0] == '*')
            {
                pattern = pattern[1..];
                if (pattern.IsEmpty) return true;
                for (var i = 0; i <= text.Length; i++)
                    if (MatchesWildcard(text[i..], pattern)) return true;
                return false;
            }
            if (text.IsEmpty) return false;
            if (pattern[0] != '?' && char.ToUpperInvariant(text[0]) != char.ToUpperInvariant(pattern[0])) return false;
            text = text[1..];
            pattern = pattern[1..];
        }
    }
}
