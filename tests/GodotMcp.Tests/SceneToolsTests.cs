using System;
using System.IO;
using GodotMcp.Core.Project;
using GodotMcp.Server;
using GodotMcp.Server.Tools;
using Xunit;

namespace GodotMcp.Tests;

public class SceneToolsTests : IDisposable
{
    private readonly string _root;
    private readonly GodotProject _project;
    private readonly ProjectLocator _locator;
    private readonly SceneTools _tools;

    public SceneToolsTests()
    {
        // Create a temporary project directory
        _root = Path.Combine(Path.GetTempPath(), "godot-mcp-scenetools-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        File.WriteAllText(
            Path.Combine(_root, "project.godot"),
            "config_version=5\n\n[application]\n\nconfig/name=\"Test\"\n");

        // Initialise core objects
        _project = GodotProject.Open(_root);
        _locator = new ProjectLocator(_root);
        _tools = new SceneTools(_locator);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string CreateTestScene(string sceneContent, string fileName = "test.tscn")
    {
        var filePath = Path.Combine(_root, fileName);
        File.WriteAllText(filePath, sceneContent);
        return $"res://{fileName}";
    }

    [Fact]
    public void GetSceneTree_ReturnsCorrectTreeForValidScene()
    {
        var scenePath = CreateTestScene("""
            [gd_scene load_steps=3 format=3 uid="uid://test123"]

            [node name="Root" type="Node"]
            [node name="Child" type="Sprite2D" parent="."]
            """);

        var result = _tools.GetSceneTree(scenePath);

        Assert.NotNull(result);
        Assert.Equal(scenePath, result.ScenePath);
        Assert.Equal("uid://test123", result.Uid);
        Assert.Equal(3, result.Format);
        Assert.Equal(2, result.NodeCount);
        Assert.NotNull(result.Root);
        Assert.Equal("Root", result.Root!.Name);
        Assert.Single(result.Root!.Children);
        Assert.Equal("Child", result.Root.Children[0].Name);
    }

    [Fact]
    public void GetSceneTree_ReturnsNullForNonExistentScene()
    {
        var result = _tools.GetSceneTree("res://nonexistent.tscn");
        Assert.Null(result);
    }

    [Fact]
    public void GetNode_ReturnsNodeWhenFound()
    {
        var scenePath = CreateTestScene("""
            [gd_scene format=3]

            [node name="Player" type="CharacterBody2D"]
            speed = 200
            [node name="Sprite" type="Sprite2D" parent="."]
            texture = ExtResource("1_icon")
            """, "player.tscn");

        File.WriteAllText(Path.Combine(_root, "icon.svg"), "<svg/>");

        var result = _tools.GetNode(scenePath, "Sprite");

        Assert.NotNull(result);
        Assert.Equal("Sprite", result!.Name);
        Assert.Equal("Sprite2D", result.Type);
        Assert.NotNull(result.Properties);
        Assert.Contains(result.Properties.Keys, k => k == "texture");
    }

    [Fact]
    public void GetNode_ReturnsNullForNonExistentNode()
    {
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Root" type="Node"]
            """);

        var result = _tools.GetNode(scenePath, "NonExistent");
        Assert.Null(result);
    }

    [Fact]
    public void GetNode_RootNode_WhenPathIsDot()
    {
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Root" type="Node"]
            """);

        var result = _tools.GetNode(scenePath, ".");

        Assert.NotNull(result);
        Assert.Equal("Root", result!.Name);
    }

    [Fact]
    public void CreateNode_AddsNodeSuccessfully()
    {
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Root" type="Node"]
            """);

        var result = _tools.CreateNode(scenePath, "NewSprite", "Sprite2D");

        Assert.True(result.Changed);
        Assert.Equal(scenePath, result.ScenePath);
        Assert.Contains("Created node 'NewSprite'", result.Detail);

        var sceneContent = File.ReadAllText(Path.Combine(_root, "test.tscn"));
        Assert.Contains("[node name=\"NewSprite\" type=\"Sprite2D\" parent=\".\"]", sceneContent);
    }

    [Fact]
    public void CreateNode_WithCustomParentPath()
    {
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Root" type="Node"]
            [node name="Parent" type="Node2D" parent="."]
            """);

        var result = _tools.CreateNode(scenePath, "Child", "Sprite2D", "Parent");

        Assert.True(result.Changed);
        var sceneContent = File.ReadAllText(Path.Combine(_root, "test.tscn"));
        Assert.Contains("[node name=\"Child\" type=\"Sprite2D\" parent=\"./Parent\"]", sceneContent);
    }

    [Fact]
    public void RemoveNode_RemovesExistingNode()
    {
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Root" type="Node"]
            [node name="ToRemove" type="Sprite2D" parent="."]
            """);

        var result = _tools.RemoveNode(scenePath, "ToRemove");

        Assert.True(result.Changed);
        Assert.Equal(scenePath, result.ScenePath);
        Assert.Contains("Removed node 'ToRemove'", result.Detail);

        var sceneContent = File.ReadAllText(Path.Combine(_root, "test.tscn"));
        Assert.DoesNotContain("[node name=\"ToRemove\"", sceneContent);
    }

    [Fact]
    public void RemoveNode_ReturnsFalseForNonExistentNode()
    {
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Root" type="Node"]
            """);

        var result = _tools.RemoveNode(scenePath, "NonExistent");

        Assert.False(result.Changed);
        Assert.Equal(scenePath, result.ScenePath);
        Assert.Contains("not found", result.Detail);
    }

    [Fact]
    public void RenameNode_ChangesNodeNameSuccessfully()
    {
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="OldName" type="Node"]
            """);

        var result = _tools.RenameNode(scenePath, "OldName", "NewName");

        Assert.True(result.Changed);
        Assert.Equal(scenePath, result.ScenePath);
        Assert.Contains("Renamed node 'OldName' to 'NewName'", result.Detail);

        var sceneContent = File.ReadAllText(Path.Combine(_root, "test.tscn"));
        Assert.Contains("[node name=\"NewName\" type=\"Node\"", sceneContent);
        Assert.DoesNotContain("[node name=\"OldName\"", sceneContent);
    }

    [Fact]
    public void ConnectSignal_AddsConnectionSuccessfully()
    {
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Emitter" type="Button" parent="."]
            [node name="Receiver" type="Node" parent="."]
            """);

        var result = _tools.ConnectSignal(scenePath, "pressed", ".", "./Receiver", "_on_button_pressed");

        Assert.True(result.Changed);
        Assert.Equal(scenePath, result.ScenePath);
        Assert.Contains("Connected ./pressed -> ./Receiver._on_button_pressed", result.Detail);

        var sceneContent = File.ReadAllText(Path.Combine(_root, "test.tscn"));
        Assert.Contains("[connection signal=\"pressed\" from=\".\" to=\"./Receiver\" method=\"_on_button_pressed\"]", sceneContent);
    }

    [Fact]
    public void DisconnectSignal_RemovesExistingConnection()
    {
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Emitter" type="Button" parent="."]
            [node name="Receiver" type="Node" parent="."]
            [connection signal="pressed" from="." to="./Receiver" method="_on_button_pressed"]
            """);

        var result = _tools.DisconnectSignal(scenePath, "pressed", ".", "./Receiver", "_on_button_pressed");

        Assert.True(result.Changed);
        Assert.Equal(scenePath, result.ScenePath);
        Assert.Contains("Disconnected .pressed -> ./Receiver._on_button_pressed", result.Detail);

        var sceneContent = File.ReadAllText(Path.Combine(_root, "test.tscn"));
        Assert.DoesNotContain("[connection", sceneContent);
    }

    [Fact]
    public void SetNodeProperty_SetsPropertySuccessfully()
    {
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Sprite" type="Sprite2D" parent="."]
            position = Vector2(0, 0)
            """);

        var result = _tools.SetNodeProperty(scenePath, "Sprite", "position", "Vector2(100, 200)");

        Assert.True(result.Changed);
        Assert.Equal(scenePath, result.ScenePath);
        Assert.Contains("Set Sprite.position = Vector2(100, 200)", result.Detail);

        var sceneContent = File.ReadAllText(Path.Combine(_root, "test.tscn"));
        Assert.Contains("position = Vector2(100, 200)", sceneContent);
        Assert.DoesNotContain("position = Vector2(0, 0)", sceneContent);
    }

    [Fact]
    public void GetNodeProperty_ReturnsPropertyValue()
    {
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Sprite" type="Sprite2D" parent="."]
            position = Vector2(50, 75)
            modulate = Color(1, 0.5, 0, 1)
            """);

        var positionResult = _tools.GetNodeProperty(scenePath, "Sprite", "position");
        var colorResult = _tools.GetNodeProperty(scenePath, "Sprite", "modulate");

        Assert.Equal("Vector2(50, 75)", positionResult);
        Assert.Equal("Color(1, 0.5, 0, 1)", colorResult);
    }

    [Fact]
    public void GetNodeProperty_ReturnsNullForNonExistentProperty()
    {
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Sprite" type="Sprite2D" parent="."]
            """);

        var result = _tools.GetNodeProperty(scenePath, "Sprite", "non_existent_prop");
        Assert.Null(result);
    }

    [Fact]
    public void GetNodeProperty_ReturnsNullForNonExistentNode()
    {
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Root" type="Node"]
            """);

        var result = _tools.GetNodeProperty(scenePath, "NonExistent", "position");
        Assert.Null(result);
    }

    [Fact]
    public void CreateNode_WithNullScenePath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _tools.CreateNode(null!, "Test", "Node"));
    }

    [Fact]
    public void GetNode_WithEmptyScenePath_ReturnsNull()
    {
        var result = _tools.GetNode("", ".");
        Assert.Null(result);
    }

    [Fact]
    public void ConnectSignal_WithDuplicateConnection_IsNoOp()
    {
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Emitter" type="Button" parent="."]
            [node name="Receiver" type="Node" parent="."]
            [connection signal="pressed" from="." to="./Receiver" method="_on_button_pressed"]
            """);

        var result = _tools.ConnectSignal(scenePath, "pressed", ".", "./Receiver", "_on_button_pressed");

        Assert.True(result.Changed);
        Assert.Equal(scenePath, result.ScenePath);

        var sceneContent = File.ReadAllText(Path.Combine(_root, "test.tscn"));
        var connectionCount = System.Text.RegularExpressions.Regex.Matches(sceneContent, @"\[connection").Count;
        Assert.Equal(1, connectionCount);
    }
}
