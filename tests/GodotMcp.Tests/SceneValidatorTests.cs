using GodotMcp.Core.Project;
using GodotMcp.Core.Scenes;
using Xunit;

namespace GodotMcp.Tests;

public class SceneValidatorTests : IDisposable
{
    readonly string _root;
    readonly GodotProject _project;

    public SceneValidatorTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "godot-mcp-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "project.godot"), "config_version=5\n\n[application]\n\nconfig/name=\"Test\"\n");
        _project = GodotProject.Open(_root);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    IReadOnlyList<SceneIssue> Validate(string scene)
    {
        File.WriteAllText(Path.Combine(_root, "scene.tscn"), scene);
        return SceneValidator.Validate(_project, "res://scene.tscn");
    }

    [Fact]
    public void CleanScene_HasNoIssues()
    {
        File.WriteAllText(Path.Combine(_root, "icon.svg"), "<svg/>");
        var issues = Validate("""
            [gd_scene load_steps=2 format=3]

            [ext_resource type="Texture2D" path="res://icon.svg" id="1_icon"]

            [node name="Main" type="Node2D"]

            [node name="Sprite" type="Sprite2D" parent="."]
            texture = ExtResource("1_icon")
            """);
        Assert.Empty(issues);
    }

    [Fact]
    public void MissingExtResourceFile_IsError()
    {
        var issues = Validate("""
            [gd_scene load_steps=2 format=3]

            [ext_resource type="Texture2D" path="res://gone.png" id="1_gone"]

            [node name="Main" type="Node2D"]
            texture = ExtResource("1_gone")
            """);
        Assert.Contains(issues, i => i is { Code: "missing-file", Severity: IssueSeverity.Error });
    }

    [Fact]
    public void UnusedExtResource_IsWarning()
    {
        File.WriteAllText(Path.Combine(_root, "icon.svg"), "<svg/>");
        var issues = Validate("""
            [gd_scene load_steps=2 format=3]

            [ext_resource type="Texture2D" path="res://icon.svg" id="1_icon"]

            [node name="Main" type="Node2D"]
            """);
        Assert.Contains(issues, i => i is { Code: "unused-ext-resource", Severity: IssueSeverity.Warning });
    }

    [Fact]
    public void OrphanSubResource_IsWarning()
    {
        var issues = Validate("""
            [gd_scene load_steps=2 format=3]

            [sub_resource type="RectangleShape2D" id="RectangleShape2D_1"]

            [node name="Main" type="Node2D"]
            """);
        Assert.Contains(issues, i => i.Code == "orphan-sub-resource");
    }

    [Fact]
    public void UndeclaredSubResource_IsError()
    {
        var issues = Validate("""
            [gd_scene format=3]

            [node name="Main" type="Area2D"]

            [node name="Shape" type="CollisionShape2D" parent="."]
            shape = SubResource("RectangleShape2D_missing")
            """);
        Assert.Contains(issues, i => i is { Code: "missing-sub-resource", Severity: IssueSeverity.Error });
    }

    [Fact]
    public void UndeclaredExtResourceReference_IsError()
    {
        var issues = Validate("""
            [gd_scene format=3]

            [node name="Main" type="Node2D"]
            texture = ExtResource("1_missing")
            """);
        Assert.Contains(issues, i => i.Code == "missing-ext-resource");
    }

    [Fact]
    public void DuplicateNodePath_IsError()
    {
        var issues = Validate("""
            [gd_scene format=3]

            [node name="Main" type="Node2D"]

            [node name="A" type="Node2D" parent="."]

            [node name="A" type="Sprite2D" parent="."]
            """);
        Assert.Contains(issues, i => i.Code == "duplicate-node-path");
    }

    [Fact]
    public void DanglingConnection_IsError()
    {
        var issues = Validate("""
            [gd_scene format=3]

            [node name="Main" type="Node2D"]

            [connection signal="pressed" from="Ghost" to="." method="_on_pressed"]
            """);
        Assert.Contains(issues, i => i.Code == "dangling-connection");
    }

    [Fact]
    public void ConnectionIntoInstancedChild_IsNotDangling()
    {
        File.WriteAllText(Path.Combine(_root, "hud.tscn"), "[gd_scene format=3]\n\n[node name=\"Hud\" type=\"Control\"]\n");
        var issues = Validate("""
            [gd_scene load_steps=2 format=3]

            [ext_resource type="PackedScene" path="res://hud.tscn" id="1_hud"]

            [node name="Main" type="Node2D"]

            [node name="Hud" parent="." instance=ExtResource("1_hud")]

            [connection signal="pressed" from="Hud/StartButton" to="." method="_on_start"]
            """);
        Assert.DoesNotContain(issues, i => i.Code == "dangling-connection");
    }

    [Fact]
    public void MissingGdScriptHandler_IsWarning()
    {
        File.WriteAllText(Path.Combine(_root, "main.gd"), "extends Node2D\n\nfunc _ready():\n\tpass\n");
        var issues = Validate("""
            [gd_scene load_steps=2 format=3]

            [ext_resource type="Script" path="res://main.gd" id="1_main"]

            [node name="Main" type="Node2D"]
            script = ExtResource("1_main")

            [node name="Button" type="Button" parent="."]

            [connection signal="pressed" from="Button" to="." method="_on_button_pressed"]
            """);
        Assert.Contains(issues, i => i.Code == "missing-handler");
    }

    [Fact]
    public void PresentGdScriptHandler_IsClean()
    {
        File.WriteAllText(Path.Combine(_root, "main.gd"), "extends Node2D\n\nfunc _on_button_pressed():\n\tpass\n");
        var issues = Validate("""
            [gd_scene load_steps=2 format=3]

            [ext_resource type="Script" path="res://main.gd" id="1_main"]

            [node name="Main" type="Node2D"]
            script = ExtResource("1_main")

            [node name="Button" type="Button" parent="."]

            [connection signal="pressed" from="Button" to="." method="_on_button_pressed"]
            """);
        Assert.DoesNotContain(issues, i => i.Code == "missing-handler");
    }
}
