using GodotMcp.Core.Parsing;
using Xunit;

namespace GodotMcp.Tests;

public class TscnParserTests
{
    const string SampleScene = """
        [gd_scene load_steps=3 format=3 uid="uid://c4f8x2y1z3w5v"]

        [ext_resource type="Script" path="res://player.gd" id="1_player"]
        [ext_resource type="Texture2D" path="res://icon.svg" id="2_icon"]

        [sub_resource type="RectangleShape2D" id="RectangleShape2D_1"]
        size = Vector2(32, 48)

        [node name="Player" type="CharacterBody2D"]
        script = ExtResource("1_player")
        speed = 220.5
        lives = 3
        invincible = false

        [node name="Sprite" type="Sprite2D" parent="."]
        texture = ExtResource("2_icon")
        position = Vector2(0, -8)

        [node name="Hitbox" type="CollisionShape2D" parent="."]
        shape = SubResource("RectangleShape2D_1")

        [connection signal="body_entered" from="." to="." method="_on_body_entered"]
        """;

    [Fact]
    public void Parse_ReadsDescriptorAttributes()
    {
        var doc = TscnParser.Parse(SampleScene);
        Assert.True(doc.IsScene);
        Assert.False(doc.IsResource);
        Assert.Equal(3, doc.Format);
        Assert.Equal("uid://c4f8x2y1z3w5v", doc.Uid);
        Assert.Equal(3, doc.Descriptor.GetAttributeInt("load_steps"));
    }

    [Fact]
    public void Parse_ReadsExtResources()
    {
        var doc = TscnParser.Parse(SampleScene);
        Assert.Equal(2, doc.ExtResources.Count());
        var script = doc.FindExtResource("1_player");
        Assert.NotNull(script);
        Assert.Equal("Script", script.GetAttributeString("type"));
        Assert.Equal("res://player.gd", script.GetAttributeString("path"));
    }

    [Fact]
    public void Parse_ReadsSubResourceProperties()
    {
        var doc = TscnParser.Parse(SampleScene);
        var shape = doc.FindSubResource("RectangleShape2D_1");
        Assert.NotNull(shape);
        var size = Assert.IsType<GodotConstructor>(shape.GetProperty("size"));
        Assert.Equal("Vector2", size.Name);
        Assert.Equal([new GodotInt(32), new GodotInt(48)], size.Arguments);
    }

    [Fact]
    public void Parse_ReadsNodesWithTypedProperties()
    {
        var doc = TscnParser.Parse(SampleScene);
        Assert.Equal(3, doc.Nodes.Count());
        var player = doc.Nodes.First();
        Assert.Equal("Player", player.GetAttributeString("name"));
        Assert.Equal("CharacterBody2D", player.GetAttributeString("type"));
        Assert.Null(player.GetAttributeString("parent"));
        Assert.Equal(new GodotFloat(220.5), player.GetProperty("speed"));
        Assert.Equal(new GodotInt(3), player.GetProperty("lives"));
        Assert.Equal(new GodotBool(false), player.GetProperty("invincible"));
        var script = Assert.IsType<GodotConstructor>(player.GetProperty("script"));
        Assert.True(script.IsExtResource);
        Assert.Equal("1_player", script.ReferenceId);
    }

    [Fact]
    public void Parse_ReadsConnections()
    {
        var doc = TscnParser.Parse(SampleScene);
        var conn = Assert.Single(doc.Connections);
        Assert.Equal("body_entered", conn.GetAttributeString("signal"));
        Assert.Equal("_on_body_entered", conn.GetAttributeString("method"));
    }

    [Fact]
    public void Parse_RoundTripsThroughSerialize()
    {
        var doc = TscnParser.Parse(SampleScene);
        var reparsed = TscnParser.Parse(doc.Serialize());
        Assert.Equal(doc.Sections.Count, reparsed.Sections.Count);
        Assert.Equal(doc.Serialize(), reparsed.Serialize());
    }

    [Fact]
    public void Parse_ThrowsOnMissingDescriptor()
    {
        Assert.Throws<GodotParseException>(() => TscnParser.Parse("name = \"orphan\""));
    }

    [Fact]
    public void Parse_SkipsComments()
    {
        var doc = TscnParser.Parse("""
            ; leading comment
            [gd_resource type="Theme" format=3]
            ; property comment
            default_font_size = 16
            """);
        Assert.True(doc.IsResource);
        Assert.Equal(new GodotInt(16), doc.Descriptor.GetProperty("default_font_size"));
    }

    [Fact]
    public void Parse_HandlesQuotedPropertyKeys()
    {
        var doc = TscnParser.Parse("""
            [gd_resource type="Theme" format=3]
            "Button/colors/font_color" = Color(1, 1, 1, 1)
            """);
        Assert.NotNull(doc.Descriptor.GetProperty("Button/colors/font_color"));
    }

    [Fact]
    public void Parse_HandlesArraysAndDictionaries()
    {
        var doc = TscnParser.Parse("""
            [gd_scene format=3]
            [node name="Root" type="Node"]
            tags = ["enemy", "boss"]
            stats = {
            "hp": 100,
            "name": "slime"
            }
            """);
        var node = doc.Nodes.Single();
        var tags = Assert.IsType<GodotArray>(node.GetProperty("tags"));
        Assert.Equal([new GodotString("enemy"), new GodotString("boss")], tags.Items);
        var stats = Assert.IsType<GodotDictionary>(node.GetProperty("stats"));
        Assert.Equal(new GodotInt(100), stats["hp"]);
        Assert.Equal(new GodotString("slime"), stats["name"]);
    }
}

public class GodotValueTests
{
    [Theory]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("42")]
    [InlineData("-17")]
    [InlineData("3.5")]
    [InlineData("\"hello\"")]
    [InlineData("&\"signal_name\"")]
    [InlineData("^\"../Parent/Child\"")]
    [InlineData("Vector2(1.5, -2.5)")]
    [InlineData("[1, 2, 3]")]
    public void Parse_WriteRoundTripsExactly(string text)
    {
        Assert.Equal(text, GodotValue.Parse(text).ToTscnString());
    }

    [Fact]
    public void Parse_ReadsEscapedStrings()
    {
        var value = Assert.IsType<GodotString>(GodotValue.Parse("\"line1\\nline2 \\\"quoted\\\"\""));
        Assert.Equal("line1\nline2 \"quoted\"", value.Value);
        Assert.Equal("\"line1\\nline2 \\\"quoted\\\"\"", value.ToTscnString());
    }

    [Fact]
    public void GodotFloat_WritesIntegralValuesWithDecimalPoint()
    {
        Assert.Equal("2.0", new GodotFloat(2).ToTscnString());
    }

    [Fact]
    public void GodotFloat_WritesSpecialValues()
    {
        Assert.Equal("inf", new GodotFloat(double.PositiveInfinity).ToTscnString());
        Assert.Equal("-inf", new GodotFloat(double.NegativeInfinity).ToTscnString());
        Assert.Equal("nan", new GodotFloat(double.NaN).ToTscnString());
    }

    [Fact]
    public void ConstructorHelpers_ProduceExpectedTscn()
    {
        Assert.Equal("ExtResource(\"1_tex\")", GodotConstructor.ExtResource("1_tex").ToTscnString());
        Assert.Equal("Vector3(1.0, 2.0, 3.0)", GodotConstructor.Vector3(1, 2, 3).ToTscnString());
        Assert.Equal("Color(1.0, 0.5, 0.0, 1.0)", GodotConstructor.Color(1, 0.5, 0).ToTscnString());
    }
}
