# TscnSection

The `TscnSection` class represents a single, self-contained section within a Godot Text Scene (`.tscn`) or Text Resource (`.tres`) file. It provides a structured model for interacting with section headers, which contain attributes (e.g., the `path` in an `[ext_resource]`), and the property block that follows, which defines specific instance variables (e.g., `position`, `name`). This class serves as a fundamental building block for parsing, inspecting, and modifying scene file contents programmatically.

## API

### Properties

*   **`string Name`**
    Gets or sets the identifier of the section (e.g., "node", "ext_resource", "sub_resource").

*   **`List<KeyValuePair<string, GodotValue>> Attributes`**
    Gets the list of header attributes defined for the section.

*   **`List<KeyValuePair<string, GodotValue>> Properties`**
    Gets the list of properties defined within the section body.

### Methods

*   **`GodotValue? GetAttribute(string name)`**
    Retrieves the value of a header attribute by its name. Returns `null` if the attribute is not found.

*   **`string? GetAttributeString(string name)`**
    Convenience method to retrieve the value of a header attribute as a string. Returns `null` if the attribute is not found or cannot be converted.

*   **`long? GetAttributeInt(string name)`**
    Convenience method to retrieve the value of a header attribute as a 64-bit integer. Returns `null` if the attribute is not found or cannot be converted.

*   **`GodotValue? GetProperty(string name)`**
    Retrieves the value of a property by its name. Returns `null` if the property is not found.

*   **`void SetAttribute(string name, GodotValue value)`**
    Adds a new header attribute or updates the value of an existing attribute with the specified name.

*   **`void SetProperty(string name, GodotValue value)`**
    Adds a new property or updates the value of an existing property with the specified name.

*   **`bool RemoveProperty(string name)`**
    Removes the first property with the specified name. Returns `true` if the property was found and removed; otherwise, `false`.

*   **`void WriteTo(TextWriter writer)`**
    Serializes the section and its contents to the provided `TextWriter` in the standard Godot text format.

## Usage

### Inspecting a Node Section
```csharp
// Assuming 'section' is a TscnSection representing a [node]
if (section.Name == "node")
{
    string? nodeName = section.GetAttributeString("name");
    string? nodeType = section.GetAttributeString("type");

    Console.WriteLine($"Node Name: {nodeName}, Type: {nodeType}");
}
```

### Modifying a Resource Property
```csharp
// Update the 'position' property of a node section
var newPosition = new GodotValue("Vector2(100, 200)");
section.SetProperty("position", newPosition);

// Verify the change
var currentPosition = section.GetProperty("position");
```

## Notes

*   **Thread Safety:** Instances of `TscnSection` are not thread-safe. Modifications to the `Attributes` or `Properties` lists, or calls to `SetAttribute`/`SetProperty` from multiple threads simultaneously, will result in undefined behavior.
*   **Case Sensitivity:** Godot text formats are generally case-sensitive regarding property and attribute keys. Ensure that the strings passed to `GetAttribute`, `SetAttribute`, `GetProperty`, and `SetProperty` match the exact casing used in the `.tscn` or `.tres` file.
*   **Null Handling:** Methods retrieving values return `null` if the requested item does not exist, allowing for safe navigation patterns.
