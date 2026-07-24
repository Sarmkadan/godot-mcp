// SPDX-License-Identifier: MIT
// Copyright: RedRocket

using System;
using GodotMcp.Core.Parsing;
using GodotMcp.Core.Scenes;
using Xunit;

namespace GodotMcp.Tests;

public sealed class SceneTreeBuilderTests
{
    // A minimal .tscn document containing a single root node.
    private const string MinimalScene = @"
[gd_scene load_steps=1 format=2]

[node name=""Root"" type=""Node""]
";

    [Fact]
    public void Build_WithMinimalDocument_ReturnsTreeWithRoot()
    {
        // Arrange
        var doc = TscnParser.Parse(MinimalScene);
        const string scenePath = "res://minimal.tscn";

        // Act
        var tree = SceneTreeBuilder.Build(doc, scenePath);

        // Assert
        Assert.NotNull(tree);
        Assert.Equal(scenePath, tree.ScenePath);
        Assert.NotNull(tree.Root);
        Assert.Equal("Root", tree.Root.Name);
        Assert.Null(tree.Root.Parent); // root has no parent
    }

    [Fact]
    public void Build_WithEmptyDocument_ReturnsTreeWithNullRoot()
    {
        // Arrange: an empty scene (no [node] sections)
        var emptyDoc = TscnParser.Parse("[gd_scene format=2]");
        const string scenePath = "res://empty.tscn";

        // Act
        var tree = SceneTreeBuilder.Build(emptyDoc, scenePath);

        // Assert
        Assert.NotNull(tree);
        Assert.Equal(scenePath, tree.ScenePath);
        Assert.Null(tree.Root);
    }

    [Fact]
    public void Build_NullDocument_ThrowsNullReferenceException()
    {
        // Arrange
        TscnDocument? doc = null;
        const string scenePath = "res://null.tscn";

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => SceneTreeBuilder.Build(doc!, scenePath));
    }

    [Fact]
    public void FindNodeSection_RootPath_ReturnsRootSection()
    {
        // Arrange
        var doc = TscnParser.Parse(MinimalScene);

        // Act
        var section = SceneTreeBuilder.FindNodeSection(doc, ".");

        // Assert
        Assert.NotNull(section);
        Assert.Equal("Root", section!.GetAttributeString("name"));
    }

    [Fact]
    public void FindNodeSection_NonExistingPath_ReturnsNull()
    {
        // Arrange
        var doc = TscnParser.Parse(MinimalScene);

        // Act
        var section = SceneTreeBuilder.FindNodeSection(doc, "NonExistent/Node");

        // Assert
        Assert.Null(section);
    }

    [Fact]
    public void FindNodeSection_NullDocument_ThrowsNullReferenceException()
    {
        // Arrange
        TscnDocument? doc = null;

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => SceneTreeBuilder.FindNodeSection(doc!, "any"));
    }

    [Theory]
    [InlineData("name")]
    [InlineData("type")]
    [InlineData("parent")]
    [InlineData("instance")]
    public void IsHeaderKey_KnownKey_ReturnsTrue(string key)
    {
        Assert.True(SceneTreeBuilder.IsHeaderKey(key));
    }

    [Theory]
    [InlineData("foo")]
    [InlineData("")]
    [InlineData(null)]
    public void IsHeaderKey_UnknownKey_ReturnsFalse(string? key)
    {
        // The method expects a non‑null string; passing null will simply return false
        // because HeaderKeys does not contain it.
        Assert.False(SceneTreeBuilder.IsHeaderKey(key ?? string.Empty));
    }
}
