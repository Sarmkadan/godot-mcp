using GodotMcp.Core.Parsing;
using GodotMcp.Core.Project;

namespace GodotMcp.Core.Scenes;

public sealed class SceneEditor(GodotProject project, string scenePath)
{
    public GodotProject Project { get; } = project;
    public string ScenePath { get; } = scenePath;
    public TscnDocument Document { get; } = project.LoadDocument(scenePath);

    public TscnSection AddNode(string name, string type, string parentPath)
    {
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

    public string AddExtResource(string type, string resPath)
    {
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
