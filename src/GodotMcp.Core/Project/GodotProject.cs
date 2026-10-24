using System.Text.RegularExpressions;
using GodotMcp.Core.Model;
using GodotMcp.Core.Parsing;
using GodotMcp.Core.Scenes;

namespace GodotMcp.Core.Project;

public sealed partial class GodotProject
{
    private readonly PathResolver _pathResolver;

    public GodotProject(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        RootPath = Path.GetFullPath(rootPath);
        _pathResolver = new PathResolver(RootPath);
    }

    public string RootPath { get; }
    public string ProjectFilePath => Path.Combine(RootPath, "project.godot");
    public bool Exists => File.Exists(ProjectFilePath);

    public static GodotProject Open(string rootPath)
    {
        var project = new GodotProject(rootPath);
        if (!project.Exists)
        {
            throw new FileNotFoundException("project.godot not found", project.ProjectFilePath);
        }

        return project;
    }

    public ProjectInfo LoadInfo()
    {
        var document = TscnParser.Parse("[gd_project]\n" + File.ReadAllText(ProjectFilePath));
        var sections = document.Sections;
        string? Get(string sectionName, string key)
        {
            var section = sectionName.Length == 0
                ? document.Descriptor
                : sections.FirstOrDefault(s => s.Name == sectionName);
            return section?.GetProperty(key) switch
            {
                GodotString s => s.Value,
                { } v => v.ToTscnString(),
                null => null
            };
        }
        var app = sections.FirstOrDefault(s => s.Name == "application");
        var features = app?.GetProperty("config/features") is GodotConstructor { Name: "PackedStringArray" } psa
            ? psa.Arguments.OfType<GodotString>().Select(s => s.Value).ToList()
            : (IReadOnlyList<string>)[];
        var autoloads = sections.FirstOrDefault(s => s.Name == "autoload")?.Properties.Select(p => p.Key).ToList() ?? [];
        GodotVersion? engine = features.FirstOrDefault(f => GodotVersion.TryParse(f, out _)) is { } fv ? GodotVersion.Parse(fv) : null;
        return new ProjectInfo
        {
            RootPath = RootPath,
            Name = Get("application", "config/name") ?? "",
            Description = Get("application", "config/description"),
            MainScene = Get("application", "run/main_scene"),
            ConfigVersion = document.Descriptor.GetProperty("config_version") is GodotInt cv ? cv.Value : 0,
            Features = features,
            AutoloadSingletons = autoloads,
            EngineVersion = engine
        };
    }

    public string ResolveResPath(string resPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resPath);
        return _pathResolver.Resolve(resPath);
    }

    public string ToResPath(string absolutePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(absolutePath);
        return _pathResolver.ToResPath(absolutePath);
    }

    public IEnumerable<string> EnumerateFiles(params string[] extensions) =>
        EnumerateFiles(new ProjectAnalyzerOptions(), extensions);

    /// <summary>
    /// Enumerates project files as res:// paths, honouring the depth, count, ignored-directory and extension limits in <paramref name="scan"/>.
    /// Throws <see cref="InvalidOperationException"/> when more than <see cref="ProjectAnalyzerOptions.MaxFileCount"/> files match.
    /// </summary>
    public IEnumerable<string> EnumerateFiles(ProjectAnalyzerOptions scan, params string[] extensions)
    {
        ArgumentNullException.ThrowIfNull(scan);
        var wanted = extensions.Select(e => e.StartsWith('.') ? e : "." + e).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var restricted = scan.ExtensionFilters.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ignored = scan.IgnoredDirectories.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var count = 0;
        foreach (var file in WalkFiles(RootPath, 0, scan, ignored))
        {
            var extension = Path.GetExtension(file);
            if (wanted.Count > 0 && !wanted.Contains(extension))
            {
                continue;
            }

            if (restricted.Count > 0 && !restricted.Contains(extension))
            {
                continue;
            }

            if (scan.MaxFileCount > 0 && ++count > scan.MaxFileCount)
            {
                throw new InvalidOperationException($"Project scan matched more than {scan.MaxFileCount} files; raise ProjectAnalyzer:MaxFileCount or narrow the extension filters.");
            }

            yield return ToResPath(file);
        }
    }

    static IEnumerable<string> WalkFiles(string directory, int depth, ProjectAnalyzerOptions scan, HashSet<string> ignored)
    {
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            yield return file;
        }

        if (scan.MaxScanDepth > 0 && depth >= scan.MaxScanDepth)
        {
            yield break;
        }

        foreach (var subdirectory in Directory.EnumerateDirectories(directory))
        {
            if (ignored.Contains(Path.GetFileName(subdirectory)))
            {
                continue;
            }
            // Symbolic links are not followed, so a link cannot loop back into the tree.
            if ((File.GetAttributes(subdirectory) & FileAttributes.ReparsePoint) != 0)
            {
                continue;
            }

            foreach (var file in WalkFiles(subdirectory, depth + 1, scan, ignored))
            {
                yield return file;
            }
        }
    }

    public IEnumerable<string> FindScenes() => EnumerateFiles(".tscn");
    public IEnumerable<string> FindResources() => EnumerateFiles(".tres", ".res", ".gdshader", ".material");
    public IEnumerable<string> FindScripts() => EnumerateFiles(".cs", ".gd");

    public TscnDocument LoadDocument(string resPath) => TscnParser.ParseFile(ResolveResPath(resPath));

    public void SaveDocument(string resPath, TscnDocument document) =>
        File.WriteAllText(ResolveResPath(resPath), document.Serialize());

    public SceneTree LoadScene(string resPath) => SceneTreeBuilder.Build(LoadDocument(resPath), resPath);

    public ScriptInfo InspectScript(string resPath)
    {
        var absolute = ResolveResPath(resPath);
        var text = File.Exists(absolute) ? File.ReadAllText(absolute) : "";
        var language = Path.GetExtension(absolute).Equals(".gd", StringComparison.OrdinalIgnoreCase) ? "gdscript" : "csharp";
        string? className = null;
        string? baseType = null;
        if (language == "csharp")
        {
            var match = CSharpClassRegex().Match(text);
            if (match.Success)
            {
                className = match.Groups["name"].Value;
                baseType = match.Groups["base"].Success ? match.Groups["base"].Value : null;
            }
        }
        else
        {
            var nameMatch = GdClassNameRegex().Match(text);
            if (nameMatch.Success)
            {
                className = nameMatch.Groups[1].Value;
            }

            var extendsMatch = GdExtendsRegex().Match(text);
            if (extendsMatch.Success)
            {
                baseType = extendsMatch.Groups[1].Value;
            }
        }
        return new ScriptInfo(resPath, language, className, baseType);
    }

    [GeneratedRegex(@"(?:public\s+)?(?:sealed\s+|abstract\s+)*(?:partial\s+)?class\s+(?<name>\w+)(?:\s*:\s*(?<base>[\w.]+))?", RegexOptions.Multiline)]
    private static partial Regex CSharpClassRegex();
    [GeneratedRegex(@"^class_name\s+(\w+)", RegexOptions.Multiline)]
    private static partial Regex GdClassNameRegex();
    [GeneratedRegex(@"^extends\s+([\w.""/]+)", RegexOptions.Multiline)]
    private static partial Regex GdExtendsRegex();
}
