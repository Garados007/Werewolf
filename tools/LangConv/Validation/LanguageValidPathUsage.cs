namespace LangConv.Validation;

internal sealed class LanguageValidPathUsage : IValidator
{
    public void Check(Data data)
    {
        Check(data.LangGame, null);
        foreach (var mode in data.LangModes.Values)
            foreach (var node in mode.Values)
                Check(node, null);
    }

    private void Check(LangNode node, string? path)
    {
        if (node.Nodes.Count > 0 && node.Entries.Count > 0)
        {
            foreach (var (_, entry) in node.Entries)
            {
                Log.Error(this, $"Path `{path ?? "root"}` defined a string in {entry.SourceFile}:{entry.Mark.Line}:{entry.Mark.Column} which is a group in other files");
            }
            return;
        }
        foreach (var (name, entry) in node.Nodes)
        {
            Check(entry, path is null ? name : $"{path}.{name}");
        }
    }
}
