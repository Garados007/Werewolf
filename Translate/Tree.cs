using System.Collections.Generic;
using OneOf;
using OneOf.Types;
using System.Text.Json;

namespace Translate
{
    using Nodes = SortedDictionary<string, Tree>;

    public class Tree
    {
        private OneOf<None, Nodes, (string text, bool visited)> content;

#pragma warning disable CS0649
        private static readonly None None;
#pragma warning restore CS0649

        public Tree()
        {
            content = None;
        }

        public bool Contains(Path path)
        {
            if (path.Parts.Length == 0)
                return true;
            return content.Match(
                _ => false,
                d => d.TryGetValue(path[0], out Tree? tree) && 
                    tree.Contains(path[1..]),
                _ => false
            );
        }

        public string? Get(Path path)
        {
            if (path.Parts.Length == 0)
                return content.TryPickT2(out (string text, bool) x, out _) ? x.text : null;
            return content.Match(
                _ => null,
                d => d.TryGetValue(path[0], out Tree? tree) ?
                    tree.Get(path[1..]) : null,
                _ => null
            );
        }

        public void Set(Path path, string value, bool visited = false)
        {
            if (path.Parts.Length == 0)
            {
                content = (value, visited);
                return;
            }
            content.Switch(
                _ =>
                {
                    var d = new Nodes();
                    Set(d, path, value, visited);
                    content = d;
                },
                d => Set(d, path, value, visited),
                _ =>
                {
                    var d = new Nodes();
                    Set(d, path, value, visited);
                    content = d;
                }
            );
        }

        private static void Set(Nodes nodes, Path path, string value, bool visited)
        {
            if (!nodes.TryGetValue(path[0], out Tree? tree))
            {
                nodes.Add(path[0], tree = new Tree());
            }
            tree.Set(path[1..], value, visited);
        }

        public void Visit(Path path)
        {
            if (path.Parts.Length == 0)
            {
                if (content.TryPickT2(out (string text, bool visited) v, out _))
                    content = (v.text, true);
                return;
            }
            content.Switch(
                _ => {},
                d => 
                {
                    if (d.TryGetValue(path[0], out Tree? tree))
                        tree.Visit(path[1..]);
                },
                _ => {}
            );
        }
    
        public static Tree From(JsonElement node)
        {
            var tree = new Tree();
            switch (node.ValueKind)
            {
                case JsonValueKind.Object:
                    var d = new Nodes();
                    tree.content = d;
                    foreach (var entry in node.EnumerateObject())
                    {
                        d.Add(entry.Name, From(entry.Value));
                    }
                    break;
                case JsonValueKind.String:
                    tree.content = (node.GetString()!, false);
                    break;
            }
            return tree;
        }

        public void Write(Utf8JsonWriter writer, bool onlyVisited = true)
        {
            content.Switch(
                _ =>
                {
                    writer.WriteStartObject();
                    writer.WriteEndObject();
                },
                d =>
                {
                    writer.WriteStartObject();
                    foreach (var (key, value) in d)
                    {
                        writer.WritePropertyName(key);
                        value.Write(writer, onlyVisited);
                    }
                    writer.WriteEndObject();
                },
                p =>
                {
                    if (!onlyVisited || p.visited)
                        writer.WriteStringValue(p.text);
                }
            );
        }

        public int VisitedCount
        {
            get
            {
                return content.Match(
                    _ => 0,
                    d =>
                    {
                        var sum = 0;
                        foreach (var (_, value) in d)
                            sum += value.VisitedCount;
                        return sum;
                    },
                    p => p.visited ? 1 : 0
                );
            }
        }

        public bool ReduceToVisited()
        {
            return content.Match(
                _ => false,
                d =>
                {
                    var @new = new Nodes();
                    foreach (var (key, value) in d)
                    {
                        if (value.ReduceToVisited())
                            @new.Add(key, value);
                    }
                    if (@new.Count == 0)
                    {
                        content = None;
                        return false;
                    }
                    else
                    {
                        content = @new;
                        return true;
                    }
                },
                p =>
                {
                    if (p.visited)
                        return true;
                    content = None;
                    return false;
                }
            );
        }
    }
}