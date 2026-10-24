namespace GodotMcp.Core.Parsing;

public static class TscnParser
{
    public static TscnDocument Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new GodotParseException("Cannot parse an empty or whitespace-only TSCN document", 0);
        }

        var reader = new GodotValueReader(text);
        var pending = ReadComments(reader);
        var descriptor = ParseSectionHeader(reader) ?? throw new GodotParseException("Missing descriptor section", 0);
        descriptor.LeadingComments.AddRange(pending);
        var document = new TscnDocument(descriptor);
        var current = descriptor;
        while (true)
        {
            pending = ReadComments(reader);
            if (reader.AtEnd)
            {
                document.TrailingComments.AddRange(pending);
                break;
            }
            var next = ParseSectionHeader(reader);
            if (next is not null)
            {
                next.LeadingComments.AddRange(pending);
                document.Sections.Add(next);
                current = next;
                continue;
            }
            var key = ParsePropertyKey(reader);
            reader.Expect('=');
            var value = reader.ReadValue();
            current.Properties.Add(new KeyValuePair<string, GodotValue>(key, value));
            if (pending.Count > 0)
            {
                current.PropertyComments[key] = pending;
            }
        }
        return document;
    }

    /// <summary>
    /// Skips whitespace and collects the comment lines in between, so they can be written back verbatim.
    /// </summary>
    static List<string> ReadComments(GodotValueReader reader)
    {
        var comments = new List<string>();
        reader.SkipWhitespace();
        while (!reader.AtEnd && reader.PeekChar() is ';' or '#')
        {
            comments.Add(reader.ReadLine().TrimEnd());
            reader.SkipWhitespace();
        }
        return comments;
    }

    public static TscnDocument ParseFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"TSCN file not found: '{path}'", path);
        }

        return Parse(File.ReadAllText(path));
    }

    static TscnSection? ParseSectionHeader(GodotValueReader reader)
    {
        if (!reader.TryConsume('['))
        {
            return null;
        }

        reader.SkipWhitespaceAndComments();
        var section = new TscnSection(reader.ReadIdentifier());
        while (true)
        {
            reader.SkipWhitespaceAndComments();
            if (reader.TryConsume(']'))
            {
                break;
            }

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
        if (!reader.AtEnd && reader.PeekChar() == '"')
        {
            return reader.ReadQuotedString();
        }

        return reader.ReadPropertyPath();
    }
}
