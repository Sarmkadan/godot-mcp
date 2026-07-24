using System;
using GodotMcp.Core.Project;

var tempDir = "/tmp/test-path-resolver";
System.IO.Directory.CreateDirectory(tempDir);

var resolver = new PathResolver(tempDir);
Console.WriteLine($"Root path: {resolver.RootPath}");

try
{
    var resolved = resolver.Resolve("res://scene.tscn");
    Console.WriteLine($"Resolved: {resolved}");
    Console.WriteLine("SUCCESS");
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: {ex.Message}");
}

System.IO.Directory.Delete(tempDir, true);
