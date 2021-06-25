using GodotMcp.Core.Project;

namespace GodotMcp.Server;

public sealed class ProjectLocator(string defaultRoot)
{
    public string DefaultRoot { get; } = defaultRoot;

    public GodotProject Resolve(string? projectPath = null) =>
        GodotProject.Open(string.IsNullOrWhiteSpace(projectPath) ? DefaultRoot : projectPath);
}
