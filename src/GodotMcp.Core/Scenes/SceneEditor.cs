using GodotMcp.Core.Parsing;
using GodotMcp.Core.Project;

namespace GodotMcp.Core.Scenes;

public sealed class SceneEditor
{
    public SceneEditor(GodotProject project, string scenePath)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenePath);
        Project = project;
        ScenePath = scenePath;
        Document = project.LoadDocument(scenePath);
    }

    public GodotProject Project { get; }
    public string ScenePath { get; }
    public TscnDocument Document { get; }

    public TscnSection AddNode(string name, string type, string parentPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(parentPath);
        if (SceneTreeBuilder.FindNodeSection(Document, parentPath) is null && parentPath != ".")
            throw new InvalidOperationException($"Parent node '{parentPath}' not found in {ScenePath}");
        var section = new TscnSection("node");
        section.SetAttribute("name", new GodotString(name));
        section.SetAttribute("type", new GodotString(type));
        section.SetAttribute("parent", new GodotString(parentPath));
        var insertAfter = Document.Sections.FindLastIndex(s => s.Name == "node");
        if (insertAfter >= 0) Document.Sections.Insert(insertAfter + 1, section);
        else Document.Sections.Add(section);
        return section;
    }

    public bool RemoveNode(string nodePath)
    {
        var section = SceneTreeBuilder.FindNodeSection(Document, nodePath);
        if (section is null) return false;
        var prefix = nodePath + "/";
        Document.Sections.RemoveAll(s =>
            s.Name == "node" &&
            s.GetAttributeString("parent") is { } parent &&
            (parent == nodePath || parent.StartsWith(prefix)));
        Document.Sections.Remove(section);
        return true;
    }

    public void SetNodeProperty(string nodePath, string property, GodotValue value)
    {
        var section = SceneTreeBuilder.FindNodeSection(Document, nodePath)
            ?? throw new InvalidOperationException($"Node '{nodePath}' not found in {ScenePath}");
        section.SetProperty(property, value);
    }

    public bool RemoveNodeProperty(string nodePath, string property) =>
        SceneTreeBuilder.FindNodeSection(Document, nodePath)?.RemoveProperty(property) ?? false;

    public GodotValue? GetNodeProperty(string nodePath, string property) =>
        SceneTreeBuilder.FindNodeSection(Document, nodePath)?.GetProperty(property);

    public void RenameNode(string nodePath, string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);
        if (newName.Contains('/') || newName.Contains(':') || newName.Contains('@'))
            throw new ArgumentException($"'{newName}' is not a valid node name", nameof(newName));
        var section = SceneTreeBuilder.FindNodeSection(Document, nodePath)
            ?? throw new InvalidOperationException($"Node '{nodePath}' not found in {ScenePath}");
        var isRoot = section.GetAttributeString("parent") is null;
        var newPath = isRoot
            ? "."
            : nodePath.Contains('/') ? nodePath[..(nodePath.LastIndexOf('/') + 1)] + newName : newName;
        if (!isRoot && SceneTreeBuilder.FindNodeSection(Document, newPath) is not null)
            throw new InvalidOperationException($"A node already exists at '{newPath}' in {ScenePath}");
        section.SetAttribute("name", new GodotString(newName));
        if (isRoot) return;
        var oldPrefix = nodePath + "/";
        var newPrefix = newPath + "/";
        string Rewrite(string path) =>
            path == nodePath ? newPath : path.StartsWith(oldPrefix) ? newPrefix + path[oldPrefix.Length..] : path;
        foreach (var node in Document.Nodes)
        {
            if (node.GetAttributeString("parent") is { } parent && Rewrite(parent) is var rewritten && rewritten != parent)
                node.SetAttribute("parent", new GodotString(rewritten));
        }
        foreach (var connection in Document.Connections)
        {
            foreach (var key in (string[])["from", "to"])
            {
                var value = connection.GetAttribute(key) switch
                {
                    GodotString s => s.Value,
                    GodotNodePath p => p.Value,
                    _ => null
                };
                if (value is not null && Rewrite(value) is var updated && updated != value)
                    connection.SetAttribute(key, new GodotString(updated));
            }
        }
    }

    public TscnSection ConnectSignal(string signal, string fromPath, string toPath, string method)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signal);
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        if (SceneTreeBuilder.FindNodeSection(Document, fromPath) is null)
            throw new InvalidOperationException($"Node '{fromPath}' not found in {ScenePath}");
        if (SceneTreeBuilder.FindNodeSection(Document, toPath) is null)
            throw new InvalidOperationException($"Node '{toPath}' not found in {ScenePath}");
        var existing = FindConnection(signal, fromPath, toPath, method);
        if (existing is not null) return existing;
        var section = new TscnSection("connection");
        section.SetAttribute("signal", new GodotString(signal));
        section.SetAttribute("from", new GodotString(fromPath));
        section.SetAttribute("to", new GodotString(toPath));
        section.SetAttribute("method", new GodotString(method));
        Document.Sections.Add(section);
        return section;
    }

    public bool DisconnectSignal(string signal, string fromPath, string toPath, string method)
    {
        var section = FindConnection(signal, fromPath, toPath, method);
        if (section is null) return false;
        Document.Sections.Remove(section);
        return true;
    }

    TscnSection? FindConnection(string signal, string fromPath, string toPath, string method) =>
        Document.Connections.FirstOrDefault(c =>
            c.GetAttributeString("signal") == signal &&
            c.GetAttributeString("from") == fromPath &&
            c.GetAttributeString("to") == toPath &&
            c.GetAttributeString("method") == method);

    public string AddExtResource(string type, string resPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(resPath);
        var existing = Document.ExtResources.FirstOrDefault(s => s.GetAttributeString("path") == resPath);
        if (existing is not null) return existing.GetAttributeString("id") ?? "";
        var index = Document.ExtResources.Count() + 1;
        var id = $"{index}_{Path.GetFileNameWithoutExtension(resPath).ToLowerInvariant()}";
        var section = new TscnSection("ext_resource");
        section.SetAttribute("type", new GodotString(type));
        section.SetAttribute("path", new GodotString(resPath));
        section.SetAttribute("id", new GodotString(id));
        var lastExt = Document.Sections.FindLastIndex(s => s.Name == "ext_resource");
        Document.Sections.Insert(lastExt >= 0 ? lastExt + 1 : 0, section);
        var loadSteps = (Document.Descriptor.GetAttributeInt("load_steps") ?? 1) + 1;
        Document.Descriptor.SetAttribute("load_steps", new GodotInt(loadSteps));
        return id;
    }

    public void Save() => Project.SaveDocument(ScenePath, Document);
}
