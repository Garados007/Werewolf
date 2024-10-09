namespace LangConv.Validation;

internal sealed class CheckValidThemesInIndex : IValidator
{
    public void Check(Data data)
    {
        var modes = new HashSet<string>(data.LangIndex.Modes.Keys);
        foreach (var (package, info) in data.Infos)
            foreach (var mode in info.Modes)
            {
                var name = data.Config.ModePackagePattern
                    .Replace("{package}", package)
                    .Replace("{mode}", mode);
                if (!modes.Remove(name))
                    Log.Error(this, $"There is no language specification for mode `{name}`");
            }
        foreach (var name in modes)
        {
            Log.Error(this, $"There is a language specification `{name}` but no corresponding mode for it.");
        }
    }
}
