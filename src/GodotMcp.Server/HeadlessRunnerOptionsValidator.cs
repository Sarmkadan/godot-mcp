using Microsoft.Extensions.Options;

namespace GodotMcp.Server;

/// <summary>
/// Validates the <see cref="HeadlessRunnerOptions"/> rules that need the file system or per-element checks:
/// the Godot binary (when set) must be an existing executable file, and every allowed project root must be non-blank.
/// </summary>
public sealed class HeadlessRunnerOptionsValidator : IValidateOptions<HeadlessRunnerOptions>
{
    public ValidateOptionsResult Validate(string? name, HeadlessRunnerOptions options)
    {
        var failures = new List<string>();

        if (options.GodotPath is not null)
        {
            var godotFailure = CheckGodotPath(options.GodotPath);
            if (godotFailure is not null)
            {
                failures.Add(godotFailure);
            }
        }

        for (var i = 0; i < options.AllowedProjectRoots.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(options.AllowedProjectRoots[i]))
            {
                failures.Add($"HeadlessRunner:AllowedProjectRoots[{i}] is blank; provide a project root path.");
            }
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static string? CheckGodotPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "HeadlessRunner:GodotPath is set but blank; remove it or provide the path to the Godot binary.";
        }

        if (!Path.IsPathRooted(path))
        {
            return $"HeadlessRunner:GodotPath must be an absolute path: '{path}'.";
        }

        if (!File.Exists(path))
        {
            return $"HeadlessRunner:GodotPath does not exist or is not a file: '{path}'.";
        }

        if (!IsExecutable(path))
        {
            return $"HeadlessRunner:GodotPath is not executable: '{path}'.";
        }

        return null;
    }

    private static bool IsExecutable(string path) =>
        OperatingSystem.IsWindows() ||
        (File.GetUnixFileMode(path) & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0;
}
