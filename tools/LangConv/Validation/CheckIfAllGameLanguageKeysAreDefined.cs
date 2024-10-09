namespace LangConv.Validation;

internal sealed class CheckIfAllGameLanguageKeysAreDefined : IValidator
{
    public void Check(Data data)
    {
        var gameTree = new LangTree();
        gameTree.Apply(data.LangGame);
        foreach (var (modeName, expectedOriginal) in data.ModeTrees)
        {
            if (!data.LangModes.TryGetValue(modeName, out var themes) ||
                !data.LangIndex.Modes.TryGetValue(modeName, out var indexMode))
                continue;
            var expected = expectedOriginal.Clone();
            expected.Remove("theme", "event", "player-notification");
            expected.Remove("theme", "label");
            expected.Remove("theme", "scene");
            expected.Remove("theme", "phase");
            expected.Remove("theme", "character");
            expected.Remove("theme", "sequence");
            expected.Remove("theme", "voting");

            foreach (var (themeName, theme) in themes)
            {
                if (!indexMode.Themes.TryGetValue(themeName, out var indexTheme) || !indexTheme.Enabled)
                    continue;
                var tree = gameTree.Clone();
                tree.Apply(theme);
                Validate(expected, tree, null, modeName, themeName);
            }
        }
    }

    private void Error(string msg, string? path, string mode, string theme)
    {
        Log.Error(this, $"{msg}; path={path ?? "root"} mode={mode} theme={theme}");
    }

    private void Validate(LangTree expected, LangTree current, string? path, string mode, string theme)
    {
        if (expected.HasLanguageStrings && !current.HasLanguageStrings)
        {
            Error($"Node is expected to be a string but it is used as an object", path, mode, theme);
            return;
        }
        if (!expected.HasLanguageStrings && current.HasLanguageStrings)
        {
            Error($"Node is expected to be an object but is used as a string", path, mode, theme);
            return;
        }
        if (expected.HasLanguageStrings && current.HasLanguageStrings)
            return;
        foreach (var (key, nextExpected) in expected.Nodes)
        {
            var nextPath = path is null ? key : $"{path}.{key}";
            if (!current.Nodes.TryGetValue(key, out var next))
            {
                Error($"Path expected but not found", nextPath, mode, theme);
                continue;
            }
            Validate(nextExpected, next, nextPath, mode, theme);
        }
    }
}
