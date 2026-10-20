using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GodotMcp.Server;

/// <summary>
/// Startup configuration for headless Godot runs. Bound from the "HeadlessRunner" configuration section
/// (for example <c>HeadlessRunner__TimeoutSeconds</c> or <c>--HeadlessRunner:TimeoutSeconds=30</c>) and validated
/// before the server starts. Checks that DataAnnotations cannot express live in <see cref="HeadlessRunnerOptionsValidator"/>.
/// </summary>
public sealed class HeadlessRunnerOptions
{
    /// <summary>
    /// Absolute path to the Godot binary. Null means the binary is located on PATH or via GODOT_BIN/GODOT.
    /// </summary>
    public string? GodotPath { get; set; }

    /// <summary>
    /// Default timeout for headless runs, in seconds.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "HeadlessRunner:TimeoutSeconds must be a positive number of seconds.")]
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Upper bound for captured Godot output, in bytes.
    /// </summary>
    [Range(1, long.MaxValue, ErrorMessage = "HeadlessRunner:MaxOutputBytes must be a positive number of bytes.")]
    public long MaxOutputBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Project root directories the runner may operate on. Defaults to the project path the server was started with.
    /// </summary>
    [MinLength(1, ErrorMessage = "HeadlessRunner:AllowedProjectRoots must contain at least one project root.")]
    public List<string> AllowedProjectRoots { get; set; } = [];
}

public static class HeadlessRunnerOptionsExtensions
{
    /// <summary>
    /// Registers <see cref="HeadlessRunnerOptions"/> bound from the "HeadlessRunner" section. If no project roots are
    /// configured, <paramref name="defaultProjectRoot"/> is used. Validation runs at host start and reports every violation.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The application configuration to bind from.</param>
    /// <param name="defaultProjectRoot">The project path the server was started with.</param>
    public static IServiceCollection AddHeadlessRunnerOptions(this IServiceCollection services, IConfiguration configuration, string defaultProjectRoot)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultProjectRoot);

        services.AddOptions<HeadlessRunnerOptions>()
            .Bind(configuration.GetSection("HeadlessRunner"))
            .PostConfigure(o =>
            {
                if (o.AllowedProjectRoots.Count == 0)
                {
                    o.AllowedProjectRoots.Add(defaultProjectRoot);
                }
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<HeadlessRunnerOptions>, HeadlessRunnerOptionsValidator>();
        return services;
    }
}
