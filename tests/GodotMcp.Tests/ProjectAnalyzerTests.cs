using GodotMcp.Core.Project;
using GodotMcp.Core.Scenes;
using Xunit;

namespace GodotMcp.Tests;

public class ProjectAnalyzerTests : IDisposable
{
    readonly string _root;
    readonly GodotProject _project;
    readonly ProjectAnalyzer _analyzer;

    public ProjectAnalyzerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "godot-mcp-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "project.godot"), """
            config_version=5

            [application]

            config/name="Test"
            run/main_scene="res://main.tscn"
            """);
        File.WriteAllText(Path.Combine(_root, "main.tscn"), """
            [gd_scene load_steps=3 format=3]

            [ext_resource type="Script" path="res://player.gd" id="1_player"]
            [ext_resource type="PackedScene" path="res://enemy.tscn" id="2_enemy"]

            [node name="Main" type="Node2D"]

            [node name="Player" type="CharacterBody2D" parent="." groups=["actors"]]
            script = ExtResource("1_player")

            [node name="Enemy" parent="." instance=ExtResource("2_enemy")]
            """);
        File.WriteAllText(Path.Combine(_root, "enemy.tscn"), """
            [gd_scene load_steps=2 format=3]

            [ext_resource type="Script" path="res://player.gd" id="1_player"]

            [node name="Enemy" type="CharacterBody2D" groups=["actors"]]
            script = ExtResource("1_player")

            [node name="Sprite" type="Sprite2D" parent="."]
            """);
        File.WriteAllText(Path.Combine(_root, "player.gd"), "extends CharacterBody2D\n");
        File.WriteAllText(Path.Combine(_root, "unused.tres"), "[gd_resource type=\"Resource\" format=3]\n");
        _project = GodotProject.Open(_root);
        _analyzer = new ProjectAnalyzer(_project);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void FindNodes_ByType_SearchesAllScenes()
    {
        var matches = _analyzer.FindNodes(type: "CharacterBody2D");
        Assert.Equal(2, matches.Count);
        Assert.Contains(matches, m => m.ScenePath == "res://main.tscn" && m.NodePath == "Player");
        Assert.Contains(matches, m => m.ScenePath == "res://enemy.tscn" && m.NodePath == ".");
    }

    [Fact]
    public void FindNodes_ByWildcardName()
    {
        var matches = _analyzer.FindNodes(namePattern: "*prite*");
        Assert.Single(matches);
        Assert.Equal("Sprite", matches[0].Name);
    }

    [Fact]
    public void FindNodes_ByGroup()
    {
        var matches = _analyzer.FindNodes(group: "actors");
        Assert.Equal(2, matches.Count);
    }

    [Fact]
    public void FindNodes_CombinedFilters()
    {
        var matches = _analyzer.FindNodes(type: "CharacterBody2D", namePattern: "Player");
        Assert.Single(matches);
        Assert.Equal("res://main.tscn", matches[0].ScenePath);
    }

    [Fact]
    public void FindScriptUsages_ReportsAllAttachments()
    {
        var usages = _analyzer.FindScriptUsages("res://player.gd");
        Assert.Equal(2, usages.Count);
        Assert.Contains(usages, u => u.ScenePath == "res://main.tscn" && u.NodePath == "Player");
        Assert.Contains(usages, u => u.ScenePath == "res://enemy.tscn" && u.NodePath == ".");
    }

    [Fact]
    public void FindOrphanResources_FlagsUnreferencedOnly()
    {
        var orphans = _analyzer.FindOrphanResources();
        Assert.Contains("res://unused.tres", orphans);
        Assert.DoesNotContain("res://main.tscn", orphans);
        Assert.DoesNotContain("res://enemy.tscn", orphans);
        Assert.DoesNotContain("res://player.gd", orphans);
    }

    [Fact]
    public void FindOrphanResources_HonorsScriptResLiterals()
    {
        File.WriteAllText(Path.Combine(_root, "loot.tres"), "[gd_resource type=\"Resource\" format=3]\n");
        File.WriteAllText(Path.Combine(_root, "player.gd"), "extends CharacterBody2D\n\nvar loot = preload(\"res://loot.tres\")\n");
        var orphans = _analyzer.FindOrphanResources();
        Assert.DoesNotContain("res://loot.tres", orphans);
    }

    [Fact]
    public void GetSceneDependencies_ListsExternalResources()
    {
        var deps = _analyzer.GetSceneDependencies("res://main.tscn");
        Assert.Equal(2, deps.Count);
        Assert.Contains(deps, d => d.Path == "res://enemy.tscn" && d.IsScene);
        Assert.Contains(deps, d => d.Path == "res://player.gd" && d.IsScript);
    }

    [Fact]
    public void SceneQuery_FindByType_Substring()
    {
        var tree = _project.LoadScene("res://enemy.tscn");
        Assert.Single(SceneQuery.FindByType(tree, "sprite", exact: false));
        Assert.Empty(SceneQuery.FindByType(tree, "sprite", exact: true));
        Assert.Single(SceneQuery.FindByType(tree, "Sprite2D"));
    }

    [Theory]
    [InlineData("Player", "Pl*", true)]
    [InlineData("Player", "*ayer", true)]
    [InlineData("Player", "P??yer", true)]
    [InlineData("Player", "player", true)]
    [InlineData("Player", "Enemy*", false)]
    [InlineData("Player", "*", true)]
    public void MatchesWildcard(string text, string pattern, bool expected) =>
        Assert.Equal(expected, SceneQuery.MatchesWildcard(text, pattern));

    [Fact]
    public void SceneDiagram_Mermaid_ContainsNodesAndEdges()
    {
        var tree = _project.LoadScene("res://main.tscn");
        var mermaid = SceneDiagram.ToMermaid(tree);
        Assert.StartsWith("graph TD", mermaid);
        Assert.Contains("Main", mermaid);
        Assert.Contains("-->", mermaid);
    }

    [Fact]
    public void SceneDiagram_Ascii_ShowsHierarchy()
    {
        var tree = _project.LoadScene("res://enemy.tscn");
        var ascii = SceneDiagram.ToAsciiTree(tree);
        Assert.Contains("Enemy (CharacterBody2D)", ascii);
        Assert.Contains("└── Sprite (Sprite2D)", ascii);
    }

    [Fact]
    public void SceneLinter_FlagsDefaultAndDuplicateNames()
    {
        File.WriteAllText(Path.Combine(_root, "messy.tscn"), """
            [gd_scene format=3]

            [node name="Main" type="Node2D"]

            [node name="Sprite2D" type="Sprite2D" parent="."]

            [node name="My Node" type="Node2D" parent="."]
            """);
        var issues = SceneLinter.Lint(_project.LoadScene("res://messy.tscn"));
        Assert.Contains(issues, i => i.Code == "default-name");
        Assert.Contains(issues, i => i.Code == "name-with-spaces");
    }

    [Fact]
    public void SceneLinter_FlagsHandlerWithoutScript()
    {
        File.WriteAllText(Path.Combine(_root, "nohandler.tscn"), """
            [gd_scene format=3]

            [node name="Main" type="Node2D"]

            [node name="Button" type="Button" parent="."]

            [connection signal="pressed" from="Button" to="." method="_on_pressed"]
            """);
        var issues = SceneLinter.Lint(_project.LoadScene("res://nohandler.tscn"));
        Assert.Contains(issues, i => i.Code == "handler-without-script");
    }
}
