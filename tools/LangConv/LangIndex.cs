using System.Text.Json;
using System.Text.Json.Serialization;

namespace LangConv;

public class LangIndex
{
    public Dictionary<string, string> Languages { get; set; } = [];

    public Dictionary<string, string> Icons { get; set; } = [];

    public Dictionary<string, LangMode> Modes { get; set; } = [];

    public static Task<LangIndex> Read(FileInfo file)
    {
        return Task.Run(() =>
        {
            using var reader = new StreamReader(file.FullName);
            var parser = new YamlDotNet.Core.Parser(new YamlDotNet.Core.Scanner(reader, skipComments: true));
            return new YamlDotNet.Serialization.DeserializerBuilder()
                .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.UnderscoredNamingConvention.Instance)
                .Build()
                .Deserialize<LangIndex>(parser);
        });
    }

    private static readonly JsonSerializerOptions options = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        },
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public async Task Write(FileInfo file)
    {
        if (!file.Directory?.Exists ?? false)
            file.Directory?.Create();
        using var stream = new FileStream(file.FullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
        await JsonSerializer.SerializeAsync(stream, this, options);
        await stream.FlushAsync();
        stream.SetLength(stream.Position);
    }
}

public class LangMode
{
    public Dictionary<string, string> Title { get; set; } = [];

    public Dictionary<string, LangTheme> Themes { get; set; } = [];
}

public class LangTheme
{
    public Dictionary<string, string> Title { get; set; } = [];

    public string? Default { get; set; }

    public bool Enabled { get; set; } = true;

    public List<string> IgnoreCharacter { get; set; } = [];
}
