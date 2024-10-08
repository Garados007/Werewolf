using System.Diagnostics.CodeAnalysis;
using LogicTools;

namespace LangConv.Validation;

internal sealed class LanguageValidateModeSettings : IValidator
{
    public void Check(Data data)
    {
        foreach (var (modeName, mode) in data.LangIndex.Modes)
        {
            if (!GetInfo(data, modeName, out var info))
                continue;
            foreach (var (themeName, theme) in mode.Themes)
            {
                if (!theme.Enabled)
                    continue;
                foreach (var ignore in theme.IgnoreCharacter)
                    if (!info.Characters.Contains(ignore))
                        Log.Error(this, $"Character `{ignore}` not defined that was listed in the ignore list of `{modeName}`:`{themeName}`");
                if (theme.Default is null)
                    continue;
                if (!mode.Themes.TryGetValue(theme.Default, out var defaultTheme))
                {
                    Log.Error(this, $"Theme `{theme.Default}` not found that was defined as default for `{modeName}`:`{themeName}`");
                    continue;
                }
                if (!defaultTheme.Enabled)
                {
                    Log.Error(this, $"The default theme of `{modeName}`:`{themeName}` is disabled.");
                    continue;
                }
                foreach (var ignore in defaultTheme.IgnoreCharacter)
                    if (!theme.IgnoreCharacter.Contains(ignore))
                        Log.Error(this, $"Default theme ignore character `{ignore}` but `{modeName}`:`{themeName}` doesn't.");
                var used = new List<string> { themeName };
                string? check = theme.Default;
                while (check is not null)
                {
                    if (used.Contains(check))
                    {
                        Log.Error(this, $"The theme dependency has a circle {string.Join(" -> ", used)} -> {check} in {modeName}");
                        break;
                    }
                    if (!mode.Themes.TryGetValue(check, out defaultTheme) || !defaultTheme.Enabled)
                        break;
                    used.Add(check);
                    check = defaultTheme.Default;
                }
            }
        }
    }

    private static bool GetInfo(Data data, string name, [NotNullWhen(true)] out Info? info)
    {
        foreach (var (packageName, package) in data.Infos)
        {
            foreach (var mode in package.Modes)
            {
                var fullName = data.Config.ModePackagePattern
                    .Replace("{package}", packageName)
                    .Replace("{mode}", mode);
                if (fullName == name)
                {
                    info = package;
                    return true;
                }
            }
        }
        info = null;
        return false;
    }
}
