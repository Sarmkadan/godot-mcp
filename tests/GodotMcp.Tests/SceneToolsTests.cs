using GodotMcp.Core.Project;
using GodotMcp.Server;
using GodotMcp.Server.Tools;
using Xunit;

namespace GodotMcp.Tests;

public class SceneToolsTests : IDisposable
{
    readonly string _root;
    readonly GodotProject _project;
    readonly ProjectLocator _locator;
    readonly SceneTools _tools;

    public SceneToolsTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "godot-mcp-scenetools-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "project.godot"), "config_version=5\n\n[application]\n\nconfig/name=\"Test\"\n");
        _project = GodotProject.Open(_root);
        _locator = new ProjectLocator(_root);
        _tools = new SceneTools(_locator);
    (_locator);
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
        // Arrange
        var scenePath = CreateTestScene("""
            [gd_scene load_steps=3 format=3 uid="uid://test123"]

            [node name="Root" type="Node"]
            [node name="Child" type="Sprite2D" parent="."]
            """);

        // Act
        var result = _tools.GetSceneTree(scenePath);

        // Assert
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
        // Act
        var result = _tools.GetSceneTree("res://nonexistent.tscn");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetNode_ReturnsNodeWhenFound()
    {
        // Arrange
        var scenePath = CreateTestScene("""
            [gd_scene format=3]

            [node name="Player" type="CharacterBody2D"]
            speed = 200
            [node name="Sprite" type="Sprite2D" parent="."]
            texture = ExtResource("1_icon")
            """, "player.tscn");

        File.WriteAllText(Path.Combine(_root, "icon.svg"), "<svg/>");

        // Act
        var result = _tools.GetNode(scenePath, "Sprite");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Sprite", result!.Name);
        Assert.Equal("Sprite2D", result.Type);
        Assert.NotNull(result.Properties);
        Assert.Contains(result.Properties.Keys, k => k == "texture");
    }

    [Fact]
    public void GetNode_ReturnsNullForNonExistentNode()
    {
        // Arrange
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Root" type="Node"]
            """);

        // Act
        var result = _tools.GetNode(scenePath, "NonExistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetNode_RootNode_WhenPathIsDot()
    {
        // Arrange
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Root" type="Node"]
            """);

        // Act
        var result = _tools.GetNode(scenePath, ".");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Root", result!.Name);
    }

    [Fact]
    public void CreateNode_AddsNodeSuccessfully()
    {
        // Arrange
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Root" type="Node"]
            """);

        // Act
        var result = _tools.CreateNode(scenePath, "NewSprite", "Sprite2D");

        // Assert
        Assert.True(result.Changed);
        Assert.Equal(scenePath, result.ScenePath);
        Assert.Contains("Created node 'NewSprite'", result.Detail);

        // Verify the node was actually added
        var sceneContent = File.ReadAllText(Path.Combine(_root, "test.tscn"));
        Assert.Contains("[node name=\"NewSprite\" type=\"Sprite2D\" parent=\".\"]", sceneContent);
    }

    [Fact]
    public void CreateNode_WithCustomParentPath()
    {
        // Arrange
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Root" type="Node"]
            [node name="Parent" type="Node2D" parent="."]
            """);

        // Act
        var result = _tools.CreateNode(scenePath, "Child", "Sprite2D", "Parent");

        // Assert
        Assert.True(result.Changed);
        var sceneContent = File.ReadAllText(Path.Combine(_root, "test.tscn"));
        Assert.Contains("[node name=\"Child\" type=\"Sprite2D\" parent=\"./Parent\"]", sceneContent);
    }

    [Fact]
    public void RemoveNode_RemovesExistingNode()
    {
        // Arrange
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Root" type="Node"]
            [node name="ToRemove" type="Sprite2D" parent="."]
            """);

        // Act
        var result = _tools.RemoveNode(scenePath, "ToRemove");

        // Assert
        Assert.True(result.Changed);
        Assert.Equal(scenePath, result.ScenePath);
        Assert.Contains("Removed node 'ToRemove'", result.Detail);

        // Verify the node was actually removed
        var sceneContent = File.ReadAllText(Path.Combine(_root, "test.tscn"));
        Assert.DoesNotContain("[node name=\"ToRemove\"", sceneContent);
    }

    [Fact]
    public void RemoveNode_ReturnsFalseForNonExistentNode()
    {
        // Arrange
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Root" type="Node"]
            """);

        // Act
        var result = _tools.RemoveNode(scenePath, "NonExistent");

        // Assert
        Assert.False(result.Changed);
        Assert.Equal(scenePath, result.ScenePath);
        Assert.Contains("not found", result.Detail);
    }

    [Fact]
    public void RenameNode_ChangesNodeNameSuccessfully()
    {
        // Arrange
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="OldName" type="Node"]
            """);

        // Act
        var result = _tools.RenameNode(scenePath, "OldName", "NewName");

        // Assert
        Assert.True(result.Changed);
        Assert.Equal(scenePath, result.ScenePath);
        Assert.Contains("Renamed node 'OldName' to 'NewName'", result.Detail);

        // Verify the node was actually renamed
        var sceneContent = File.ReadAllText(Path.Combine(_root, "test.tscn"));
        Assert.Contains("[node name=\"NewName\" type=\"Node\"", sceneContent);
        Assert.DoesNotContain("[node name=\"OldName\"", sceneContent);
    }

    [Fact]
    public void ConnectSignal_AddsConnectionSuccessfully()
    {
        // Arrange
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Emitter" type="Button" parent="."]
            [node name="Receiver" type="Node" parent="."]
            """);

        // Act
        var result = _tools.ConnectSignal(scenePath, "pressed", ".", "./Receiver", "_on_button_pressed");

        // Assert
        Assert.True(result.Changed);
        Assert.Equal(scenePath, result.ScenePath);
        Assert.Contains("Connected ./pressed -> ./Receiver._on_button_pressed", result.Detail);

        // Verify the connection was actually added
        var sceneContent = File.ReadAllText(Path.Combine(_root, "test.tscn"));
        Assert.Contains("[connection signal=\"pressed\" from=\".\" to=\"./Receiver\" method=\"_on_button_pressed\"]", sceneContent);
    }

    [Fact]
    public void DisconnectSignal_RemovesExistingConnection()
    {
        // Arrange
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Emitter" type="Button" parent="."]
            [node name="Receiver" type="Node" parent="."]
            [connection signal="pressed" from="." to="./Receiver" method="_on_button_pressed"]
            """);

        // Act
        var result = _tools.DisconnectSignal(scenePath, "pressed", ".", "./Receiver", "_on_button_pressed");

        // Assert
        Assert.True(result.Changed);
        Assert.Equal(scenePath, result.ScenePath);
        Assert.Contains("Disconnected .pressed -> ./Receiver._on_button_pressed", result.Detail);

        // Verify the connection was actually removed
        var sceneContent = File.ReadAllText(Path.Combine(_root, "test.tscn"));
        Assert.DoesNotContain("[connection", sceneContent);
    }

    [Fact]
    public void SetNodeProperty_SetsPropertySuccessfully()
    {
        // Arrange
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Sprite" type="Sprite2D" parent="."]
            position = Vector2(0, 0)
            """);

        // Act
        var result = _tools.SetNodeProperty(scenePath, "Sprite", "position", "Vector2(100, 200)");

        // Assert
        Assert.True(result.Changed);
        Assert.Equal(scenePath, result.ScenePath);
        Assert.Contains("Set Sprite.position = Vector2(100, 200)", result.Detail);

        // Verify the property was actually set
        var sceneContent = File.ReadAllText(Path.Combine(_root, "test.tscn"));
        Assert.Contains("position = Vector2(100, 200)", sceneContent);
        Assert.DoesNotContain("position = Vector2(0, 0)", sceneContent);
    }

    [Fact]
    public void GetNodeProperty_ReturnsPropertyValue()
    {
        // Arrange
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Sprite" type="Sprite2D" parent="."]
            position = Vector2(50, 75)
            modulate = Color(1, 0.5, 0, 1)
            """);

        // Act
        var positionResult = _tools.GetNodeProperty(scenePath, "Sprite", "position");
        var colorResult = _tools.GetNodeProperty(scenePath, "Sprite", "modulate");

        // Assert
        Assert.Equal("Vector2(50, 75)", positionResult);
        Assert.Equal("Color(1, 0.5, 0, 1)", colorResult);
    }

    [Fact]
    public void GetNodeProperty_ReturnsNullForNonExistentProperty()
    {
        // Arrange
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Sprite" type="Sprite2D" parent="."]
            """);

        // Act
        var result = _tools.GetNodeProperty(scenePath, "Sprite", "non_existent_prop");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetNodeProperty_ReturnsNullForNonExistentNode()
    {
        // Arrange
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Root" type="Node"]
            """);

        // Act
        var result = _tools.GetNodeProperty(scenePath, "NonExistent", "position");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CreateNode_WithNullScenePath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _tools.CreateNode(null!, "Test", "Node"));
    }

    [Fact]
    public void GetNode_WithEmptyScenePath_ReturnsNull()
    {
        // Act
        var result = _tools.GetNode("", ".");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ConnectSignal_WithDuplicateConnection_IsNoOp()
    {
        // Arrange
        var scenePath = CreateTestScene("""
            [gd_scene format=3]
            [node name="Emitter" type="Button" parent="."]
            [node name="Receiver" type="Node" parent="."]
            [connection signal="pressed" from="." to="./Receiver" method="_on_button_pressed"]
            """);

        // Act
        var result = _tools.ConnectSignal(scenePath, "pressed", ".", "./Receiver", "_on_button_pressed");

        // Assert
        Assert.True(result.Changed); // Should still succeed as it's a no-op
        Assert.Equal(scenePath, result.ScenePath);

        // Verify connection count hasn't changed
        var sceneContent = File.ReadAllText(Path.Combine(_root, "test.tscn"));
        var connectionCount = System.Text.RegularExpressions.Regex.Matches(sceneContent, @"\[connection").Count;
        Assert.Equal(1, connectionCount);
    }
}