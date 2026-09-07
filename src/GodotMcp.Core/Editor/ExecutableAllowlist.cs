using System.Security;
using System.Collections.Frozen;

namespace GodotMcp.Core.Editor;

/// <summary>
/// Provides executable path allowlisting to prevent arbitrary command execution.
/// Executables must be explicitly allowlisted at server startup via configuration.
/// </summary>
public static class ExecutableAllowlist
{
    private static FrozenSet<string> _allowedExecutables = FrozenSet<string>.Empty;

    /// <summary>
    /// Initializes the allowlist with the specified executable paths.
    /// </summary>
    /// <param name="allowedExecutablePaths">Collection of absolute paths to allowed executables</param>
    /// <exception cref="ArgumentNullException">Thrown if allowedExecutablePaths is null</exception>
    /// <exception cref="ArgumentException">Thrown if any path is null, empty, or not absolute</exception>
    public static void Initialize(IEnumerable<string> allowedExecutablePaths)
    {
        ArgumentNullException.ThrowIfNull(allowedExecutablePaths);

        var normalizedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in allowedExecutablePaths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Executable path cannot be null or whitespace", nameof(allowedExecutablePaths));
            }

            var absolutePath = Path.GetFullPath(path.Trim());
            if (!Path.IsPathRooted(absolutePath))
            {
                throw new ArgumentException(
                    $"Executable path must be absolute: '{path}'",
                    nameof(allowedExecutablePaths));
            }

            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException(
                    $"Allowlisted executable not found: '{absolutePath}'",
                    absolutePath);
            }

            normalizedPaths.Add(absolutePath);
        }

        _allowedExecutables = normalizedPaths.ToFrozenSet();
    }

    /// <summary>
    /// Checks if an executable path is allowlisted.
    /// </summary>
    /// <param name="executablePath">The executable path to check</param>
    /// <returns>True if the executable is allowlisted; otherwise false</returns>
    /// <exception cref="ArgumentNullException">Thrown if executablePath is null</exception>
    public static bool IsAllowed(string executablePath)
    {
        ArgumentNullException.ThrowIfNull(executablePath);
        return _allowedExecutables.Contains(Path.GetFullPath(executablePath));
    }

    /// <summary>
    /// Gets the count of allowlisted executables.
    /// </summary>
    public static int Count => _allowedExecutables.Count;

    /// <summary>
    /// Validates that the executable is allowlisted and throws if not.
    /// </summary>
    /// <param name="executablePath">The executable path to validate</param>
    /// <exception cref="SecurityException">Thrown if the executable is not allowlisted</exception>
    /// <exception cref="ArgumentNullException">Thrown if executablePath is null</exception>
    public static void Validate(string executablePath)
    {
        ArgumentNullException.ThrowIfNull(executablePath);

        var absolutePath = Path.GetFullPath(executablePath);
        if (!_allowedExecutables.Contains(absolutePath))
        {
            throw new SecurityException(
                $"Executable not allowlisted: '{absolutePath}'. " +
                "Only allowlisted executables can be executed for security reasons.");
        }
    }

    /// <summary>
    /// Gets the allowlist as a read-only collection.
    /// </summary>
    public static IReadOnlyCollection<string> GetAllowlist() => _allowedExecutables;
}