using GodotMcp.Core.Parsing;
using GodotMcp.Core.Project;
using GodotMcp.Core.Scenes;
using Xunit;

namespace GodotMcp.Tests;

public class SceneEditorTests : IDisposable
{
    const string Scene = """
        [gd_scene load_steps=2 format=3]

        [ext_resource type="Texture2D" path="res://icon.svg" id="1_icon"]

        [node name="Main" type="Node2D"]

        [node name="Sprite" type="Sprite2D" parent="."]
        texture = ExtResource("1_icon")

        [node name="Label" type="Label" parent="Sprite"]
        text = "hp"
        """;

    readonly string _root;
    readonly GodotProject _project;

    public SceneEditorTests()
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
    public void AddNode_AppearsInReparsedScene()
    {
        var editor = OpenEditor();
        editor.AddNode("Enemy", "Area2D", ".");
        editor.Save();
        var tree = _project.LoadScene("res://main.tscn");
        var enemy = tree.Root!.Children.Single(c => c.Name == "Enemy");
        Assert.Equal("Area2D", enemy.Type);
    }

    [Fact]
    public void AddNode_UnderNestedParent()
    {
        var editor = OpenEditor();
        editor.AddNode("Icon", "TextureRect", "Sprite/Label");
        var section = SceneTreeBuilder.FindNodeSection(editor.Document, "Sprite/Label/Icon");
        Assert.NotNull(section);
        Assert.Equal("Sprite/Label", section.GetAttributeString("parent"));
    }

    [Fact]
    public void AddNode_ThrowsForMissingParent()
    {
        var editor = OpenEditor();
        Assert.Throws<InvalidOperationException>(() => editor.AddNode("X", "Node", "NoSuchNode"));
    }

    [Fact]
    public void SetNodeProperty_RoundTripsThroughSaveAndReparse()
    {
        var editor = OpenEditor();
        editor.SetNodeProperty("Sprite", "position", GodotConstructor.Vector2(10, 20));
        editor.SetNodeProperty("Sprite/Label", "text", new GodotString("mp"));
        editor.Save();
        var reloaded = OpenEditor();
        Assert.Equal("Vector2(10.0, 20.0)", reloaded.GetNodeProperty("Sprite", "position")!.ToTscnString());
        Assert.Equal(new GodotString("mp"), reloaded.GetNodeProperty("Sprite/Label", "text"));
    }

    [Fact]
    public void SetNodeProperty_OverwritesExistingValue()
    {
        var editor = OpenEditor();
        editor.SetNodeProperty("Sprite/Label", "text", new GodotString("first"));
        editor.SetNodeProperty("Sprite/Label", "text", new GodotString("second"));
        var section = SceneTreeBuilder.FindNodeSection(editor.Document, "Sprite/Label")!;
        Assert.Single(section.Properties, p => p.Key == "text");
        Assert.Equal(new GodotString("second"), section.GetProperty("text"));
    }

    [Fact]
    public void RemoveNodeProperty_RemovesOnlyThatKey()
    {
        var editor = OpenEditor();
        Assert.True(editor.RemoveNodeProperty("Sprite/Label", "text"));
        Assert.False(editor.RemoveNodeProperty("Sprite/Label", "text"));
        Assert.Null(editor.GetNodeProperty("Sprite/Label", "text"));
    }

    [Fact]
    public void RemoveNode_RemovesDescendantsToo()
    {
        var editor = OpenEditor();
        Assert.True(editor.RemoveNode("Sprite"));
        Assert.Null(SceneTreeBuilder.FindNodeSection(editor.Document, "Sprite"));
        Assert.Null(SceneTreeBuilder.FindNodeSection(editor.Document, "Sprite/Label"));
        Assert.NotNull(SceneTreeBuilder.FindNodeSection(editor.Document, "."));
    }

    [Fact]
    public void RemoveNode_ReturnsFalseForUnknownPath()
    {
        Assert.False(OpenEditor().RemoveNode("Ghost"));
    }

    [Fact]
    public void AddExtResource_AssignsIdAndBumpsLoadSteps()
    {
        var editor = OpenEditor();
        var id = editor.AddExtResource("Script", "res://enemy.gd");
        Assert.Equal("2_enemy", id);
        Assert.Equal(3, editor.Document.Descriptor.GetAttributeInt("load_steps"));
        Assert.NotNull(editor.Document.FindExtResource(id));
    }

    [Fact]
    public void AddExtResource_IsIdempotentPerPath()
    {
        var editor = OpenEditor();
        var first = editor.AddExtResource("Script", "res://enemy.gd");
        var second = editor.AddExtResource("Script", "res://enemy.gd");
        Assert.Equal(first, second);
        Assert.Equal(2, editor.Document.ExtResources.Count());
    }

    [Fact]
    public void SceneTree_BuildsHierarchyFromDocument()
    {
        var tree = _project.LoadScene("res://main.tscn");
        Assert.Equal("Main", tree.Root!.Name);
        var sprite = Assert.Single(tree.Root.Children);
        Assert.Equal("Sprite", sprite.Name);
        var label = Assert.Single(sprite.Children);
        Assert.Equal("Label", label.Name);
        Assert.Equal(new GodotString("hp"), label.Properties.Single(p => p.Name == "text").Value);
    }

    [Fact]
    public void FullEdit_RoundTripSurvivesSecondReparse()
    {
        var editor = OpenEditor();
        var id = editor.AddExtResource("Script", "res://enemy.gd");
        var node = editor.AddNode("Enemy", "Area2D", ".");
        node.SetProperty("script", GodotConstructor.ExtResource(id));
        editor.Save();
        var serialized = File.ReadAllText(Path.Combine(_root, "main.tscn"));
        var reparsed = TscnParser.Parse(serialized);
        Assert.Equal(serialized, reparsed.Serialize());
        var enemy = SceneTreeBuilder.FindNodeSection(reparsed, "Enemy")!;
        var script = Assert.IsType<GodotConstructor>(enemy.GetProperty("script"));
        Assert.Equal(id, script.ReferenceId);
    }
}
