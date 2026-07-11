using System.Globalization;
using System.Text;

namespace GodotMcp.Core.Parsing;

public sealed class GodotParseException(string message, int position) : Exception($"{message} at position {position}")
{
    public int Position { get; } = position;
}

public sealed class GodotValueReader
{
    public GodotValueReader(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        _text = text;
    }

    readonly string _text;
    public int Position { get; private set; }
    public bool AtEnd => Position >= _text.Length;
    char Current => _text[Position];

    public void SkipWhitespaceAndComments()
    {
        while (!AtEnd)
        {
            var c = Current;
            if (char.IsWhiteSpace(c)) Position++;
            else if (c is ';' or '#')
            {
                while (!AtEnd && Current != '\n') Position++;
            }
            else break;
        }
    }

    public void SkipInlineWhitespace()
    {
        while (!AtEnd && (Current == ' ' || Current == '\t')) Position++;
    }

    public bool TryConsume(char c)
    {
        SkipWhitespaceAndComments();
        if (AtEnd || Current != c) return false;
        Position++;
        return true;
    }

    public void Expect(char c)
    {
        if (!TryConsume(c)) throw new GodotParseException($"Expected '{c}'", Position);
    }

    public GodotValue ReadValue()
    {
        SkipWhitespaceAndComments();
        if (AtEnd) throw new GodotParseException("Unexpected end of input", Position);
        var c = Current;
        return c switch
        {
            '"' => new GodotString(ReadQuotedString()),
            '&' => ReadPrefixedString(v => new GodotStringName(v)),
            '^' => ReadPrefixedString(v => new GodotNodePath(v)),
            '[' => ReadArray(),
            '{' => ReadDictionary(),
            '-' or '+' => ReadNumber(),
            _ when char.IsDigit(c) => ReadNumber(),
            _ when char.IsLetter(c) || c == '_' || c == '@' => ReadWord(),
            _ => throw new GodotParseException($"Unexpected character '{c}'", Position)
        };
    }

    GodotValue ReadPrefixedString(Func<string, GodotValue> factory)
    {
        Position++;
        if (AtEnd || Current != '"') throw new GodotParseException("Expected '\"'", Position);
        return factory(ReadQuotedString());
    }

    public string ReadQuotedString()
    {
        Expect('"');
        var sb = new StringBuilder();
        while (true)
        {
            if (AtEnd) throw new GodotParseException("Unterminated string", Position);
            var c = Current;
            Position++;
            if (c == '"') break;
            if (c == '\\')
            {
                if (AtEnd) throw new GodotParseException("Unterminated escape", Position);
                var e = Current;
                Position++;
                sb.Append(e switch
                {
                    'n' => '\n',
                    't' => '\t',
                    'r' => '\r',
                    'b' => '\b',
                    'f' => '\f',
                    'u' => ReadUnicodeEscape(4),
                    'U' => ReadUnicodeEscape(6),
                    _ => e
                });
            }
            else sb.Append(c);
        }
        return sb.ToString();
    }

    char ReadUnicodeEscape(int digits)
    {
        if (Position + digits > _text.Length) throw new GodotParseException("Truncated unicode escape", Position);
        var hex = _text.Substring(Position, digits);
        Position += digits;
        return (char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }

    GodotValue ReadNumber()
    {
        var start = Position;
        if (Current is '-' or '+') Position++;
        if (!AtEnd && (char.IsLetter(Current)))
        {
            var word = ReadIdentifier();
            var negative = _text[start] == '-';
            return word switch
            {
                "inf" => new GodotFloat(negative ? double.NegativeInfinity : double.PositiveInfinity),
                "inf_neg" => new GodotFloat(double.NegativeInfinity),
                "nan" => new GodotFloat(double.NaN),
                _ => throw new GodotParseException($"Invalid number '{word}'", start)
            };
        }
        var isFloat = false;
        while (!AtEnd)
        {
            var c = Current;
            if (char.IsDigit(c)) Position++;
            else if (c == '.') { isFloat = true; Position++; }
            else if (c is 'e' or 'E')
            {
                isFloat = true;
                Position++;
                if (!AtEnd && (Current is '-' or '+')) Position++;
            }
            else break;
        }
        var span = _text.AsSpan(start, Position - start);
        if (!isFloat && long.TryParse(span, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)) return new GodotInt(l);
        if (double.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return new GodotFloat(d);
        throw new GodotParseException($"Invalid number '{span}'", start);
    }

    public char PeekChar() => Current;

    public string ReadPropertyPath()
    {
        var start = Position;
        while (!AtEnd && (char.IsLetterOrDigit(Current) || Current is '_' or '/' or '.' or ':' or '-' or '%' or '@')) Position++;
        if (Position == start) throw new GodotParseException("Expected property name", Position);
        return _text[start..Position];
    }

    public string ReadIdentifier()
    {
        var start = Position;
        while (!AtEnd && (char.IsLetterOrDigit(Current) || Current is '_' or '@' or '/')) Position++;
        if (Position == start) throw new GodotParseException("Expected identifier", Position);
        return _text[start..Position];
    }

    GodotValue ReadWord()
    {
        var start = Position;
        var word = ReadIdentifier();
        switch (word)
        {
            case "true": return new GodotBool(true);
            case "false": return new GodotBool(false);
            case "null": return GodotNull.Instance;
            case "inf": return new GodotFloat(double.PositiveInfinity);
            case "inf_neg": return new GodotFloat(double.NegativeInfinity);
            case "nan": return new GodotFloat(double.NaN);
        }
        SkipInlineWhitespace();
        if (!AtEnd && Current == '(')
        {
            Position++;
            var args = new List<GodotValue>();
            SkipWhitespaceAndComments();
            if (!TryConsume(')'))
            {
                while (true)
                {
                    args.Add(ReadValue());
                    if (TryConsume(')')) break;
                    Expect(',');
                }
            }
            return new GodotConstructor(word, args);
        }
        Position = start + word.Length;
        return new GodotIdentifier(word);
    }

    GodotValue ReadArray()
    {
        Expect('[');
        var items = new List<GodotValue>();
        if (TryConsume(']')) return new GodotArray(items);
        while (true)
        {
            items.Add(ReadValue());
            if (TryConsume(']')) break;
            Expect(',');
            if (TryConsume(']')) break;
        }
        return new GodotArray(items);
    }

    GodotValue ReadDictionary()
    {
        Expect('{');
        var entries = new List<KeyValuePair<GodotValue, GodotValue>>();
        if (TryConsume('}')) return new GodotDictionary(entries);
        while (true)
        {
            var key = ReadValue();
            Expect(':');
            var value = ReadValue();
            entries.Add(new KeyValuePair<GodotValue, GodotValue>(key, value));
            if (TryConsume('}')) break;
            Expect(',');
            if (TryConsume('}')) break;
        }
        return new GodotDictionary(entries);
    }
}
