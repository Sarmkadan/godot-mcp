# GodotParseException

The `GodotParseException` is a specialized exception thrown by the `godot-mcp` library when the parser encounters invalid syntax or structural irregularities while reading Godot text-format files (e.g., `.tscn` or `.tres` files). It captures the specific error message and the exact character position within the input stream where the parsing failure occurred, facilitating accurate debugging and error reporting.

## API

### GodotParseException(string message, int position)
Initializes a new instance of the `GodotParseException` class.

*   **Parameters:**
    *   `message`: A `string` describing the nature of the parsing error.
    *   `position`: An `int` representing the character index in the source text where the error was detected.

### Position
Gets the character position in the input text where the parsing error occurred.

*   **Type:** `int`
*   **Returns:** The zero-based index of the character location.

## Usage

### Throwing an Exception
This example demonstrates how a custom parser might throw the exception when encountering an unexpected character.

```csharp
public void Expect(char expected, GodotValueReader reader)
{
    if (!reader.TryConsume(expected))
    {
        throw new GodotParseException(
            $"Expected character '{expected}', but found '{reader.PeekChar()}'", 
            reader.Position
        );
    }
}
```

### Catching and Handling an Exception
This example shows how a consuming application can catch the exception to report the specific failure location to the user.

```csharp
try
{
    var document = parser.Parse(tscnContent);
}
catch (GodotParseException ex)
{
    Console.Error.WriteLine($"Failed to parse Godot file at position {ex.Position}: {ex.Message}");
}
```

## Notes

*   **Thread Safety:** As an exception type, `GodotParseException` is inherently thread-safe to throw and propagate. The `Position` property is immutable after the exception is instantiated.
*   **Edge Cases:** The `Position` value represents the character index at the time the violation was detected. When parsing large files, this value is crucial for mapping errors back to the source file lines and columns for user-friendly reporting.
*   **Inheritance:** Inherits from `System.Exception`. Standard exception handling practices apply.
