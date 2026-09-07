using GodotMcp.Core.Editor;
using GodotMcp.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
builder.Services.AddSingleton(new ProjectLocator(args.FirstOrDefault() ?? Environment.GetEnvironmentVariable("GODOT_PROJECT") ?? Environment.CurrentDirectory));
builder.Services
    .AddMcpServer(o => o.ServerInfo = new() { Name = "godot-mcp", Version = "0.1.0" })
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
await builder.Build().RunAsync();
