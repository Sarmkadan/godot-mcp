using System.ComponentModel;
using GodotMcp.Core.Parsing;
using GodotMcp.Core.Scenes;
using ModelContextProtocol.Server;

namespace GodotMcp.Server.Tools;

[McpServerToolType]
public sealed class SceneTools(ProjectLocator locator)
{
    [McpServerTool(Name = "godot_get_scene_tree"), Description("Parse a .tscn file and return its full node tree with types, groups and properties.")]
    public SceneTreeResult GetSceneTree([Description("Scene path as res:// or project-relative")] string scenePath, string? projectPath = null)
    {
        var tree = locator.Resolve(projectPath).LoadScene(scenePath);
        return new SceneTreeResult(
            tree.ScenePath, tree.Uid, tree.Format, tree.NodeCount,
            tree.Root is null ? null : NodeResult.From(tree.Root, recurse: true),
            tree.ExternalResources.Select(r => new ExternalResourceResult(r.Id, r.Type, r.Path, r.Uid)).ToList(),
            tree.Connections.Select(c => new ConnectionResult(c.Signal, c.From, c.To, c.Method)).ToList());
    }

    [McpServerTool(Name = "godot_get_node"), Description("Get a single node from a scene by its scene-tree path, including its properties.")]
    public NodeResult? GetNode(string scenePath, [Description("Node path within the scene, '.' for the root")] string nodePath, string? projectPath = null)
    {
        var node = locator.Resolve(projectPath).LoadScene(scenePath).FindNode(nodePath);
        return node is null ? null : NodeResult.From(node, recurse: false);
    }

    [McpServerTool(Name = "godot_create_node"), Description("Add a new node to a scene under the given parent and save the file.")]
    public MutationResult CreateNode(string scenePath, [Description("Name for the new node")] string name, [Description("Godot node type, e.g. Node2D, Sprite2D, Control")] string type, [Description("Parent node path, '.' for the root")] string parentPath = ".", string? projectPath = null)
    {
        var editor = new SceneEditor(locator.Resolve(projectPath), scenePath);
        editor.AddNode(name, type, parentPath);
        editor.Save();
        return new MutationResult(true, scenePath, $"Created node '{name}' of type '{type}' under '{parentPath}'");
    }

    [McpServerTool(Name = "godot_remove_node"), Description("Remove a node (and its descendants) from a scene and save the file.")]
    public MutationResult RemoveNode(string scenePath, string nodePath, string? projectPath = null)
    {
        var editor = new SceneEditor(locator.Resolve(projectPath), scenePath);
        var removed = editor.RemoveNode(nodePath);
        if (removed) editor.Save();
        return new MutationResult(removed, scenePath, removed ? $"Removed node '{nodePath}'" : $"Node '{nodePath}' not found");
    }

    [McpServerTool(Name = "godot_set_node_property"), Description("Set a property on a scene node using Godot text syntax (e.g. 'Vector2(10, 20)', '\"hello\"', 'true') and save the file.")]
    public MutationResult SetNodeProperty(string scenePath, string nodePath, string property, [Description("Value in Godot .tscn literal syntax")] string value, string? projectPath = null)
    {
        var editor = new SceneEditor(locator.Resolve(projectPath), scenePath);
        editor.SetNodeProperty(nodePath, property, GodotValue.Parse(value));
        editor.Save();
        return new MutationResult(true, scenePath, $"Set {nodePath}.{property} = {value}");
    }

    [McpServerTool(Name = "godot_get_node_property"), Description("Get a single property value from a scene node in Godot text syntax.")]
    public string? GetNodeProperty(string scenePath, string nodePath, string property, string? projectPath = null) =>
        new SceneEditor(locator.Resolve(projectPath), scenePath).GetNodeProperty(nodePath, property)?.ToTscnString();
}
