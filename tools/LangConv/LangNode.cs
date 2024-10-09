using System.Text.Json;
using YamlDotNet.RepresentationModel;

namespace LangConv;

[Serializable]
internal sealed class LangParsingException(string file, YamlDotNet.Core.Mark pos, string path, string message) : Exception(message)
{
    public string File { get; } = file;
    public YamlDotNet.Core.Mark Pos { get; } = pos;
    public string Path { get; } = path;

    public override string ToString()
    {
        return $"{Message} at {Path} in {File}:{Pos.Line}:{Pos.Column}";
    }
}

internal sealed class LangNode
{
    public HashSet<string> Languages { get; } = [];

    public Dictionary<string, LangEntry> Entries { get; } = [];

    public Dictionary<string, LangNode> Nodes { get; } = [];

    public LangNode Clone()
    {
        var clone = new LangNode();
        foreach (var language in Languages)
            _ = clone.Languages.Add(language);
        foreach (var (name, entry) in Entries)
            clone.Entries.Add(name, entry);
        foreach (var (name, node) in Nodes)
            clone.Nodes.Add(name, node.Clone());
        return clone;
    }

    public void WithDefault(LangNode other)
    {
        Languages.UnionWith(other.Languages);
        foreach (var (key, entry) in other.Entries)
        {
            _ = Entries.TryAdd(key, entry);
        }
        foreach (var (key, node) in other.Nodes)
        {
            if (Nodes.TryGetValue(key, out var current))
            {
                current.WithDefault(node);
            }
            else
            {
                Nodes.Add(key, node.Clone());
            }
        }
    }

    public static Task<LangNode> LoadAsync(string file, string language)
    {
        return Task.Run(() =>
        {
            using var input = new StreamReader(file);
            var yaml = new YamlStream();
            yaml.Load(input);
            if (yaml.Documents.Count == 0)
                return new();
            var node = yaml.Documents[0].RootNode as YamlMappingNode
                ?? throw new AbortException(AbortCode.CannotParseData, $"Root element is not a valid yaml object: {file}");
            try
            {
                return Get(node, language, file);
            }
            catch (LangParsingException e)
            {
                throw new AbortException(AbortCode.CannotParseData, e.ToString());
            }
        });
    }

    private static readonly JsonWriterOptions options = new()
    {
        Indented = true,
    };

    public async Task WriteAsync(string directory)
    {
        if (!Directory.Exists(directory))
            _ = Directory.CreateDirectory(directory);
        await Task.WhenAll(
            Languages.Select(x => WriteAsync(Path.Combine(directory, $"{x}.json"), x))
        );
    }

    private async Task WriteAsync(string file, string language)
    {
        using var stream = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
        using var writer = new Utf8JsonWriter(stream, options);
        Write(writer, language);
        await writer.FlushAsync();
        await stream.FlushAsync();
        stream.SetLength(stream.Position);
    }

    private void Write(Utf8JsonWriter writer, string language)
    {
        if (Entries.TryGetValue(language, out var entry))
        {
            writer.WriteStringValue(entry.Text);
        }
        else
        {
            writer.WriteStartObject();
            foreach (var (name, node) in Nodes)
            {
                if (!node.Languages.Contains(language))
                    continue;
                writer.WritePropertyName(name);
                node.Write(writer, language);
            }
            writer.WriteEndObject();
        }
    }

    public static LangNode Get(YamlMappingNode node, string language, string file)
    {
        var result = new LangNode();
        _ = result.Languages.Add(language);
        foreach (var (rawKey, rawValue) in node.Children)
        {
            if (rawKey is not YamlScalarNode key || key.Value is null)
                throw new LangParsingException(file, rawKey.Start, "", $"The key {rawKey} is not a scalar");
            if (rawValue is YamlScalarNode valueScalar && valueScalar.Value is not null)
            {
                result.Nodes.Add(key.Value, new LangNode
                {
                    Entries = { { language, new(valueScalar.Value, file, rawValue.Start) } },
                    Languages = { language }
                });
                continue;
            }
            if (rawValue is YamlMappingNode valueNode)
            {
                LangNode subNode;
                try { subNode = Get(valueNode, language, file); }
                catch (LangParsingException e)
                {
                    throw new LangParsingException(file, e.Pos, $"{key.Value}.{e.Path}", e.Message);
                }
                result.Nodes.Add(key.Value, subNode);
                continue;
            }
            throw new LangParsingException(file, rawValue.Start, key.Value, $"value is not a supported node type ({rawValue.NodeType})");
        }
        return result;
    }

    public void Merge(LangNode other, bool allowDuplicateStrings = false)
    => Merge(other, null, allowDuplicateStrings);

    private void Merge(LangNode other, string? path, bool allowDuplicateStrings)
    {
        Languages.UnionWith(other.Languages);
        foreach (var (lang, entry) in other.Entries)
        {
            if (!allowDuplicateStrings && !Entries.TryAdd(lang, entry))
            {
                var current = Entries[lang];
                throw new AbortException(
                    AbortCode.CannotParseData,
                    $"Two files define the same key {path ?? "root"}: {current.SourceFile}:{current.Mark.Line}:{current.Mark.Column} <==> {entry.SourceFile}:{entry.Mark.Line}:{entry.Mark.Column}"
                );
            }
        }
        foreach (var (key, node) in other.Nodes)
        {
            if (Nodes.TryGetValue(key, out var current))
            {
                current.Merge(node, path is null ? key : $"{path}.{key}", allowDuplicateStrings);
            }
            else
            {
                Nodes.Add(key, node);
            }
        }
    }

    public void RestrictLanguages(HashSet<string> language)
    {
        var remove = new HashSet<string>(Entries.Keys);
        remove.ExceptWith(language);
        foreach (var key in remove)
            _ = Entries.Remove(key);
        remove.Clear();
        foreach (var (key, node) in Nodes)
        {
            node.RestrictLanguages(language);
            if (node.Entries.Count == 0 && node.Nodes.Count == 0)
                _ = remove.Add(key);
        }
        foreach (var key in remove)
            _ = Nodes.Remove(key);
    }
}

internal sealed record class LangEntry(
    string Text,
    string SourceFile,
    YamlDotNet.Core.Mark Mark
);
