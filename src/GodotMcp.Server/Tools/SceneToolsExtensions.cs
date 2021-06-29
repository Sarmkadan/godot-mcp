using System.ComponentModel;
using GodotMcp.Server;

namespace GodotMcp.Server.Tools;

/// <summary>
/// Provides extension methods for the <see cref="SceneTools"/> class.
/// </summary>
public static class SceneToolsExtensions
{
    /// <summary>
    /// Checks if a node exists in the scene.
    /// </summary>
    /// <param name="tools">The SceneTools instance.</param>
    /// <param name="scenePath">The path to the .tscn file.</param>
    /// <param name="nodePath">The path to the node.</param>
    /// <param name="projectPath">Optional project path.</param>
    /// <returns>True if the node exists, false otherwise.</returns>
    /// <exception cref="ArgumentException">Thrown when scenePath or nodePath is null or empty.</exception>
    public static bool NodeExists(this SceneTools tools, string scenePath, string nodePath, string? projectPath = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(scenePath);
        ArgumentException.ThrowIfNullOrEmpty(nodePath);
        return tools.GetNode(scenePath, nodePath, projectPath) is not null;
    }

    /// <summary>
    /// Gets the groups of a node.
    /// </summary>
    /// <param name="tools">The SceneTools instance.</param>
    /// <param name="scenePath">The path to the .tscn file.</param>
    /// <param name="nodePath">The path to the node.</param>
    /// <param name="projectPath">Optional project path.</param>
    /// <returns>A read-only list of group names.</returns>
    /// <exception cref="ArgumentException">Thrown when scenePath or nodePath is null or empty.</exception>
    public static IReadOnlyList<string> GetNodeGroups(this SceneTools tools, string scenePath, string nodePath, string? projectPath = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(scenePath);
        ArgumentException.ThrowIfNullOrEmpty(nodePath);
        return tools.GetNode(scenePath, nodePath, projectPath)?.Groups ?? Array.Empty<string>();
    }

    /// <summary>
    /// Gets the parent path of a node.
    /// </summary>
    /// <param name="tools">The SceneTools instance.</param>
    /// <param name="scenePath">The path to the .tscn file.</param>
    /// <param name="nodePath">The path to the node.</param>
    /// <param name="projectPath">Optional project path.</param>
    /// <returns>The parent path, or null if the node doesn't exist or is the root.</returns>
    /// <exception cref="ArgumentException">Thrown when scenePath or nodePath is null or empty.</exception>
    public static string? GetNodeParentPath(this SceneTools tools, string scenePath, string nodePath, string? projectPath = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(scenePath);
        ArgumentException.ThrowIfNullOrEmpty(nodePath);
        return tools.GetNode(scenePath, nodePath, projectPath)?.Parent;
    }
}
