using System.Security;

namespace GodotMcp.Core.Project;

/// <summary>
/// Centralized path resolution service that confines all res:// and relative path resolution
/// to the project root and prevents directory traversal attacks.
/// </summary>
/// <remarks>
/// This class ensures that all path resolution stays within the project root directory,
/// preventing security issues like directory traversal attacks (e.g., ../../../../etc/passwd).
/// It handles both res:// prefixed paths and relative paths, converting them to absolute
/// filesystem paths while validating they remain within the project boundaries.
/// </remarks>
public sealed class PathResolver
{
    private readonly string _rootPath;
    private readonly string _rootPathNormalized;

    /// <summary>
    /// Initializes a new PathResolver for the given project root.
    /// </summary>
    /// <param name="rootPath">The absolute path to the project root directory</param>
    /// <exception cref="ArgumentNullException">Thrown if rootPath is null or whitespace</exception>
    /// <exception cref="ArgumentException">Thrown if rootPath is not a valid directory path</exception>
    public PathResolver(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        _rootPath = Path.GetFullPath(rootPath);
        _rootPathNormalized = NormalizePath(_rootPath);
    }

    /// <summary>
    /// Resolves a res:// or relative path to an absolute filesystem path,
    /// ensuring it stays within the project root.
    /// </summary>
    /// <param name="path">The path to resolve (res://path or relative path)</param>
    /// <returns>Absolute filesystem path within the project root</returns>
    /// <exception cref="ArgumentNullException">Thrown if path is null or whitespace</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if path attempts to escape the project root via directory traversal
    /// (e.g., ../../../../etc/passwd, res://../, absolute paths outside project)
    /// </exception>
    public string Resolve(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // Handle res:// prefix
        var relativePath = path.StartsWith("res://", StringComparison.Ordinal)
            ? path[6..]  // Remove "res://" prefix
            : path.TrimStart('/', '\\');

        // Normalize path separators and remove redundant separators
        relativePath = NormalizePath(relativePath);

        // Convert to absolute path relative to root
        // Use Path.Combine which handles relative paths correctly
        var combinedPath = Path.Combine(_rootPath, relativePath);
        var absolutePath = Path.GetFullPath(combinedPath);

        // Ensure the resolved path is within the project root
        if (!IsPathWithinRoot(absolutePath))
        {
            throw new ArgumentException(
                $"Path '{path}' resolves to '{GetRelativePathDisplay(absolutePath)}' which is outside the project root '{GetRelativePathDisplay(_rootPath)}'. Path traversal attacks are not allowed.",
                nameof(path));
        }

        return absolutePath;
    }

    /// <summary>
    /// Converts an absolute filesystem path back to a res:// path.
    /// </summary>
    /// <param name="absolutePath">Absolute filesystem path</param>
    /// <returns>res:// path relative to project root</returns>
    /// <exception cref="ArgumentNullException">Thrown if absolutePath is null or whitespace</exception>
    /// <exception cref="ArgumentException">Thrown if absolutePath is outside the project root</exception>
    public string ToResPath(string absolutePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(absolutePath);

        var absolutePathNormalized = Path.GetFullPath(absolutePath);

        if (!IsPathWithinRoot(absolutePathNormalized))
        {
            throw new ArgumentException(
                $"Path '{GetRelativePathDisplay(absolutePath)}' is outside the project root '{GetRelativePathDisplay(_rootPath)}'",
                nameof(absolutePath));
        }

        var relative = Path.GetRelativePath(_rootPath, absolutePathNormalized);
        return "res://" + relative.Replace(Path.DirectorySeparatorChar, '/');
    }

    /// <summary>
    /// Validates that a path string is safe and doesn't contain path traversal sequences.
    /// </summary>
    /// <param name="path">Path to validate</param>
    /// <returns>True if path is safe, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown if path is null</exception>
    public bool IsPathSafe(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (string.IsNullOrWhiteSpace(path))
            return false;

        // Check for obvious traversal attempts
        var normalized = path.Replace('\\', '/');
        if (normalized.Contains("/../") ||
            normalized.StartsWith("../", StringComparison.Ordinal) ||
            normalized.StartsWith("..\\", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the project root path.
    /// </summary>
    public string RootPath => _rootPath;

    #region Private Methods

    /// <summary>
    /// Normalizes a path by converting separators and removing redundant separators.
    /// </summary>
    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        // Convert all separators to forward slashes for consistent handling
        var normalized = path.Replace('\\', '/');

        // Remove redundant separators
        normalized = normalized.Replace("//", "/");

        // Trim leading/trailing slashes (except for res:// which we already handled)
        return normalized.Trim('/');
    }

    /// <summary>
    /// Checks if an absolute path is within the project root.
    /// </summary>
    private bool IsPathWithinRoot(string absolutePath)
    {
        var absolutePathNormalized = Path.GetFullPath(absolutePath);

        // Normalize both paths for comparison
        var rootNormalized = _rootPathNormalized;

        // Check if the path starts with the root path
        // Use ordinal comparison for consistency
        return absolutePathNormalized.StartsWith(rootNormalized, StringComparison.Ordinal) &&
               (absolutePathNormalized.Length == rootNormalized.Length ||
                absolutePathNormalized[rootNormalized.Length] == Path.DirectorySeparatorChar ||
                absolutePathNormalized[rootNormalized.Length] == Path.AltDirectorySeparatorChar);
    }

    /// <summary>
    /// Gets a display-friendly relative path for error messages.
    /// </summary>
    private static string GetRelativePathDisplay(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return fullPath.Length > 100 ? fullPath[..50] + "..." + fullPath[^25..] : fullPath;
    }

    #endregion
}