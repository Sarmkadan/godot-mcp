using GodotMcp.Core.Editor;
using GodotMcp.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var config = ExecutableConfig.FromArgs(args);

// Initialize executable allowlist at server startup
if (config.AllowedExecutables.Count > 0)
{
    try
    {
        ExecutableAllowlist.Initialize(config.AllowedExecutables);
        Console.Error.WriteLine($"Initialized executable allowlist with {config.AllowedExecutables.Count} entries");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Failed to initialize executable allowlist: {ex.Message}");
        Environment.Exit(1);
    }
}
else
{
    Console.Error.WriteLine("WARNING: No executable allowlist configured. Server will fail closed for security.");
}

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
var projectRoot = args.FirstOrDefault() ?? Environment.GetEnvironmentVariable("GODOT_PROJECT") ?? Environment.CurrentDirectory;
builder.Services.AddSingleton(new ProjectLocator(projectRoot));
builder.Services.AddHeadlessRunnerOptions(builder.Configuration, projectRoot);
builder.Services
    .AddMcpServer(o => o.ServerInfo = new() { Name = "godot-mcp", Version = "0.1.0" })
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
var host = builder.Build();
try
{
    await host.StartAsync();
}
catch (OptionsValidationException ex)
{
    Console.Error.WriteLine("Invalid HeadlessRunner configuration:");
    foreach (var failure in ex.Failures)
    {
        Console.Error.WriteLine($"  - {failure}");
    }
    Environment.Exit(1);
}
await host.WaitForShutdownAsync();
