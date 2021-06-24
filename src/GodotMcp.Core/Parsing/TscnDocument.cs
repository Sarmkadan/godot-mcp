using System.Text;

namespace GodotMcp.Core.Parsing;

public sealed class TscnSection(string name)
{
    public string Name { get; } = name;
    public List<KeyValuePair<string, GodotValue>> Attributes { get; } = [];
    public List<KeyValuePair<string, GodotValue>> Properties { get; } = [];

    public GodotValue? GetAttribute(string key) => Attributes.FirstOrDefault(a => a.Key == key).Value;
    public string? GetAttributeString(string key) => GetAttribute(key) is GodotString s ? s.Value : null;
    public long? GetAttributeInt(string key) => GetAttribute(key) is GodotInt i ? i.Value : null;
    public GodotValue? GetProperty(string key) => Properties.FirstOrDefault(p => p.Key == key).Value;

    public void SetAttribute(string key, GodotValue value)
    {
        var index = Attributes.FindIndex(a => a.Key == key);
        var pair = new KeyValuePair<string, GodotValue>(key, value);
        if (index >= 0) Attributes[index] = pair;
        else Attributes.Add(pair);
    }

    public void SetProperty(string key, GodotValue value)
    {
        var index = Properties.FindIndex(p => p.Key == key);
        var pair = new KeyValuePair<string, GodotValue>(key, value);
        if (index >= 0) Properties[index] = pair;
        else Properties.Add(pair);
    }

    public bool RemoveProperty(string key)
    {
        var index = Properties.FindIndex(p => p.Key == key);
        if (index < 0) return false;
        Properties.RemoveAt(index);
        return true;
    }

    public void WriteTo(StringBuilder sb)
    {
        sb.Append('[').Append(Name);
        foreach (var (key, value) in Attributes)
        {
            sb.Append(' ').Append(key).Append('=');
            value.Write(sb);
        }
        sb.Append("]\n");
        foreach (var (key, value) in Properties)
        {
            sb.Append(key).Append(" = ");
            value.Write(sb);
            sb.Append('\n');
        }
    }
}

public sealed class TscnDocument(TscnSection descriptor)
{
    public TscnSection Descriptor { get; } = descriptor;
    public List<TscnSection> Sections { get; } = [];

    public bool IsScene => Descriptor.Name == "gd_scene";
    public bool IsResource => Descriptor.Name == "gd_resource";
    public string? Uid => Descriptor.GetAttributeString("uid");
    public long Format => Descriptor.GetAttributeInt("format") ?? 3;

    public IEnumerable<TscnSection> SectionsNamed(string name) => Sections.Where(s => s.Name == name);
    public IEnumerable<TscnSection> ExtResources => SectionsNamed("ext_resource");
    public IEnumerable<TscnSection> SubResources => SectionsNamed("sub_resource");
    public IEnumerable<TscnSection> Nodes => SectionsNamed("node");
    public IEnumerable<TscnSection> Connections => SectionsNamed("connection");

    public TscnSection? FindExtResource(string id) => ExtResources.FirstOrDefault(s => s.GetAttributeString("id") == id);
    public TscnSection? FindSubResource(string id) => SubResources.FirstOrDefault(s => s.GetAttributeString("id") == id);

    public string Serialize()
    {
        var sb = new StringBuilder();
        Descriptor.WriteTo(sb);
        foreach (var section in Sections)
        {
            sb.Append('\n');
            section.WriteTo(sb);
        }
        return sb.ToString();
    }
}
