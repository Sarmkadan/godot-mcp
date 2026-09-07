namespace GodotMcp.Server;

/// <summary>
/// Configuration for executable allowlisting and security settings.
/// </summary>
public sealed record ExecutableConfig
{
    /// <summary>
    /// Gets or sets the collection of allowlisted executable paths.
    /// </summary>
    public IReadOnlyList<string> AllowedExecutables { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the default timeout for executable runs in seconds.
    /// </summary>
    public int DefaultTimeoutSeconds { get; init; } = 120;

    /// <summary>
    /// Creates a default configuration with no allowlisted executables (fail closed).
    /// </summary>
    public static ExecutableConfig Default() => new()
    {
        AllowedExecutables = Array.Empty<string>(),
        DefaultTimeoutSeconds = 120
    };

    /// <summary>
    /// Creates a configuration from command line arguments.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Configured ExecutableConfig</returns>
    public static ExecutableConfig FromArgs(string[] args)
    {
        var config = new ExecutableConfig();

        // Parse command line arguments for executable paths
        // Format: --allowlist-executable=/path/to/godot or --allowlist-executable=/path/to/godot --allowlist-executable=/path/to/other
        var allowlist = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--allowlist-executable=", StringComparison.Ordinal))
            {
                var path = args[i]["--allowlist-executable=".Length..];
                if (!string.IsNullOrWhiteSpace(path))
                {
                    allowlist.Add(path);
                }
            }
            else if (args[i] == "--allowlist-executable" && i + 1 < args.Length)
            {
                allowlist.Add(args[i + 1]);
                i++; // Skip next arg
            }
        }

        if (allowlist.Count > 0)
        {
            config = config with { AllowedExecutables = allowlist.AsReadOnly() };
        }

        return config;
    }
}