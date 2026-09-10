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
        var nodePath = parentPath == "." ? name : $"{parentPath}/{name}";
        if (SceneTreeBuilder.FindNodeSection(Document, nodePath) is not null)
            throw new InvalidOperationException($"Node {nodePath} already exists in {ScenePath}");
        var section = new TscnSection("node");
        section.SetAttribute("name", new GodotString(name));
        section.SetAttribute("type", new GodotString(type));
        section.SetAttribute("parent", new GodotString(parentPath));
        var subtreePrefix = parentPath + "/";
        var insertAfter = Document.Sections.FindLastIndex(s =>
        {
            if (s.Name != "node") return false;
            var parent = s.GetAttributeString("parent");
            var sectionName = s.GetAttributeString("name") ?? "";
            var path = parent switch
            {
                null => ".",
                "." => sectionName,
                _ => $"{parent}/{sectionName}"
            };
            return parentPath == "." || path == parentPath || path.StartsWith(subtreePrefix, StringComparison.Ordinal);
        });
        if (insertAfter < 0) insertAfter = Document.Sections.FindLastIndex(s => s.Name == "node");
        if (insertAfter >= 0) Document.Sections.Insert(insertAfter + 1, section);
        else Document.Sections.Add(section);
        return section;
    }

    public bool RemoveNode(string nodePath)
    {
        var section = SceneTreeBuilder.FindNodeSection(Document, nodePath);
        if (section is null) return false;
        var prefix = nodePath + "/";
        bool ReferencesRemovedNode(TscnSection connection, string key)
        {
            var value = connection.GetAttribute(key) switch
            {
                GodotString s => s.Value,
                GodotNodePath p => p.Value,
                _ => null
            };
            return nodePath == "." || value == nodePath || value?.StartsWith(prefix) == true;
        }
        Document.Sections.RemoveAll(s =>
            s.Name == "connection" &&
            (ReferencesRemovedNode(s, "from") || ReferencesRemovedNode(s, "to")));
        Document.Sections.RemoveAll(s =>
            s.Name == "node" &&
            (nodePath == "." ||
             s.GetAttributeString("parent") is { } parent &&
             (parent == nodePath || parent.StartsWith(prefix))));
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

    public void DuplicateNode(string nodePath, string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);
        if (newName.Contains('/') || newName.Contains(':') || newName.Contains('@'))
            throw new ArgumentException($"'{newName}' is not a valid node name", nameof(newName));

        var sourceSection = SceneTreeBuilder.FindNodeSection(Document, nodePath)
            ?? throw new InvalidOperationException($"Node '{nodePath}' not found in {ScenePath}");

        var sourceParent = sourceSection.GetAttributeString("parent");
        var sourceName = sourceSection.GetAttributeString("name") ?? "";
        var sourcePath = sourceParent == null ? "." :
                         sourceParent == "." ? sourceName :
                         $"{sourceParent}/{sourceName}";

        var newPath = sourceParent == null ? "." :
                      sourceParent == "." ? newName :
                      $"{sourceParent}/{newName}";
        if (SceneTreeBuilder.FindNodeSection(Document, newPath) is not null)
            throw new InvalidOperationException($"A node already exists at '{newPath}' in {ScenePath}");

        // Helper to check if a node path is in the subtree rooted at subtreeRootPath
        bool IsInSubtree(string path, string subtreeRootPath)
        {
            return path == subtreeRootPath || path.StartsWith(subtreeRootPath + "/", StringComparison.Ordinal);
        }

        // Collect all node sections in the subtree (including root) in document order
        var subtreeNodes = new List<TscnSection>();
        foreach (var section in Document.Sections)
        {
            if (section.Name != "node") continue;
            var parent = section.GetAttributeString("parent");
            var name = section.GetAttributeString("name") ?? "";
            var path = parent == null ? "." :
                       parent == "." ? name :
                       $"{parent}/{name}";
            if (IsInSubtree(path, nodePath))
                subtreeNodes.Add(section);
        }

        // Create mapping from original node section to duplicated node section
        var mapping = new Dictionary<TscnSection, TscnSection>();
        var duplicatedNodes = new List<TscnSection>();

        // First pass: duplicate nodes and set up mapping
        foreach (var original in subtreeNodes)
        {
            var copy = new TscnSection(original.Name);
            // Copy attributes
            foreach (var attr in original.Attributes)
                copy.SetAttribute(attr.Key, attr.Value);
            // Copy properties
            foreach (var prop in original.Properties)
                copy.SetProperty(prop.Key, prop.Value);

            if (original == sourceSection)
            {
                // Root of the duplicated subtree: set new name
                copy.SetAttribute("name", new GodotString(newName));
                // Parent remains the same as source's parent (so it becomes a sibling)
            }
            else
            {
                // For descendants, update parent attribute to point to duplicated parent
                var originalParentValue = original.GetAttributeString("parent");
                if (originalParentValue is not null)
                {
                    // Find the original parent section
                    var originalParentSection = Document.Sections.FirstOrDefault(s =>
                        s.Name == "node" &&
                        s.GetAttributeString("name") == originalParentValue.Split('/').Last() &&
                        (s.GetAttributeString("parent") ?? "") ==
                        (originalParentValue.Contains('/') ?
                            string.Join("/", originalParentValue.Split('/').Take(originalParentValue.Split('/').Length - 1)) :
                        ""));
                    // Actually, we can use the mapping: the original parent must be in the subtree (since we're processing in order and parent comes before children)
                    // But to be safe, we'll find the duplicated section for the original parent via mapping
                    var originalParentSectionByPath = SceneTreeBuilder.FindNodeSection(Document, originalParentValue);
                    if (originalParentSectionByPath is not null && mapping.TryGetValue(originalParentSectionByPath, out var duplicatedParent))
                    {
                        var duplicatedParentName = duplicatedParent.GetAttributeString("name") ?? "";
                        var duplicatedParentParent = duplicatedParent.GetAttributeString("parent");
                        var duplicatedParentPath = duplicatedParentParent == null ? "." :
                                                   duplicatedParentParent == "." ? duplicatedParentName :
                                                   $"{duplicatedParentParent}/{duplicatedParentName}";
                        copy.SetAttribute("parent", new GodotString(duplicatedParentPath));
                    }
                    else
                    {
                        // Fallback: if parent not found in mapping (shouldn't happen for valid subtree), keep original parent
                        copy.SetAttribute("parent", new GodotString(originalParentValue));
                    }
                }
            }

            mapping[original] = copy;
            duplicatedNodes.Add(copy);
        }

        // Second pass: duplicate connections that are entirely within the subtree
        var duplicatedConnections = new List<TscnSection>();
        foreach (var connection in Document.Connections)
        {
            var from = connection.GetAttributeString("from");
            var to = connection.GetAttributeString("to");
            if (from is not null && to is not null &&
                IsInSubtree(from, nodePath) && IsInSubtree(to, nodePath))
            {
                var copy = new TscnSection(connection.Name);
                // Copy attributes
                foreach (var attr in connection.Attributes)
                    copy.SetAttribute(attr.Key, attr.Value);
                // Update from and to to point to duplicated nodes
                var fromSection = SceneTreeBuilder.FindNodeSection(Document, from);
                var toSection = SceneTreeBuilder.FindNodeSection(Document, to);
                if (fromSection is not null && mapping.TryGetValue(fromSection, out var duplicatedFrom))
                {
                    var duplicatedFromName = duplicatedFrom.GetAttributeString("name") ?? "";
                    var duplicatedFromParent = duplicatedFrom.GetAttributeString("parent");
                    var duplicatedFromPath = duplicatedFromParent == null ? "." :
                                             duplicatedFromParent == "." ? duplicatedFromName :
                                             $"{duplicatedFromParent}/{duplicatedFromName}";
                    copy.SetAttribute("from", new GodotString(duplicatedFromPath));
                }
                else
                {
                    copy.SetAttribute("from", new GodotString(from));
                }
                if (toSection is not null && mapping.TryGetValue(toSection, out var duplicatedTo))
                {
                    var duplicatedToName = duplicatedTo.GetAttributeString("name") ?? "";
                    var duplicatedToParent = duplicatedTo.GetAttributeString("parent");
                    var duplicatedToPath = duplicatedToParent == null ? "." :
                                           duplicatedToParent == "." ? duplicatedToName :
                                           $"{duplicatedToParent}/{duplicatedToName}";
                    copy.SetAttribute("to", new GodotString(duplicatedToPath));
                }
                else
                {
                    copy.SetAttribute("to", new GodotString(to));
                }
                // Copy other attributes (signal, method) are already copied above
                duplicatedConnections.Add(copy);
            }
        }

        // Insert duplicated nodes after the last node of the original subtree
        int lastIndex = -1;
        for (int i = 0; i < Document.Sections.Count; i++)
        {
            var section = Document.Sections[i];
            if (section.Name != "node") continue;
            var parent = section.GetAttributeString("parent");
            var name = section.GetAttributeString("name") ?? "";
            var path = parent == null ? "." :
                       parent == "." ? name :
                       $"{parent}/{name}";
            if (IsInSubtree(path, nodePath))
                lastIndex = i;
        }

        if (lastIndex >= 0)
        {
            // Insert duplicated nodes in reverse order so they appear in correct order
            for (int i = duplicatedNodes.Count - 1; i >= 0; i--)
            {
                Document.Sections.Insert(lastIndex + 1, duplicatedNodes[i]);
            }
        }
        else
        {
            // Fallback: insert at end
            foreach (var node in duplicatedNodes)
                Document.Sections.Add(node);
        }

        // Insert duplicated connections at the end of the document
        foreach (var conn in duplicatedConnections)
            Document.Sections.Add(conn);
    }

    public void MoveNode(string nodePath, string newParentPath)
    {
        var section = SceneTreeBuilder.FindNodeSection(Document, nodePath)
            ?? throw new InvalidOperationException($"Node '{nodePath}' not found in {ScenePath}");
        if (newParentPath != "." && SceneTreeBuilder.FindNodeSection(Document, newParentPath) is null)
            throw new InvalidOperationException($"Parent node '{newParentPath}' not found in {ScenePath}");
        var oldPrefix = nodePath + "/";
        if (newParentPath == nodePath || newParentPath.StartsWith(oldPrefix, StringComparison.Ordinal))
            throw new InvalidOperationException($"Cannot move node '{nodePath}' under itself or one of its descendants");
        if (section.GetAttributeString("parent") is null)
            throw new InvalidOperationException("The scene root node cannot be moved");

        var name = section.GetAttributeString("name") ?? "";
        var newPath = newParentPath == "." ? name : $"{newParentPath}/{name}";
        if (newPath != nodePath && SceneTreeBuilder.FindNodeSection(Document, newPath) is not null)
            throw new InvalidOperationException($"A node already exists at '{newPath}' in {ScenePath}");

        var newPrefix = newPath + "/";
        string Rewrite(string path) =>
            path == nodePath ? newPath : path.StartsWith(oldPrefix, StringComparison.Ordinal) ? newPrefix + path[oldPrefix.Length..] : path;

        section.SetAttribute("parent", new GodotString(newParentPath));
        foreach (var node in Document.Nodes)
        {
            if (ReferenceEquals(node, section)) continue;
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

        // Validate and resolve the path to ensure it stays within project bounds
        var resolvedPath = Project.ResolveResPath(resPath);

        var existing = Document.ExtResources.FirstOrDefault(s => s.GetAttributeString("path") == resPath);
        if (existing is not null) return existing.GetAttributeString("id") ?? "";
        var index = Document.ExtResources.Count() + 1;
        // Extract filename safely from the resolved path to avoid directory traversal in the ID
        var fileName = Path.GetFileName(resolvedPath).ToLowerInvariant();
        var id = $"{index}_{fileName}";
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
