using GodotMcp.Core.Project;
using GodotMcp.Core.Scenes;
using Xunit;

namespace GodotMcp.Tests;

public class SceneRefactorTests : IDisposable
{
    const string Scene = """
        [gd_scene load_steps=2 format=3]

        [ext_resource type="Script" path="res://player.gd" id="1_player"]

        [node name="Main" type="Node2D"]

        [node name="Player" type="CharacterBody2D" parent="."]
        script = ExtResource("1_player")

        [node name="Sprite" type="Sprite2D" parent="Player"]

        [node name="Hitbox" type="Area2D" parent="Player/Sprite"]

        [node name="Button" type="Button" parent="."]

        [connection signal="pressed" from="Button" to="Player" method="_on_button_pressed"]

        [connection signal="area_entered" from="Player/Sprite/Hitbox" to="Player" method="_on_hit"]
        """;

    readonly string _root;
    readonly GodotProject _project;

    public SceneRefactorTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "godot-mcp-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "project.godot"), "config_version=5\n\n[application]\n\nconfig/name=\"Test\"\n");
        File.WriteAllText(Path.Combine(_root, "main.tscn"), Scene);
        File.WriteAllText(Path.Combine(_root, "player.gd"), "extends CharacterBody2D\n\nfunc _on_button_pressed():\n\tpass\n");
        _project = GodotProject.Open(_root);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    SceneEditor OpenEditor() => new(_project, "res://main.tscn");

    [Fact]
    public void RenameNode_UpdatesDescendantParentPaths()
    {
        var editor = OpenEditor();
        editor.RenameNode("Player", "Hero");
        editor.Save();
        var tree = _project.LoadScene("res://main.tscn");
        Assert.Null(tree.FindNode("Player"));
        var hero = tree.FindNode("Hero");
        Assert.NotNull(hero);
        Assert.NotNull(tree.FindNode("Hero/Sprite/Hitbox"));
    }

    [Fact]
    public void RenameNode_UpdatesConnectionEndpoints()
    {
        var editor = OpenEditor();
        editor.RenameNode("Player", "Hero");
        editor.Save();
        var tree = _project.LoadScene("res://main.tscn");
        Assert.All(tree.Connections, c => Assert.Equal("Hero", c.To));
        Assert.Contains(tree.Connections, c => c.From == "Hero/Sprite/Hitbox");
    }

    [Fact]
    public void RenameNode_Root_OnlyChangesName()
    {
        var editor = OpenEditor();
        editor.RenameNode(".", "Game");
        editor.Save();
        var tree = _project.LoadScene("res://main.tscn");
        Assert.Equal("Game", tree.Root!.Name);
        Assert.NotNull(tree.FindNode("Player/Sprite"));
    }

    [Fact]
    public void RenameNode_MissingNode_Throws()
    {
        var editor = OpenEditor();
        Assert.Throws<InvalidOperationException>(() => editor.RenameNode("Ghost", "Anything"));
    }

    [Fact]
    public void RenameNode_CollidingName_Throws()
    {
        var editor = OpenEditor();
        Assert.Throws<InvalidOperationException>(() => editor.RenameNode("Player", "Button"));
    }

    [Fact]
    public void RenameNode_InvalidName_Throws()
    {
        var editor = OpenEditor();
        Assert.Throws<ArgumentException>(() => editor.RenameNode("Player", "Bad/Name"));
    }

    [Fact]
    public void ConnectSignal_AddsConnectionSection()
    {
        var editor = OpenEditor();
        editor.ConnectSignal("pressed", "Button", ".", "_on_pressed");
        editor.Save();
        var tree = _project.LoadScene("res://main.tscn");
        Assert.Contains(tree.Connections, c => c is { Signal: "pressed", From: "Button", To: ".", Method: "_on_pressed" });
    }

    [Fact]
    public void ConnectSignal_Duplicate_IsIdempotent()
    {
        var editor = OpenEditor();
        editor.ConnectSignal("pressed", "Button", "Player", "_on_button_pressed");
        Assert.Equal(2, editor.Document.Connections.Count());
    }

    [Fact]
    public void ConnectSignal_MissingNode_Throws()
    {
        var editor = OpenEditor();
        Assert.Throws<InvalidOperationException>(() => editor.ConnectSignal("pressed", "Ghost", ".", "_on_pressed"));
    }

    [Fact]
    public void DisconnectSignal_RemovesConnection()
    {
        var editor = OpenEditor();
        var removed = editor.DisconnectSignal("pressed", "Button", "Player", "_on_button_pressed");
        Assert.True(removed);
        Assert.Single(editor.Document.Connections);
    }

    [Fact]
    public void DisconnectSignal_NoMatch_ReturnsFalse()
    {
        var editor = OpenEditor();
        Assert.False(editor.DisconnectSignal("pressed", "Button", "Player", "_wrong_method"));
    }
}
