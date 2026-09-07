using GodotMcp.Core.Parsing;
using GodotMcp.Core.Project;
using GodotMcp.Core.Scenes;
using Xunit;

namespace GodotMcp.Tests;

public class SceneEditorEdgeCaseTests : IDisposable
{
    const string Scene = """
        [gd_scene format=3]

        [node name="Main" type="Node2D"]

        [node name="Enemy" type="Node2D" parent="."]

        [node name="Weapon" type="Node2D" parent="Enemy"]

        [node name="Muzzle" type="Marker2D" parent="Enemy/Weapon"]

        [node name="Enemy2" type="Node2D" parent="."]

        [node name="Weapon" type="Node2D" parent="Enemy2"]

        [connection signal="tree_entered" from="Enemy/Weapon/Muzzle" to="Enemy" method="_on_enemy_ready"]
        """;

    readonly string _root;
    readonly GodotProject _project;

    public SceneEditorEdgeCaseTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "godot-mcp-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "project.godot"), "config_version=5\n\n[application]\n\nconfig/name=\"Test\"\n");
        File.WriteAllText(Path.Combine(_root, "main.tscn"), Scene);
        _project = GodotProject.Open(_root);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    SceneEditor OpenEditor() => new(_project, "res://main.tscn");

    [Fact]
    public void AddNode_WithMissingParent_Throws()
    {
        var editor = OpenEditor();

        Assert.Throws<InvalidOperationException>(() => editor.AddNode("Orphan", "Node", "Missing"));
        Assert.Null(SceneTreeBuilder.FindNodeSection(editor.Document, "Orphan"));
    }

    [Fact]
    public void AddNode_UnderRootDot_CreatesRootChild()
    {
        var editor = OpenEditor();

        var added = editor.AddNode("Camera", "Camera2D", ".");

        Assert.Equal(".", added.GetAttributeString("parent"));
        Assert.Same(added, SceneTreeBuilder.FindNodeSection(editor.Document, "Camera"));
    }

    [Fact]
    public void RemoveNode_RemovesFullSubtree_ButNotSimilarlyPrefixedSibling()
    {
        var editor = OpenEditor();

        Assert.True(editor.RemoveNode("Enemy"));

        Assert.Null(SceneTreeBuilder.FindNodeSection(editor.Document, "Enemy"));
        Assert.Null(SceneTreeBuilder.FindNodeSection(editor.Document, "Enemy/Weapon"));
        Assert.Null(SceneTreeBuilder.FindNodeSection(editor.Document, "Enemy/Weapon/Muzzle"));
        Assert.NotNull(SceneTreeBuilder.FindNodeSection(editor.Document, "Enemy2"));
        Assert.NotNull(SceneTreeBuilder.FindNodeSection(editor.Document, "Enemy2/Weapon"));
    }

    [Fact]
    public void RemoveNode_NonexistentNode_ReturnsFalse()
    {
        var editor = OpenEditor();

        Assert.False(editor.RemoveNode("Missing"));
        Assert.NotNull(SceneTreeBuilder.FindNodeSection(editor.Document, "Enemy"));
    }

    [Fact]
    public void SetNodeProperty_OnMissingNode_Throws()
    {
        var editor = OpenEditor();

        Assert.Throws<InvalidOperationException>(() =>
            editor.SetNodeProperty("Missing", "visible", new GodotBool(true)));
    }

    [Fact]
    public void RemoveNodeProperty_MissingProperty_ReturnsFalse()
    {
        var editor = OpenEditor();

        Assert.False(editor.RemoveNodeProperty("Enemy", "visible"));
        Assert.NotNull(SceneTreeBuilder.FindNodeSection(editor.Document, "Enemy"));
    }

    [Fact]
    public void RenameNode_RewritesDescendantParentsAndConnections()
    {
        var editor = OpenEditor();

        editor.RenameNode("Enemy", "Boss");

        Assert.Equal("Boss", SceneTreeBuilder.FindNodeSection(editor.Document, "Boss/Weapon")!.GetAttributeString("parent"));
        Assert.Equal("Boss/Weapon", SceneTreeBuilder.FindNodeSection(editor.Document, "Boss/Weapon/Muzzle")!.GetAttributeString("parent"));
        var connection = Assert.Single(editor.Document.Connections);
        Assert.Equal("Boss/Weapon/Muzzle", connection.GetAttributeString("from"));
        Assert.Equal("Boss", connection.GetAttributeString("to"));
        Assert.NotNull(SceneTreeBuilder.FindNodeSection(editor.Document, "Enemy2"));
    }
}
