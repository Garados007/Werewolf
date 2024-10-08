using System.Text.Json;

namespace LangConv.Validation;

internal sealed class LanguageCheckIfEverythingFromThemesAreDefined : IValidator
{
    private string? namePackage;
    private string? nameTheme;
    private readonly Stack<string> callPath = [];
    private HashSet<string> langs = [];
    private bool optional;
    private bool ignoreWarning;
    private LangTree? tree;

    private void Error(string message, bool canBeWarning = false)
    {
        var msg = $"{message}; Mode={namePackage} Theme={nameTheme} Path={string.Join('.', callPath.Reverse())}";
        if (canBeWarning && optional)
        {
            if (!ignoreWarning)
                Log.Warning(this, msg);
        }
        else Log.Error(this, msg);
    }

    private void Error(string path, string message, bool canBeWarning = false)
    {
        callPath.Push(path);
        Error(message, canBeWarning);
        _ = callPath.Pop();
    }

    public void Check(Data data)
    {
        ignoreWarning = data.Config.NoPrintMissingLangStringWarning;
        foreach (var (namePackage, info) in data.Infos)
        {
            foreach (var mode in info.Modes)
            {
                var fullName = this.namePackage = data.Config.ModePackagePattern.Replace("{package}", namePackage).Replace("{mode}", mode);
                if (!data.LangModes.TryGetValue(fullName, out var package))
                    continue;
                if (!data.LangIndex.Modes.TryGetValue(fullName, out var indexMode))
                    continue;
                tree = !data.ModeTrees.TryGetValue(fullName, out tree) ? null : tree.Clone();
                foreach (var (nameTheme, theme) in package)
                {
                    if (!indexMode.Themes.TryGetValue(nameTheme, out var indexTheme) || !indexTheme.Enabled)
                        continue;
                    langs = new HashSet<string>(indexTheme.Title.Keys);
                    optional = indexTheme.Default is not null;

                    var node = data.LangGame.Clone();
                    node.Merge(theme, true);
                    this.nameTheme = nameTheme;

                    callPath.Clear();
                    CheckNodes(
                        node,
                        info.PlayerNotification.Append("offline-player-left"),
                        ["theme", "event", "player-notification"]
                    );
                    CheckNodes(
                        node,
                        info.Labels
                            .Where(x => x.Value.Target.HasFlag(LogicTools.LabelTarget.Character))
                            .Select(x => x.Key),
                        ["theme", "label"]
                    );
                    CheckNodes(node, info.Scenes, ["theme", "scene"]);
                    CheckNodes(node, info.Phases, ["theme", "phase"]);
                    CheckNodes(
                        node,
                        info.Characters.Where(x => !indexTheme.IgnoreCharacter.Contains(x)),
                        ["theme", "character"],
                        new LangTree()
                        {
                            Nodes =
                            {
                                { "info", new LangTree{ HasLanguageStrings = true }},
                                { "name", new LangTree{ HasLanguageStrings = true }},
                            }
                        });
                    if (this.tree is not null)
                        _ = this.tree.Nodes["theme"].Nodes["character"].Add(
                            info.Characters.Select(x => (x, new LangTree()
                            {
                                Nodes =
                                {
                                    { "info", new LangTree{ HasLanguageStrings = true }},
                                    { "name", new LangTree{ HasLanguageStrings = true }},
                                }
                            }))
                        );
                    var defaultSequence = new LangTree()
                        .Add("init", new LangTree { HasLanguageStrings = true, IsDefaultValue = true })
                        .Add("step", new LangTree { IsDefaultValue = true }
                            .Add(info.Sequences.Values
                                .SelectMany(x => x.Steps)
                                .Distinct()
                                .Select(x => (x, new LangTree { HasLanguageStrings = true, IsDefaultValue = true }))
                            )
                        );
                    CheckNodes(
                        node,
                        new LangTree().Add(info.Sequences.Select(info =>
                            (info.Key, new LangTree()
                                .Add("name", new LangTree { HasLanguageStrings = true })
                                .Add("step", new LangTree
                                {
                                    Default = defaultSequence.Nodes["step"]
                                }.Add(
                                    info.Value.Steps.Select(
                                        step => (step, new LangTree
                                        {
                                            HasLanguageStrings = true,
                                            Default = defaultSequence.Nodes["step"].Nodes[step]
                                        })
                                    )
                                ))
                                .Add("init", new LangTree
                                {
                                    HasLanguageStrings = true,
                                    Default = defaultSequence.Nodes["init"],
                                })
                            )
                        )).Add("default", defaultSequence),
                        ["theme", "sequence"]);
                    var defaultVoting = new LangTree()
                        .Add("options", new LangTree { IsDefaultValue = true }
                            .Add(info.Options.Select(
                                x => (x, new LangTree { HasLanguageStrings = true, IsDefaultValue = true })
                            ))
                            .Add("character", new LangTree { HasLanguageStrings = true, IsDefaultValue = true })
                            .Add("stop-voting", new LangTree { HasLanguageStrings = true, IsDefaultValue = true })
                        );
                    CheckNodes(
                        node,
                        new LangTree().Add(info.Votings.Select(voting =>
                            (voting.Key, new LangTree()
                                .Add("options", new LangTree
                                {
                                    Default = defaultVoting.Nodes["options"],
                                }
                                    .Add(voting.Value.UsedOptions.Select(name =>
                                        (name, new LangTree
                                        {
                                            HasLanguageStrings = true,
                                            Default = defaultVoting.Nodes["options"].Nodes[name],
                                        })
                                    ))
                                    .Add("character", new LangTree
                                    {
                                        HasLanguageStrings = true,
                                        Default = defaultVoting.Nodes["options"].Nodes["character"],
                                    })
                                    .Add("stop-voting", new LangTree
                                    {
                                        HasLanguageStrings = true,
                                        Default = defaultVoting.Nodes["options"].Nodes["stop-voting"],
                                    })
                                )
                                .Add("title", new LangTree
                                {
                                    HasLanguageStrings = true,
                                })
                            )
                        )).Add("default", defaultVoting)
                        .Add("default-logs", new LangTree()
                            .Add("title", new LangTree { HasLanguageStrings = true })
                        ),
                        ["theme", "voting"]
                    );
                    WriteSchema(data);
                    this.tree = null;
                }
            }
        }
    }

    private void WriteSchema(Data data)
    {
        if (tree is null || namePackage is null)
            return;
        Console.WriteLine($"INFO: Write schema {namePackage}");
        var path = Path.Combine(data.Config.Directory.FullName, "raw/modes", namePackage, $"schema.json");
        using var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        tree.Write(writer, true);
        writer.Flush();
        stream.Flush();
        stream.SetLength(stream.Position);
    }

    private void CheckNodes(LangNode node, IEnumerable<string> names, ReadOnlySpan<string> path,
        LangTree? tree = null)
    {
        tree ??= new LangTree
        {
            HasLanguageStrings = true,
        };
        var newTree = new LangTree();
        foreach (var name in names)
            newTree.Nodes.Add(name, tree);
        CheckNodes(node, newTree, path);
    }

    private void CheckNodes(LangNode node, LangTree tree, ReadOnlySpan<string> path)
    {
        var langs = new HashSet<string>(this.langs);
        callPath.Clear();
        LangTree? parentTree = null;
        LangTree? currentTree = this.tree;
        string? lastKey = null;
        while (path.Length > 0)
        {
            if (!node.Nodes.TryGetValue(path[0], out var next))
            {
                Error(path[0], "path not found", true);
                return;
            }
            callPath.Push(path[0]);
            foreach (var lang in this.langs)
            {
                if (!langs.Contains(lang) || next.Languages.Contains(lang))
                    continue;
                Error($"path not found for language `{lang}`", true);
                _ = langs.Remove(lang);
            }
            parentTree = currentTree;
            if (currentTree is not null && !currentTree.Nodes.TryGetValue(path[0], out currentTree))
                currentTree = null;
            lastKey = path[0];
            node = next;
            path = path[1..];
        }
        CheckSubTree(langs, node, tree);
        if (currentTree is not null)
        {
            if (parentTree is null || lastKey is null)
            {
                this.tree = tree;
            }
            else
            {
                parentTree.Nodes[lastKey] = tree;
            }
        }
    }

    private void CheckSubTree(HashSet<string> langs, LangNode node, LangTree tree)
    {
        if (tree.HasLanguageStrings)
        {
            foreach (var lang in langs)
                if (!node.Entries.ContainsKey(lang))
                {
                    if (tree.Default is not null || (tree.IsDefaultValue && !tree.RequireValue))
                    {
                        if (tree.Default is not null)
                            tree.Default.RequireValue = true;
                    }
                    else
                    {
                        Error($"path not defined for language `{lang}`", true);
                    }
                }
            var invalid = new HashSet<string>(node.Nodes.SelectMany(x => x.Value.Languages));
            foreach (var lang in invalid)
                Error($"path has sub sequential paths which is not expected for language `{lang}`");
        }
        else
        {
            foreach (var lang in node.Entries.Keys)
                Error($"path has a string but expects an object for language `{lang}`");
            var names = new HashSet<string>(node.Nodes.Keys);
            foreach (var (name, sub) in tree.Nodes)
            {
                if (!names.Remove(name))
                {
                    if (sub.Default is not null || (sub.IsDefaultValue && !sub.RequireValue))
                    {
                        if (sub.Default is not null)
                            sub.Default.RequireValue = true;
                    }
                    else
                    {
                        Error(name, $"path not found for any language", true);
                    }
                    continue;
                }
                var nextLangs = new HashSet<string>(langs);
                foreach (var lang in langs)
                    if (!node.Languages.Contains(lang))
                    {
                        _ = nextLangs.Remove(lang);

                        if (sub.Default is not null || (sub.IsDefaultValue && !sub.RequireValue))
                        {
                            if (sub.Default is not null)
                                sub.Default.RequireValue = true;
                        }
                        else
                        {
                            Error($"path not defined for language `{lang}`", true);
                        }
                    }
                callPath.Push(name);
                CheckSubTree(nextLangs, node.Nodes[name], sub);
                _ = callPath.Pop();
            }
            foreach (var name in names)
                Error(name, $"path defined but not expected from language(s) {{{string.Join(", ", node.Nodes[name].Languages)}}}");
        }
    }
}
