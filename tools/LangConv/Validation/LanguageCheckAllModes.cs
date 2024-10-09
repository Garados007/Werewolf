namespace LangConv.Validation;

internal sealed class LanguageCheckAllModes : IValidator
{
    public void Check(Data data)
    {
        var packages = new HashSet<string>(data.LangIndex.Modes.Keys);
        foreach (var (namePackage, package) in data.LangModes)
        {
            if (!packages.Remove(namePackage))
            {
                Log.Error(this, $"The package {namePackage} is not defined in the index file");
                continue;
            }
            var modes = new HashSet<string>(data.LangIndex.Modes[namePackage].Themes.Keys);
            foreach (var nameMode in package.Keys)
            {
                if (!modes.Remove(nameMode))
                {
                    Log.Error(this, $"The mode `{nameMode}` from `{namePackage}` is not defined in the index file");
                }
            }
            foreach (var name in data.LangIndex.Modes[namePackage].Themes.Where(x => !x.Value.Enabled).Select(x => x.Key))
                _ = modes.Remove(name);
            foreach (var name in modes)
            {
                Log.Error(this, $"The index file expects mode `{name}` from `{namePackage}` but no language files are found");
            }
        }
        foreach (var name in packages)
        {
            Log.Error(this, $"The index files expects the package {name} but no language files are found");
        }
    }
}
