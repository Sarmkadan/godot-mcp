using GodotMcp.Core.Project;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GodotMcp.Server;

public static class ProjectAnalyzerOptionsExtensions
{
    /// <summary>
    /// Registers <see cref="ProjectAnalyzerOptions"/> bound from the "ProjectAnalyzer" section (for example
    /// <c>ProjectAnalyzer__MaxFileCount</c> or <c>--ProjectAnalyzer:MaxScanDepth=8</c>). Validation runs at host start and
    /// reports every violation, so an invalid scan configuration stops the server before it serves any request.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The application configuration to bind from.</param>
    public static IServiceCollection AddProjectAnalyzerOptions(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection("ProjectAnalyzer");
        services.AddOptions<ProjectAnalyzerOptions>()
            // The binder appends to list properties, so a configured list must replace the built-in defaults instead of extending them.
            .Configure(o =>
            {
                if (section.GetSection(nameof(ProjectAnalyzerOptions.IgnoredDirectories)).Exists())
                {
                    o.IgnoredDirectories = [];
                }

                if (section.GetSection(nameof(ProjectAnalyzerOptions.ExtensionFilters)).Exists())
                {
                    o.ExtensionFilters = [];
                }
            })
            .Bind(section)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ProjectAnalyzerOptions>, ProjectAnalyzerOptionsValidator>();
        return services;
    }
}
