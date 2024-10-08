namespace LangConv.Validation;

internal sealed class LanguageUsageInIndex : IValidator
{
    public void Check(Data data)
    {
        foreach (var (modeName, mode) in data.LangIndex.Modes)
        {
            foreach (var lang in mode.Title.Keys)
                if (!data.LangIndex.Languages.ContainsKey(lang))
                    Log.Error(this, $"The title language {lang} is not defined in mode {modeName}");
            foreach (var (themeName, theme) in mode.Themes)
            {
                foreach (var lang in theme.Title.Keys)
                    if (!data.LangIndex.Languages.ContainsKey(lang))
                        Log.Error(this, $"The title language {lang} is not defined in mode {modeName}, theme {themeName}");
            }
        }
    }
}
