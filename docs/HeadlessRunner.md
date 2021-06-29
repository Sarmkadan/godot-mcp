# HeadlessRunner

The `HeadlessRunner` class provides a structured interface for executing Godot engine instances in headless mode from a C# application. It abstracts the complexities of process management, allowing for programmatic interaction with Godot projects, such as running specific scenes, executing standalone scripts, performing resource imports, or managing engine termination based on frame counts.

## API

### Properties

*   **`GodotProject Project`**
    The configuration object representing the target Godot project to be executed.
*   **`GodotExecutable Executable`**
    The configuration object representing the Godot engine binary to be used for the execution.
*   **`TimeSpan DefaultTimeout`**
    The default duration allowed for an execution operation before it is considered timed out and potentially terminated.

### Methods

*   **`Task<GodotRunResult> RunProjectAsync(CancellationToken cancellationToken = default)`**
    Executes the configured Godot project in headless mode.
    *   **Returns**: A `Task` containing the `GodotRunResult` upon completion.
    *   **Throws**: `OperationCanceledException` if the provided cancellation token is triggered.
*   **`Task<GodotRunResult> RunScriptAsync(string scriptPath, IEnumerable<string> arguments = null, CancellationToken cancellationToken = default)`**
    Executes a standalone Godot script in headless mode.
    *   **Parameters**: `scriptPath` (path to the GDScript or C# script), `arguments` (optional arguments to pass to the script).
    *   **Returns**: A `Task` containing the `GodotRunResult`.
*   **`Task<GodotRunResult> ImportResourcesAsync(CancellationToken cancellationToken = default)`**
    Triggers a resource import operation for the project.
    *   **Returns**: A `Task` containing the `GodotRunResult` indicating the outcome of the import process.
*   **`Task<GodotRunResult> QuitAfterFramesAsync(int frameCount, CancellationToken cancellationToken = default)`**
    Runs the engine until a specified number of frames have elapsed, then requests a graceful termination.
    *   **Parameters**: `frameCount` (the number of frames to simulate).
    *   **Returns**: A `Task` containing the `GodotRunResult`.

## Usage

### Running a Project with a Timeout
```csharp
var runner = new HeadlessRunner {
    Project = myProject,
    Executable = myGodotExe,
    DefaultTimeout = TimeSpan.FromMinutes(5)
};

using var cts = new CancellationTokenSource(runner.DefaultTimeout);
GodotRunResult result = await runner.RunProjectAsync(cts.Token);

if (result.Success) {
    Console.WriteLine("Project ran successfully.");
}
```

### Executing a Custom Import Script
```csharp
var runner = new HeadlessRunner {
    Project = myProject,
    Executable = myGodotExe
};

var scriptPath = "res://scripts/setup_assets.gd";
GodotRunResult result = await runner.RunScriptAsync(scriptPath, new[] { "--verbose" });

if (result.ExitCode != 0) {
    throw new Exception($"Script failed with exit code {result.ExitCode}");
}
```

## Notes

*   **Process Lifecycle**: `HeadlessRunner` manages the lifecycle of the external Godot process. If a task is cancelled via `CancellationToken`, the runner attempts to terminate the associated process gracefully before disposing of resources.
*   **Thread Safety**: The `HeadlessRunner` instance itself is not guaranteed to be thread-safe. It is recommended to use a separate instance per task or ensure thread-safe access if sharing instances across concurrent operations.
*   **Environment**: Ensure the `GodotExecutable` points to a valid engine binary capable of running in headless mode (`--headless` flag is typically applied by the runner internally).
*   **Execution Outcomes**: Always inspect the `GodotRunResult` returned by the asynchronous methods, as a completed `Task` does not guarantee the underlying Godot process finished successfully; check the `ExitCode` or `Success` property of the result object.
