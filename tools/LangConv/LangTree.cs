using System.Text.Json;

namespace LangConv;


/// <summary>
/// Contains basic information of each language path
/// </summary>
internal sealed class LangTree
{
    public bool HasLanguageStrings { get; set; }

    public Dictionary<string, LangTree> Nodes { get; } = [];

    public LangTree? Default { get; set; }

    public bool IsDefaultValue { get; set; }

    public bool RequireValue { get; set; }

    public LangTree Clone()
    {
        return new LangTree
        {
            HasLanguageStrings = HasLanguageStrings,
            IsDefaultValue = IsDefaultValue,
            RequireValue = RequireValue,
        }
        .Add(Nodes.Select(x => (x.Key, x.Value.Clone())));
    }

    public LangTree Add(IEnumerable<(string, LangTree)> values)
    {
        foreach (var (name, tree) in values)
            Nodes[name] = tree;
        return this;
    }

    public LangTree Add(string key, LangTree value)
    {
        Nodes[key] = value;
        return this;
    }

    public void Apply(LangNode node)
    {
        HasLanguageStrings |= node.Entries.Count > 0;
        foreach (var (key, value) in node.Nodes)
        {
            if (!Nodes.TryGetValue(key, out var next))
            {
                Nodes.Add(key, next = new());
            }
            next.Apply(value);
        }
    }

    public static LangTree Apply(IEnumerable<LangNode> nodes)
    {
        return nodes.Aggregate(new LangTree(), (tree, node) =>
        {
            tree.Apply(node);
            return tree;
        });
    }

    public void Remove(params string[] path)
    {
        LangTree? parent = null;
        string? key = null;
        var current = this;
        foreach (var element in path)
        {
            parent = current;
            if (!current.Nodes.TryGetValue(element, out current))
                return;
            key = element;
        }
        if (parent is not null && key is not null)
            _ = parent.Nodes.Remove(key);
        else
        {
            current.Nodes.Clear();
            HasLanguageStrings = false;
        }
    }

    public void Write(Utf8JsonWriter writer, bool isRoot = false)
    {
        writer.WriteStartObject();
        if (isRoot)
            writer.WriteString("$schema", "http://json-schema.org/draft-07/schema#");
        if (HasLanguageStrings && Nodes.Count > 0)
        {
            writer.WriteStartArray("type");
            writer.WriteStringValue("string");
            writer.WriteStringValue("object");
            writer.WriteEndArray();
        }
        else if (HasLanguageStrings)
        {
            writer.WriteString("type", "string");
        }
        else
        {
            writer.WriteString("type", "object");
        }

        if (Nodes.Count > 0)
        {
            writer.WriteStartObject("properties");
            foreach (var (name, tree) in Nodes)
            {
                writer.WritePropertyName(name);
                tree.Write(writer);
            }
            writer.WriteEndObject(); // properties
        }

        writer.WriteEndObject();
    }
}
