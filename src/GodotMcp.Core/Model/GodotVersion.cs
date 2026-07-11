using System.Globalization;

namespace GodotMcp.Core.Model;

public readonly record struct GodotVersion(int Major, int Minor, int Patch = 0, string Status = "stable")
{
    public static GodotVersion Parse(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        var parts = text.Trim().Split('.', StringSplitOptions.RemoveEmptyEntries);
        var major = parts.Length > 0 && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ma) ? ma : 0;
        var minor = parts.Length > 1 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var mi) ? mi : 0;
        var patch = 0;
        var status = "stable";
        for (var i = 2; i < parts.Length; i++)
        {
            if (int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var p)) patch = p;
            else status = parts[i];
        }
        return new GodotVersion(major, minor, patch, status);
    }

    public static bool TryParse(string? text, out GodotVersion version)
    {
        version = default;
        if (string.IsNullOrWhiteSpace(text) || !char.IsDigit(text.TrimStart()[0])) return false;
        version = Parse(text);
        return version.Major > 0;
    }

    public override string ToString() => Patch > 0 ? $"{Major}.{Minor}.{Patch}.{Status}" : $"{Major}.{Minor}.{Status}";
}
