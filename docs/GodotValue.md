# GodotValue

`GodotValue` is the abstract base class representing data types supported by the Godot text serialization format (TSCN/TRES). It serves as the foundation for the `godot-mcp` value model, enabling the representation, serialization, and parsing of various Godot-compatible data types within the library, ensuring consistent handling of scene and resource file formats.

## API

### Types and Enumerations

*   **`GodotValueKind`** (enum): An enumeration defining the supported types of `GodotValue`, used to identify the underlying concrete implementation.
*   **`GodotBool`** (sealed record): Represents a Godot boolean value (`true` or `false`).
*   **`GodotInt`** (sealed record): Represents a Godot 64-bit integer value.
*   **`GodotFloat`** (sealed record): Represents a Godot floating-point value.
*   **`GodotString`** (sealed record): Represents a Godot string value, including proper escaping for TSCN files.
*   **`GodotStringName`** (sealed record): Represents a Godot `StringName` identifier.
*   **`GodotNodePath`** (sealed record): Represents a Godot `NodePath` value.

### Properties and Methods

*   **`Kind`** (abstract property): Returns the `GodotValueKind` associated with the current instance.
*   **`Write(TextWriter writer)`** (abstract method): Writes the value to the specified `TextWriter` in the correct TSCN format. Concrete record types (`GodotBool`, etc.) provide specific implementations of this method.
*   **`ToTscnString()`** (method): Returns a string representation of the `GodotValue` formatted for inclusion in a TSCN file.
*   **`ToString()`** (sealed override): Returns a string representation of the value, typically used for debugging purposes.
*   **`Parse(string value)`** (static method): Parses a TSCN-formatted string into its corresponding `GodotValue` subtype. Throws `GodotParseException` if the provided string is not a valid representation.
*   **`Escape(string value)`** (static method): Sanitizes a string by escaping special characters, ensuring it conforms to the Godot text serialization format.

## Usage

### Parsing a Godot Value
```csharp
string tscnValue = "42";
GodotValue parsedValue = GodotValue.Parse(tscnValue);

if (parsedValue is GodotInt intValue)
{
    Console.WriteLine($"Parsed integer: {intValue}");
}
```

### Serializing a Godot Value
```csharp
GodotValue myNodePath = new GodotNodePath("../Player");
string serialized = myNodePath.ToTscnString();

// serialized is now: "NodePath(\"../Player\")"
```

## Notes

*   **Immutability:** All concrete `GodotValue` subtypes are implemented as `sealed record` types, ensuring that they are immutable and suitable for use in read-only data structures.
*   **Error Handling:** The `Parse` method will throw a `GodotParseException` when encountering malformed TSCN strings that do not map to supported `GodotValueKind` types.
*   **Thread Safety:** Since the records are immutable, instances of `GodotValue` are thread-safe and can be safely shared across threads. Users should ensure that the `TextWriter` passed to the `Write` method is handled according to its own thread-safety requirements.
