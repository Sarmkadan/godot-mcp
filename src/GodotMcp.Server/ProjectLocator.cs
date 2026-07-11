using GodotMcp.Core.Project;

namespace GodotMcp.Server;

public sealed class ProjectLocator
{
    public ProjectLocator(string defaultRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultRoot);
        DefaultRoot = defaultRoot;
    }

    public string DefaultRoot { get; }

    public GodotProject Resolve(string? projectPath = null) =>
        GodotProject.Open(string.IsNullOrWhiteSpace(projectPath) ? DefaultRoot : projectPath);
}
