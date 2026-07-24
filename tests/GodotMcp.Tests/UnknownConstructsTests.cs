using GodotMcp.Core.Parsing;
using Xunit;

namespace GodotMcp.Tests;

public class UnknownConstructsTests
{
    const string SceneWithUnknownSection = """
[gd_scene format=3]

[node name="Main" type="Node2D"]

; This is a custom section that the parser doesn't understand
[custom_data some_id="custom123"]
custom_value = "should be preserved"

[ext_resource type="Script" path="res://test.gd" id="1_test"]
""";

    const string SceneWithUnknownPropertyFormat = """
[gd_scene format=3]

[node name="Main" type="Node2D"]
; This property format is not recognized by the parser
unknown_property = SomeCustomType(1, 2, 3)

[ext_resource type="Texture2D" path="res://icon.png" id="2_icon"]
""";

    const string SceneWithCustomResource = """
[gd_scene format=3]

[ext_resource type="Script" path="res://test.gd" id="1_test"]

[sub_resource type="CustomResource" id="Custom_1"]
custom_field = "value"

[node name="Main" type="Node2D"]
""";

    [Fact]
    public void Parse_PreservesUnknownSectionsThroughRoundTrip()
    {
        var doc = TscnParser.Parse(SceneWithUnknownSection);
        var serialized = doc.Serialize();
        var reparsed = TscnParser.Parse(serialized);

        // Should have the same number of sections
        Assert.Equal(doc.Sections.Count, reparsed.Sections.Count);

        // The unknown section should be preserved
        var originalUnknown = doc.Sections.FirstOrDefault(s => s.Name == "custom_data");
        var reparsedUnknown = reparsed.Sections.FirstOrDefault(s => s.Name == "custom_data");
        Assert.NotNull(originalUnknown);
        Assert.NotNull(reparsedUnknown);

        // Properties should be preserved
        Assert.Equal(originalUnknown.Properties.Count, reparsedUnknown.Properties.Count);
    }

    [Fact]
    public void Parse_PreservesUnknownPropertyFormatsThroughRoundTrip()
    {
        var doc = TscnParser.Parse(SceneWithUnknownPropertyFormat);
        var serialized = doc.Serialize();
        var reparsed = TscnParser.Parse(serialized);

        var node = doc.Nodes.First();
        var reparsedNode = reparsed.Nodes.First();

        // The unknown property should be preserved as a raw value
        var originalProp = node.GetProperty("unknown_property");
        var reparsedProp = reparsedNode.GetProperty("unknown_property");

        Assert.NotNull(originalProp);
        Assert.NotNull(reparsedProp);
    }

    [Fact]
    public void Parse_PreservesCustomResourceSections()
    {
        var doc = TscnParser.Parse(SceneWithCustomResource);
        var serialized = doc.Serialize();
        var reparsed = TscnParser.Parse(serialized);

        var originalSub = doc.FindSubResource("Custom_1");
        var reparsedSub = reparsed.FindSubResource("Custom_1");

        Assert.NotNull(originalSub);
        Assert.NotNull(reparsedSub);
        Assert.Equal(originalSub.Properties.Count, reparsedSub.Properties.Count);
    }
}