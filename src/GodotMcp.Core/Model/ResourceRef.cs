namespace GodotMcp.Core.Model;

public sealed record ResourceRef(string Id, string Type, string Path, string? Uid = null)
{
    public string Extension => System.IO.Path.GetExtension(Path);
    public bool IsScript => Extension is ".cs" or ".gd";
    public bool IsScene => Extension is ".tscn" or ".scn";
}
