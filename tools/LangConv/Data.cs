using LogicTools;

namespace LangConv;

internal sealed record class Data(
    Configuration Config,
    Dictionary<string, Info> Infos,
    LangIndex LangIndex,
    LangNode LangGame,
    Dictionary<string, Dictionary<string, LangNode>> LangModes,
    Dictionary<string, LangTree> ModeTrees
)
{
    public void Cleanup()
    {
        var langs = new HashSet<string>(LangIndex.Languages.Keys);
        LangGame.RestrictLanguages(langs);

        var removeMode = new HashSet<string>();
        foreach (var (modeName, mode) in LangModes)
        {
            if (!LangIndex.Modes.TryGetValue(modeName, out var indexMode))
            {
                _ = removeMode.Add(modeName);
                continue;
            }

            var removeTheme = new HashSet<string>();
            foreach (var (themeName, theme) in mode)
            {
                if (!indexMode.Themes.TryGetValue(themeName, out var indexTheme) || !indexTheme.Enabled)
                {
                    _ = removeTheme.Add(themeName);
                    continue;
                }

                theme.RestrictLanguages(langs);
                if (theme.Entries.Count == 0 && theme.Nodes.Count == 0)
                    _ = removeTheme.Add(themeName);
            }
            foreach (var key in removeTheme)
                _ = mode.Remove(key);
        }
        foreach (var key in removeMode)
            _ = LangModes.Remove(key);

        foreach (var (_, mode) in LangIndex.Modes)
        {
            var remove = new HashSet<string>();
            foreach (var (themeName, theme) in mode.Themes)
            {
                if (!theme.Enabled)
                    _ = remove.Add(themeName);
            }
            foreach (var key in remove)
                mode.Themes.Remove(key);
        }
    }
}
