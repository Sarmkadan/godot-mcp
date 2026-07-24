using GodotMcp.Core.Project;
using Xunit;

namespace GodotMcp.Tests;

public sealed class PathResolverTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PathResolver _resolver;

    public PathResolverTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _resolver = new PathResolver(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, true);
        }
        catch
        {
            // Best effort cleanup
        }
    }

    [Fact]
    public void Resolve_ResPath_ReturnsAbsolutePathWithinProject()
    {
        // Arrange
        var subDir = Path.Combine(_tempDir, "scripts");
        Directory.CreateDirectory(subDir);
        var resPath = "res://scripts/player.gd";

        // Act
        var absolutePath = _resolver.Resolve(resPath);

        // Assert
        Assert.StartsWith(_tempDir, absolutePath);
        Assert.EndsWith("scripts/player.gd", absolutePath.Replace('\\', '/'));
        Assert.Contains("scripts", absolutePath);
    }

    [Fact]
    public void Resolve_PathWithParentDirectory_ThrowsArgumentException()
    {
        // Arrange
        var maliciousPath = "res://scripts/../../../etc/passwd";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _resolver.Resolve(maliciousPath));
        Assert.Contains("outside the project root", exception.Message);
        Assert.Contains("traversal", exception.Message);
    }

    [Fact]
    public void Resolve_PathWithDoubleDots_ThrowsArgumentException()
    {
        // Arrange
        var maliciousPath = "res://../scripts/player.gd";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _resolver.Resolve(maliciousPath));
        Assert.Contains("outside the project root", exception.Message);
    }

    [Fact]
    public void Resolve_AbsolutePathOutsideProject_ThrowsArgumentException()
    {
        // Arrange
        var absolutePath = "/etc/passwd";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _resolver.Resolve(absolutePath));
        Assert.Contains("outside the project root", exception.Message);
    }

    [Fact]
    public void Resolve_EmptyPath_ThrowsArgumentException()
    {
        // Arrange
        var emptyPath = "";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _resolver.Resolve(emptyPath));
    }

    [Fact]
    public void Resolve_NullPath_ThrowsArgumentNullException()
    {
        // Arrange
        string? nullPath = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _resolver.Resolve(nullPath));
    }

    [Fact]
    public void ToResPath_AbsolutePathWithinProject_ReturnsResPath()
    {
        // Arrange
        var subDir = Path.Combine(_tempDir, "scripts");
        Directory.CreateDirectory(subDir);
        var absolutePath = Path.Combine(subDir, "player.gd");
        File.WriteAllText(absolutePath, "# test");

        // Act
        var resPath = _resolver.ToResPath(absolutePath);

        // Assert
        Assert.Equal("res://scripts/player.gd", resPath);

        // Cleanup
        File.Delete(absolutePath);
    }

    [Fact]
    public void ToResPath_PathOutsideProject_ThrowsArgumentException()
    {
        // Arrange
        var outsidePath = Path.Combine(Path.GetTempPath(), "outside", "file.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(outsidePath)!);
        File.WriteAllText(outsidePath, "test");

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _resolver.ToResPath(outsidePath));
        Assert.Contains("outside the project root", exception.Message);

        // Cleanup
        File.Delete(outsidePath);
        Directory.Delete(Path.GetDirectoryName(outsidePath)!);
    }

    [Fact]
    public void IsPathSafe_MaliciousPaths_ReturnsFalse()
    {
        // Arrange
        var maliciousPaths = new[] { "res://scripts/../../../etc/passwd", "../../../etc/passwd",
                                   "res://../scripts/player.gd", "scripts/../../player.gd",
                                   "/etc/passwd" };

        // Act & Assert
        foreach (var path in maliciousPaths)
        {
            Assert.False(_resolver.IsPathSafe(path));
        }
    }

    [Fact]
    public void RootPath_ReturnsProjectRoot()
    {
        // Act
        var rootPath = _resolver.RootPath;

        // Assert
        Assert.Equal(_tempDir, rootPath);
        Assert.StartsWith(_tempDir, rootPath);
    }
}