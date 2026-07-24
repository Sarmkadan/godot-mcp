using GodotMcp.Core.Parsing;
using Xunit;

namespace GodotMcp.Tests;

public class RoundTripPreservationTests
{
    const string SceneWithRawValue = """
[gd_scene format=3]

[node name="Main" type="Node2D"]
; This is a comment that should be preserved
position = Vector2(100.0, 200.0)
custom_array = PackedVector2Array(1, 2, 3, 4, 5)

[ext_resource type="Script" path="res://test.gd" id="1_test"]
""";

    const string SceneWithExoticNodePath = """
[gd_scene format=3]

[node name="Main" type="Node2D"]
path_with_percent = @"res://scenes/%Main.tscn"
unique_node_path = "%Node2D_12345"

[ext_resource type="Texture2D" path="res://icon.png" id="2_icon"]
""";

    const string SceneWithMixedFormats = """
[gd_scene format=3 uid="uid://test123"]

; Header comment
[ext_resource type="Script" path="res://test.gd" id="1_test"]

[sub_resource type="RectangleShape2D" id="Shape_1"]
size = Vector2(32.0, 48.0)

[node name="Player" type="CharacterBody2D" parent="."]
position = Vector2(64.0, 64.0)
script = ExtResource("1_test")

[connection signal="body_entered" from="." to="." method="_on_body_entered"]

; Footer comment
""";

    [Fact]
    public void Parse_PreservesExactSerializationThroughRoundTrip()
    {
        var doc = TscnParser.Parse(SceneWithRawValue);
        var serialized = doc.Serialize();
        var reparsed = TscnParser.Parse(serialized);

        // The serialized output should be byte-for-byte identical to the reparsed input
        Assert.Equal(SceneWithRawValue.Trim(), serialized.Trim());
    }

    [Fact]
    public void Parse_PreservesPercentInNodePaths()
    {
        var doc = TscnParser.Parse(SceneWithExoticNodePath);
        var serialized = doc.Serialize();
        var reparsed = TscnParser.Parse(serialized);

        var node = doc.Nodes.First();
        var reparsedNode = reparsed.Nodes.First();

        var originalPath = node.GetProperty("path_with_percent");
        var reparsedPath = reparsedNode.GetProperty("path_with_percent");
        var originalUnique = node.GetProperty("unique_node_path");
        var reparsedUnique = reparsedNode.GetProperty("unique_node_path");

        Assert.Equal(originalPath?.ToTscnString(), reparsedPath?.ToTscnString());
        Assert.Equal(originalUnique?.ToTscnString(), reparsedUnique?.ToTscnString());
        Assert.Equal(SceneWithExoticNodePath.Trim(), serialized.Trim());
    }

    [Fact]
    public void Parse_PreservesCommentsAndWhitespaceThroughRoundTrip()
    {
        var doc = TscnParser.Parse(SceneWithMixedFormats);
        var serialized = doc.Serialize();
        var reparsed = TscnParser.Parse(serialized);

        // Should preserve the exact structure
        Assert.Equal(doc.Descriptor.Name, reparsed.Descriptor.Name);
        Assert.Equal(doc.Format, reparsed.Format);
        Assert.Equal(doc.Uid, reparsed.Uid);
        Assert.Equal(doc.Sections.Count, reparsed.Sections.Count);

        // The serialized output should preserve the original formatting
        Assert.Equal(SceneWithMixedFormats.Trim(), serialized.Trim());
    }

    [Fact]
    public void Parse_HandlesComplexGodotTypes()
    {
        const string complexScene = """
[gd_scene format=3]

[node name="Main" type="Node2D"]
array_value = [1, 2, Vector2(10, 20), "string", true]
dict_value = {"key1": 123, "key2": Vector3(1, 2, 3), "nested": {"inner": "value"}}
constructor_value = PackedScene("res://other.tscn")

[ext_resource type="Script" path="res://test.gd" id="1_test"]
""";

        var doc = TscnParser.Parse(complexScene);
        var serialized = doc.Serialize();
        var reparsed = TscnParser.Parse(serialized);

        var node = doc.Nodes.First();
        var reparsedNode = reparsed.Nodes.First();

        Assert.Equal(complexScene.Trim(), serialized.Trim());
        Assert.Equal(node.Properties.Count, reparsedNode.Properties.Count);
    }
}