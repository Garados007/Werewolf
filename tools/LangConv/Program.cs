using CommandLine;
using LogicTools;

namespace LangConv;

internal sealed record Configuration
{
    [Option('d', "directory", HelpText = "The path of the language directory. This holds all the target files and the yml source files as well.")]
    public DirectoryInfo Directory { get; set; } = new(Environment.CurrentDirectory);

    [Option('m', "mode", HelpText = "A mode information file. The mode group key is derived from the file name.")]
    public IEnumerable<FileInfo> Modes { get; set; } = [];

    [Option("mode-package-pattern", HelpText = "The pattern for the namespace name of single modes.")]
    public string ModePackagePattern { get; set; } = "Theme.{package}.Mode_{mode}";

    [Option("no-print-missing-lang-string-warning", HelpText = "If set it wont print any warning message if a language string was not set in a language mode that has a default specified.")]
    public bool NoPrintMissingLangStringWarning { get; set; }
}

public enum AbortCode
{
    None = 0,
    FileNotFound = 1,
    CannotParseData = 2,
    ValidationError = 3,
}

[Serializable]
public class AbortException(AbortCode code, string message) : Exception(message)
{
    public AbortCode Code { get; } = code;
}

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            _ = await new Parser(x =>
            {
                x.AllowMultiInstance = true;
                x.AutoHelp = true;
                x.AutoVersion = true;
                x.HelpWriter = Console.Error;
            }).ParseArguments<Configuration>(args)
                .WithParsedAsync(Run);
        }
        catch (AbortException e)
        {
            Console.Error.WriteLine($"FATAL: {e.Message}");
            return (int)e.Code;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"FATAL: unhandled exception");
            var ex = e;
            while (ex is not null)
            {
                Console.Error.WriteLine($"{ex.GetType().FullName}: {ex}");
                Console.Error.WriteLine(ex.StackTrace);
                ex = ex.InnerException;
            }
            return 255;
        }
        return 0;
    }

    private static async Task Run(Configuration config)
    {
        Console.WriteLine("INFO: Loading files");
        await Task.WhenAll(
            Task.WhenAll(config.Modes.Select(x => LoadMode(x))),
            LoadIndexFile(config.Directory),
            LoadAllLanguageFiles(config.Directory)
        );
        Console.WriteLine("INFO: Analyze files");
        var data = GetData(config);
        if (data is null)
            return;
        Console.WriteLine("INFO: Validate content");
        if (!await Validation.IValidator.CheckAllAsync(data))
            throw new AbortException(AbortCode.ValidationError, "A validation error occurred. No files are changed.");
        Console.WriteLine("INFO: Cleanup");
        data.Cleanup();
        Console.WriteLine("INFO: Write files");
        await Task.WhenAll(
            data.LangIndex.Write(new FileInfo(Path.Combine(config.Directory.FullName, "index.json"))),
            data.LangGame.WriteAsync(Path.Combine(config.Directory.FullName, "game")),
            Task.WhenAll(
                data.LangModes.SelectMany(x =>
                {
                    return !data.LangIndex.Modes.TryGetValue(x.Key, out var indexMode)
                        ? []
                        : x.Value.Select(async y =>
                        {
                            var node = y.Value.Clone();
                            var current = y.Key;
                            while (current is not null)
                            {
                                if (!indexMode.Themes.TryGetValue(current, out var indexTheme))
                                    break;
                                if (current != y.Key)
                                    node.WithDefault(x.Value[current]);
                                current = indexTheme.Default;
                            }
                            await node.WriteAsync(Path.Combine(config.Directory.FullName, "modes", x.Key, y.Key));
                        });
                })
            )
        );
        Console.WriteLine("INFO: OK");
    }

    private static readonly Dictionary<string, Info> infos = [];
    private static LangIndex? LangIndex;
    private static LangNode? LangGame;
    private static readonly Dictionary<string, Dictionary<string, LangNode>> LangModes = [];
    private static readonly object lockObject = new();

    private static Data? GetData(Configuration config)
    {
        if (LangIndex is null || LangGame is null)
            return null;
        var modeTrees = LangModes.ToDictionary(
            x => x.Key,
            x => LangTree.Apply(
                (
                    LangIndex.Modes.TryGetValue(x.Key, out var indexMode) ?
                    x.Value.Where(
                        y => indexMode.Themes.TryGetValue(y.Key, out var indexTheme)
                            && indexTheme.Enabled
                    ) :
                    []
                ).Select(y => y.Value).Append(LangGame)
            )
        );
        return new Data(config, infos, LangIndex, LangGame, LangModes, modeTrees);
    }

    private static readonly System.Text.Json.JsonSerializerOptions jsonSerializerOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new System.Text.Json.Serialization.JsonStringEnumConverter()
        },
    };

    private static async Task LoadMode(FileInfo file)
    {
        if (!file.Exists)
            throw new AbortException(AbortCode.FileNotFound, $"Mode file {file.FullName} not found");
        Console.WriteLine($"INFO: Load mode {file.FullName}");
        using var stream = new FileStream(file.FullName, FileMode.Open);
        var info = await System.Text.Json.JsonSerializer.DeserializeAsync<LogicTools.Info>(stream, jsonSerializerOptions);
        _ = info ?? throw new AbortException(AbortCode.CannotParseData, $"Cannot read file {file.FullName}");
        var name = Path.GetFileNameWithoutExtension(file.Name);
        lock (lockObject)
            infos.Add(name, info);
    }

    private static async Task LoadIndexFile(DirectoryInfo directory)
    {
        var fullPath = Path.Combine(directory.FullName, "raw/index.yml");
        if (!File.Exists(fullPath))
            throw new AbortException(AbortCode.FileNotFound, $"Cannot read file {fullPath}");
        LangIndex = await LangIndex.Read(new FileInfo(fullPath));
    }

    private static async Task LoadAllLanguageFiles(DirectoryInfo directory)
    {
        var package = new DirectoryInfo(Path.Combine(directory.FullName, "raw/modes"));
        if (!package.Exists)
            throw new AbortException(AbortCode.FileNotFound, $"Directory not found: {package.FullName}");
        await Task.WhenAll(
            LoadLanguageFiles(
                new DirectoryInfo(Path.Combine(directory.FullName, "raw/game")),
                node => LangGame = node
            ),
            Task.WhenAll(
                package.EnumerateDirectories()
                    .Where(x => x.Name is not "." and not "..")
                    .Select(x => LoadLanguageFilesFromPackage(x))
            )
        );
    }

    private static async Task LoadLanguageFilesFromPackage(DirectoryInfo directory)
    {
        var result = new Dictionary<string, LangNode>();
        var lockResult = new object();
        await Task.WhenAll(
            directory.EnumerateFiles("*.yml")
                .Select(x =>
                {
                    var pos = x.Name.IndexOf('.');
                    return (x, pos >= 0 ? x.Name.Remove(pos) : "");
                })
                .GroupBy(x => x.Item2)
                .Select(async x =>
                {
                    var node = await LoadLanguageFiles(x.Select(x => x.x))
                        ?? throw new AbortException(AbortCode.CannotParseData, $"No files found for mode `{x.Key}` in {directory.FullName}");
                    lock (lockResult)
                        result.Add(x.Key, node);
                })
        );
        lock (lockObject)
            LangModes.Add(directory.Name, result);
    }

    private static async Task LoadLanguageFiles(DirectoryInfo directory, Action<LangNode> loaded)
    {
        if (!directory.Exists)
            throw new AbortException(AbortCode.FileNotFound, $"Language directory not found: {directory.FullName}");
        loaded(await LoadLanguageFiles(directory.EnumerateFiles())
            ?? throw new AbortException(AbortCode.FileNotFound, $"Language directory has no language files: {directory.FullName}")
        );
    }

    private static async Task<LangNode?> LoadLanguageFiles(IEnumerable<FileInfo> files)
    {
        LangNode? node = null;
        foreach (var file in files)
        {
            var lang = Path.GetExtension(Path.GetFileNameWithoutExtension(file.Name))
                ?? throw new AbortException(AbortCode.CannotParseData, $"File {file.FullName} has no language specified.");
            lang = lang.TrimStart('.');
            var newNode = await LangNode.LoadAsync(file.FullName, lang);
            if (node is null)
                node = newNode;
            else node.Merge(newNode);
        }
        return node;
    }
}
