using GodotMcp.Core.Parsing;

namespace GodotMcp.Core.Model;

public sealed record NodeProperty(string Name, GodotValue Value)
{
    public string ValueText => Value.ToTscnString();
    public string Kind => Value.Kind.ToString();
}
