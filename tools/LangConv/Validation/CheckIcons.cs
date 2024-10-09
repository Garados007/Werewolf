namespace LangConv.Validation;

internal sealed class CheckIcons : IValidator
{
    public void Check(Data data)
    {
        foreach (var lang in data.LangIndex.Languages.Keys)
        {
            if (!data.LangIndex.Icons.ContainsKey(lang))
                Log.Error(this, $"For the language {lang} is no icon defined");
        }
        var dir = Path.Combine(data.Config.Directory.FullName, "../vendor/flag-icon-css/flags/4x3");
        if (!Directory.Exists(dir))
        {
            Log.Error(this, $"Icon directory not found: {dir}");
            return;
        }
        foreach (var (lang, icon) in data.LangIndex.Icons)
        {
            var file = Path.Combine(dir, $"{icon}.svg");
            if (!File.Exists(file))
                Log.Error(this, $"For the language {lang} is the icon file not found: {file}");
        }
    }
}
