using System.Globalization;
using System.Text;

namespace GodotMcp.Core.Parsing;

public enum GodotValueKind { Null, Bool, Int, Float, String, StringName, NodePath, Array, Dictionary, Constructor, Identifier }

public abstract record GodotValue
{
    public abstract GodotValueKind Kind { get; }
    public abstract void Write(StringBuilder sb);
    public string ToTscnString()
    {
        var sb = new StringBuilder();
        Write(sb);
        return sb.ToString();
    }
    public sealed override string ToString() => ToTscnString();
    public static GodotValue Parse(string text) => new GodotValueReader(text).ReadValue();

    public static string Escape(string s)
    {
        var sb = new StringBuilder(s.Length + 2);
        foreach (var c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\t': sb.Append("\\t"); break;
                case '\r': sb.Append("\\r"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }
}

public sealed record GodotNull : GodotValue
{
    public static readonly GodotNull Instance = new();
    public override GodotValueKind Kind => GodotValueKind.Null;
    public override void Write(StringBuilder sb) => sb.Append("null");
}

public sealed record GodotBool(bool Value) : GodotValue
{
    public override GodotValueKind Kind => GodotValueKind.Bool;
    public override void Write(StringBuilder sb) => sb.Append(Value ? "true" : "false");
}

public sealed record GodotInt(long Value) : GodotValue
{
    public override GodotValueKind Kind => GodotValueKind.Int;
    public override void Write(StringBuilder sb) => sb.Append(Value.ToString(CultureInfo.InvariantCulture));
}

public sealed record GodotFloat(double Value) : GodotValue
{
    public override GodotValueKind Kind => GodotValueKind.Float;
    public override void Write(StringBuilder sb)
    {
        if (double.IsPositiveInfinity(Value)) sb.Append("inf");
        else if (double.IsNegativeInfinity(Value)) sb.Append("-inf");
        else if (double.IsNaN(Value)) sb.Append("nan");
        else
        {
            var text = Value.ToString("R", CultureInfo.InvariantCulture);
            sb.Append(text);
            if (!text.Contains('.') && !text.Contains('e') && !text.Contains('E')) sb.Append(".0");
        }
    }
}

public sealed record GodotString(string Value) : GodotValue
{
    public override GodotValueKind Kind => GodotValueKind.String;
    public override void Write(StringBuilder sb) => sb.Append('"').Append(Escape(Value)).Append('"');
}

public sealed record GodotStringName(string Value) : GodotValue
{
    public override GodotValueKind Kind => GodotValueKind.StringName;
    public override void Write(StringBuilder sb) => sb.Append("&\"").Append(Escape(Value)).Append('"');
}

public sealed record GodotNodePath(string Value) : GodotValue
{
    public override GodotValueKind Kind => GodotValueKind.NodePath;
    public override void Write(StringBuilder sb) => sb.Append("^\"").Append(Escape(Value)).Append('"');
}

public sealed record GodotIdentifier(string Name) : GodotValue
{
    public override GodotValueKind Kind => GodotValueKind.Identifier;
    public override void Write(StringBuilder sb) => sb.Append(Name);
}

public sealed record GodotArray(IReadOnlyList<GodotValue> Items) : GodotValue
{
    public override GodotValueKind Kind => GodotValueKind.Array;
    public override void Write(StringBuilder sb)
    {
        sb.Append('[');
        for (var i = 0; i < Items.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            Items[i].Write(sb);
        }
        sb.Append(']');
    }
}

public sealed record GodotDictionary(IReadOnlyList<KeyValuePair<GodotValue, GodotValue>> Entries) : GodotValue
{
    public override GodotValueKind Kind => GodotValueKind.Dictionary;
    public GodotValue? this[string key] => Entries.FirstOrDefault(e => e.Key is GodotString s && s.Value == key).Value;
    public override void Write(StringBuilder sb)
    {
        if (Entries.Count == 0)
        {
            sb.Append("{}");
            return;
        }
        sb.Append("{\n");
        for (var i = 0; i < Entries.Count; i++)
        {
            Entries[i].Key.Write(sb);
            sb.Append(": ");
            Entries[i].Value.Write(sb);
            if (i < Entries.Count - 1) sb.Append(',');
            sb.Append('\n');
        }
        sb.Append('}');
    }
}

public sealed record GodotConstructor(string Name, IReadOnlyList<GodotValue> Arguments) : GodotValue
{
    public override GodotValueKind Kind => GodotValueKind.Constructor;
    public bool IsExtResource => Name == "ExtResource";
    public bool IsSubResource => Name == "SubResource";
    public bool IsResource => Name == "Resource";
    public string? ReferenceId => Arguments is [GodotString s] ? s.Value : null;
    public static GodotConstructor ExtResource(string id) => new("ExtResource", [new GodotString(id)]);
    public static GodotConstructor SubResource(string id) => new("SubResource", [new GodotString(id)]);
    public static GodotConstructor Vector2(double x, double y) => new("Vector2", [new GodotFloat(x), new GodotFloat(y)]);
    public static GodotConstructor Vector3(double x, double y, double z) => new("Vector3", [new GodotFloat(x), new GodotFloat(y), new GodotFloat(z)]);
    public static GodotConstructor Color(double r, double g, double b, double a = 1) => new("Color", [new GodotFloat(r), new GodotFloat(g), new GodotFloat(b), new GodotFloat(a)]);
    public override void Write(StringBuilder sb)
    {
        sb.Append(Name).Append('(');
        for (var i = 0; i < Arguments.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            Arguments[i].Write(sb);
        }
        sb.Append(')');
    }
}
