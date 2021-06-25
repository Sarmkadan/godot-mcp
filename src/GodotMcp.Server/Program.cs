using GodotMcp.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services.AddSingleton(new ProjectLocator(args.FirstOrDefault() ?? Environment.GetEnvironmentVariable("GODOT_PROJECT") ?? Environment.CurrentDirectory));
builder.Services
    .AddMcpServer(o => o.ServerInfo = new() { Name = "godot-mcp", Version = "0.1.0" })
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
await builder.Build().RunAsync();
