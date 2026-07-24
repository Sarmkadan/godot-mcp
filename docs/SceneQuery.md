# SceneQuery

Provides static helper methods for querying the current Godot scene tree and returning lightweight `NodeInfo` descriptors for matching nodes. The API is intended for analysis tools, editor extensions, and runtime introspection where direct node manipulation is unnecessary or undesirable.

## API

### `public static IEnumerable<NodeInfo> AllNodes`
- **Purpose:** Returns information for every node in the active scene tree.
- **Parameters:** None.
- **Return value:** An enumerable of `NodeInfo` objects representing each node; empty if the scene tree is not initialized.
- **Exceptions:** 
  - `InvalidOperationException` – thrown when accessed outside of a valid scene tree context (e.g., before the scene is loaded or after it has been freed).

### `public static IEnumerable<NodeInfo> FindByType(Type type)`
- **Purpose:** Returns nodes whose type matches the supplied `System.Type`, including derived types.
- **Parameters:** 
  - `type` – The .NET type to match against; must not be `null`.
- **Return value:** An enumerable of `NodeInfo` objects for each matching node; empty if no nodes match.
- **Exceptions:** 
  - `ArgumentNullException` – if `type` is `null`.

### `public static IEnumerable<NodeInfo> FindByName(string pattern)`
- **Purpose:** Returns nodes whose name matches the supplied wildcard pattern (`*` and `?` are supported).
- **Parameters:** 
  - `pattern` – The name pattern to match; must not be `null`.
- **Return value:** An enumerable of `NodeInfo` objects for each node whose name satisfies the pattern; empty if no matches.
- **Exceptions:** 
  - `ArgumentNullException` – if `pattern` is `null`.

### `public static IEnumerable<NodeInfo> FindInGroup(string group)`
- **Purpose:** Returns nodes that belong to the specified scene group.
- **Parameters:** 
  - `group` – The group name to filter by; must not be `null` or empty.
- **Return value:** An enumerable of `NodeInfo` objects for each node in the group; empty if the group contains no nodes.
- **Exceptions:** 
  - `ArgumentNullException` – if `group` is `null`.
  - `ArgumentException` – if `group` is an empty string.

### `public static IEnumerable<NodeInfo> FindWithScript(Type scriptType)`
- **Purpose:** Returns nodes that have a script of the supplied type assigned (including scripts that inherit from the type).
- **Parameters:** 
  - `scriptType` – The .NET type of the script to look for; must not be `null`.
- **Return value:** An enumerable of `NodeInfo` objects for each node with a matching script; empty if none are found.
- **Exceptions:** 
  - `ArgumentNullException` – if `scriptType` is `null`.

### `public static bool MatchesWildcard(string pattern, string input)`
- **Purpose:** Determines whether `input` matches the wildcard `pattern` (`*` matches any sequence, `?` matches any single character).
- **Parameters:** 
  - `pattern` – The wildcard pattern; must not be `null`.
  - `input` – The string to test; must not be `null`.
- **Return value:** `true` if `input` conforms to `pattern`; otherwise `false`.
- **Exceptions:** 
  - `ArgumentNullException` – if either `pattern` or `input` is `null`.

## Usage

```csharp
using Godot;
using Godot.Mcp;

// Example 1: Collect all MeshInstance nodes in the current scene.
IEnumerable<NodeInfo> meshInstances = SceneQuery.FindByType(typeof(MeshInstance));
foreach (var info in meshInstances)
{
    GD.Print($"Found MeshInstance at {info.NodePath} with name {info.Name}");
}
```

```csharp
using Godot;
using Godot.Mcp;

// Example 2: Find enemy characters named "Boss*" that have the EnemyAI script attached.
IEnumerable<NodeInfo> bosses = SceneQuery.FindByName("Boss*")
                                         .Where(info => SceneQuery.FindWithScript(typeof(EnemyAI))
                                                                   .Any(i => i.NodePath == info.NodePath))
                                         .ToList();

if (bosses.Any())
{
    GD.Print($"{bosses.Count()} boss enemies detected.");
}
else
{
    GD.Print("No matching bosses found.");
}
```

## Notes
- All query methods operate on the scene tree as it exists at the moment of invocation; modifications to the tree after the call are not reflected in the returned enumerables.
- The methods are thread‑safe with respect to concurrent calls, but they must be executed on the Godot main thread because accessing the scene tree from other threads can lead to undefined behavior or crashes.
- Empty results are returned as empty enumerables rather than `null`; callers should not assume a non‑null return value indicates matches.
- Wildcard matching in `FindByName` and `MatchesWildcard` follows Godot’s standard pattern semantics: `*` matches zero or more characters, `?` matches exactly one character, and matching is case‑sensitive.
- If the scene is currently being changed (e.g., during a `QueueFree` or `AddChild` operation), the enumeration may reflect an inconsistent state; it is advisable to call these methods after the scene tree has settled (e.g., in `_Process` or `_PhysicsProcess` callbacks, or from editor tooling that runs after the frame).
