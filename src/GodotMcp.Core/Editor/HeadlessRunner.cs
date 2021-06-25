using GodotMcp.Core.Project;

namespace GodotMcp.Core.Editor;

public sealed class HeadlessRunner(GodotProject project, GodotExecutable executable)
{
    public GodotProject Project { get; } = project;
    public GodotExecutable Executable { get; } = executable;
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(2);

    public Task<GodotRunResult> RunProjectAsync(string? scene = null, IReadOnlyList<string>? extraArgs = null, Action<string>? onOutputLine = null, CancellationToken cancellationToken = default)
    {
        var args = new List<string> { "--headless", "--path", Project.RootPath };
        if (scene is not null) args.Add(scene);
        if (extraArgs is not null) args.AddRange(extraArgs);
        return Executable.RunAsync(args, Project.RootPath, DefaultTimeout, onOutputLine, cancellationToken);
    }

    public Task<GodotRunResult> RunScriptAsync(string scriptResPath, Action<string>? onOutputLine = null, CancellationToken cancellationToken = default) =>
        Executable.RunAsync(["--headless", "--path", Project.RootPath, "--script", scriptResPath], Project.RootPath, DefaultTimeout, onOutputLine, cancellationToken);

    public Task<GodotRunResult> ImportResourcesAsync(CancellationToken cancellationToken = default) =>
        Executable.RunAsync(["--headless", "--path", Project.RootPath, "--import"], Project.RootPath, DefaultTimeout, null, cancellationToken);

    public Task<GodotRunResult> QuitAfterFramesAsync(int frames, CancellationToken cancellationToken = default) =>
        Executable.RunAsync(["--headless", "--path", Project.RootPath, "--quit-after", frames.ToString()], Project.RootPath, DefaultTimeout, null, cancellationToken);
}
