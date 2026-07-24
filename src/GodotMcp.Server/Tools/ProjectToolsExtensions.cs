using System.ComponentModel;
using GodotMcp.Core.Model;

namespace GodotMcp.Server.Tools;

public static class ProjectToolsExtensions
{
    /// <summary>
    /// Filters script files by language (C# or GDScript).
    /// </summary>
    /// <param name="tools">The ProjectTools instance</param>
    /// <param name="language">The language to filter by ("C#" or "GDScript")</param>
    /// <param name="projectPath">Optional project path; omit for the configured default</param>
    /// <returns>List of script paths matching the specified language</returns>
    /// <exception cref="ArgumentNullException">Thrown when tools is null</exception>
    public static IReadOnlyList<string> ListScriptsByLanguage(this ProjectTools tools, string language, string? projectPath = null)
    {
        ArgumentNullException.ThrowIfNull(tools);
        ArgumentException.ThrowIfNullOrEmpty(language);

        var allScripts = tools.ListScripts(projectPath);

        return language switch
        {
            "C#" => allScripts.Where(s => s.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).ToList(),
            "GDScript" => allScripts.Where(s => s.EndsWith(".gd", StringComparison.OrdinalIgnoreCase)).ToList(),
            _ => allScripts
        };
    }

    /// <summary>
    /// Finds all scripts that inherit from a specific base type.
    /// </summary>
    /// <param name="tools">The ProjectTools instance</param>
    /// <param name="baseType">The base type to search for (e.g., "Node", "CharacterBody2D", "Area2D")</param>
    /// <param name="projectPath">Optional project path; omit for the configured default</param>
    /// <returns>List of script paths with the specified base type</returns>
    /// <exception cref="ArgumentNullException">Thrown when tools is null</exception>
    public static IReadOnlyList<string> ListScriptsWithBaseType(this ProjectTools tools, string baseType, string? projectPath = null)
    {
        ArgumentNullException.ThrowIfNull(tools);
        ArgumentException.ThrowIfNullOrEmpty(baseType);

        var scripts = tools.ListScripts(projectPath);
        var result = new List<string>();

        foreach (var scriptPath in scripts)
        {
            var scriptInfo = tools.InspectScript(scriptPath, projectPath);
            if (scriptInfo.BaseType?.Equals(baseType, StringComparison.OrdinalIgnoreCase) == true)
            {
                result.Add(scriptPath);
            }
        }

        return result;
    }

    /// <summary>
    /// Checks if the project uses specific features.
    /// </summary>
    /// <param name="tools">The ProjectTools instance</param>
    /// <param name="feature">The feature to check for</param>
    /// <param name="projectPath">Optional project path; omit for the configured default</param>
    /// <returns>True if the project uses the feature; otherwise false</returns>
    /// <exception cref="ArgumentNullException">Thrown when tools is null</exception>
    public static bool HasFeature(this ProjectTools tools, string feature, string? projectPath = null)
    {
        ArgumentNullException.ThrowIfNull(tools);
        ArgumentException.ThrowIfNullOrEmpty(feature);

        var projectInfo = tools.ProjectInfo(projectPath);
        return projectInfo.Features.Contains(feature, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the list of autoload singletons in the project.
    /// </summary>
    /// <param name="tools">The ProjectTools instance</param>
    /// <param name="projectPath">Optional project path; omit for the configured default</param>
    /// <returns>List of autoload singleton paths</returns>
    /// <exception cref="ArgumentNullException">Thrown when tools is null</exception>
    public static IReadOnlyList<string> ListAutoloads(this ProjectTools tools, string? projectPath = null)
    {
        ArgumentNullException.ThrowIfNull(tools);

        var projectInfo = tools.ProjectInfo(projectPath);
        return projectInfo.Autoloads;
    }
}