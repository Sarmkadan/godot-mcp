using System.Text.RegularExpressions;
using GodotMcp.Core.Project;
using Microsoft.Extensions.Options;

namespace GodotMcp.Server;

/// <summary>
/// Validates <see cref="ProjectAnalyzerOptions"/> at startup. Reports every violation: negative limits, ignored-directory
/// entries that are not a single directory name, and extension filters that are not plain extensions like <c>.tscn</c>.
/// </summary>
public sealed partial class ProjectAnalyzerOptionsValidator : IValidateOptions<ProjectAnalyzerOptions>
{
    public ValidateOptionsResult Validate(string? name, ProjectAnalyzerOptions options)
    {
        var failures = new List<string>();

        if (options.MaxScanDepth < 0)
        {
            failures.Add($"ProjectAnalyzer:MaxScanDepth must be 0 (unlimited) or a positive number of directory levels, got {options.MaxScanDepth}.");
        }

        if (options.MaxFileCount < 0)
        {
            failures.Add($"ProjectAnalyzer:MaxFileCount must be 0 (unlimited) or a positive number of files, got {options.MaxFileCount}.");
        }

        ValidateIgnoredDirectories(options.IgnoredDirectories, failures);
        ValidateExtensionFilters(options.ExtensionFilters, failures);

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateIgnoredDirectories(IReadOnlyList<string> entries, List<string> failures)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var key = $"ProjectAnalyzer:IgnoredDirectories[{i}]";

            if (string.IsNullOrWhiteSpace(entry))
            {
                failures.Add($"{key} is blank; provide a directory name such as '.git'.");
                continue;
            }

            if (entry.Contains('/') || entry.Contains('\\'))
            {
                failures.Add($"{key} '{entry}' contains a path separator; ignored directories are matched by name at any depth, so use a single name such as '.godot'.");
                continue;
            }

            if (entry is "." or ".." || entry.Contains('*') || entry.Contains('?') || entry.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                failures.Add($"{key} '{entry}' is not a valid directory name; wildcards and relative segments are not supported.");
                continue;
            }

            if (!seen.Add(entry))
            {
                failures.Add($"{key} '{entry}' is a duplicate of an earlier entry.");
            }
        }
    }

    private static void ValidateExtensionFilters(IReadOnlyList<string> entries, List<string> failures)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var key = $"ProjectAnalyzer:ExtensionFilters[{i}]";

            if (string.IsNullOrWhiteSpace(entry))
            {
                failures.Add($"{key} is blank; provide an extension such as '.tscn'.");
                continue;
            }

            if (!ExtensionPattern().IsMatch(entry))
            {
                failures.Add($"{key} '{entry}' is not a valid extension; use a leading dot followed by letters, digits, '_' or '-' (for example '.tscn'). Wildcards and paths are not supported.");
                continue;
            }

            if (!seen.Add(entry))
            {
                failures.Add($"{key} '{entry}' is a duplicate of an earlier entry (extensions are compared case-insensitively).");
            }
        }
    }

    [GeneratedRegex(@"^\.[A-Za-z0-9_-]+$")]
    private static partial Regex ExtensionPattern();
}
