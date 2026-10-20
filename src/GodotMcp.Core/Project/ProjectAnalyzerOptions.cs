namespace GodotMcp.Core.Project;

/// <summary>
/// Limits and filters applied when <see cref="ProjectAnalyzer"/> enumerates project files.
/// Validation of configured values lives in the Server host; these defaults are the values used when nothing is configured.
/// </summary>
public sealed class ProjectAnalyzerOptions
{
    /// <summary>
    /// How many directory levels below the project root are scanned. 1 means the root and its immediate subdirectories.
    /// 0 means unlimited.
    /// </summary>
    public int MaxScanDepth { get; set; } = 32;

    /// <summary>
    /// Maximum number of files a single scan may return. Exceeding it fails the scan instead of returning partial results,
    /// because a truncated scan would make reference checks report false orphans. 0 means unlimited.
    /// </summary>
    public int MaxFileCount { get; set; } = 100_000;

    /// <summary>
    /// Directory names (not paths) that are skipped at any depth.
    /// </summary>
    public List<string> IgnoredDirectories { get; set; } = [".git", ".godot", ".import"];

    /// <summary>
    /// Extensions such as <c>.tscn</c> that a scan is restricted to, in addition to the extensions each query asks for.
    /// Empty means no restriction.
    /// </summary>
    public List<string> ExtensionFilters { get; set; } = [];
}
