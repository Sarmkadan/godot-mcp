namespace GodotMcp.Core.Parsing;

public static class TscnParser
{
    public static TscnDocument Parse(string text)
    {
        var reader = new GodotValueReader(text);
        reader.SkipWhitespaceAndComments();
        var descriptor = ParseSectionHeader(reader) ?? throw new GodotParseException("Missing descriptor section", 0);
        var document = new TscnDocument(descriptor);
        var current = descriptor;
        while (true)
        {
            reader.SkipWhitespaceAndComments();
            if (reader.AtEnd) break;
            var next = ParseSectionHeader(reader);
            if (next is not null)
            {
                document.Sections.Add(next);
                current = next;
                continue;
            }
            var key = ParsePropertyKey(reader);
            reader.Expect('=');
            var value = reader.ReadValue();
            current.Properties.Add(new KeyValuePair<string, GodotValue>(key, value));
        }
        return document;
    }

    public static TscnDocument ParseFile(string path) => Parse(File.ReadAllText(path));

    static TscnSection? ParseSectionHeader(GodotValueReader reader)
    {
        if (!reader.TryConsume('[')) return null;
        reader.SkipWhitespaceAndComments();
        var section = new TscnSection(reader.ReadIdentifier());
        while (true)
        {
            reader.SkipWhitespaceAndComments();
            if (reader.TryConsume(']')) break;
            var key = reader.ReadIdentifier();
            reader.Expect('=');
            var value = reader.ReadValue();
            section.Attributes.Add(new KeyValuePair<string, GodotValue>(key, value));
        }
        return section;
    }

    static string ParsePropertyKey(GodotValueReader reader)
    {
        reader.SkipWhitespaceAndComments();
        if (!reader.AtEnd && reader.PeekChar() == '"') return reader.ReadQuotedString();
        return reader.ReadPropertyPath();
    }
}
