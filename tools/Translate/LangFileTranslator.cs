using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using Serilog;

namespace Translate
{
    class LangFileTranslator
    {
        public LangFileTranslator(Priority priority, string source, string target, 
            string basePath, Report.ReportStatus report)
        {
            Priority = priority;
            Source = source;
            Target = target;
            BasePath = basePath;
            ReportStatus = report;
        }

        public Priority Priority { get; }

        public string Source { get; }

        public string Target { get; }

        public string BasePath { get; }

        public Report.ReportStatus ReportStatus { get; }

        public async Task TranslateAsync()
        {
            Log.Information("Translate {path} from {source} to {target}", BasePath, Source, Target);
            // get tree
            var tree = await GetBaseTreeAsync()
                .ConfigureAwait(false);
            // get source
            var source = await GetSourceAsync()
                .ConfigureAwait(false);
            // walk the tree and translate texts
            await WalkAsync(new Path(), source.RootElement, tree).ConfigureAwait(false);
            // reduce tree
            tree.ReduceToVisited();
            // add preflight comment
            tree.Set("#hint", $"This language file is automaticly translated from {Source} to {Target}", true);
            tree.Set("#date", System.DateTime.UtcNow.ToString("u"), true);
            // write tree
            await WriteAsync(tree).ConfigureAwait(false);
        }

        private async Task<Tree> GetBaseTreeAsync()
        {
            if (!File.Exists($"{BasePath}/{Target}.json"))
                return new Tree();
            using var file = new FileStream($"{BasePath}/{Target}.json",
                mode: FileMode.Open,
                access: FileAccess.Read,
                share: FileShare.ReadWrite);
            var d = await JsonDocument.ParseAsync(file)
                .ConfigureAwait(false);
            return Tree.From(d.RootElement);
        }
    
        private async Task<JsonDocument> GetSourceAsync()
        {
            using var file = new FileStream($"{BasePath}/{Source}.json",
                mode: FileMode.Open,
                access: FileAccess.Read,
                share: FileShare.ReadWrite);
            return await JsonDocument.ParseAsync(file)
                .ConfigureAwait(false);
        }
    
        private async Task<bool> WalkAsync(Path path, JsonElement node, Tree tree)
        {
            switch (node.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var entry in node.EnumerateObject())
                    {
                        if (!await WalkAsync(path + entry.Name, entry.Value, tree).ConfigureAwait(false))
                            return false;
                    }
                    break;
                case JsonValueKind.String:
                {
                    var text = node.GetString()!;
                    // no translation exists at the target -> just translate it
                    if (!tree.Contains(path))
                    {
                        await TranslateAsync(path, tree, text, true, null, null)
                            .ConfigureAwait(false);
                        tree.Visit(path);
                        break;
                    }
                    // get translation status
                    var report = ReportStatus.Get(path);
                    var originalTranslator = report?.Translator;
                    // translation was marked as custom, donnot change anything here
                    if (report?.IsCustomTranslation ?? false)
                    {
                        report = new Report.ReportRecord(path);
                        ReportStatus.Add(report);
                        tree.Visit(path);
                        break;
                    }
                    // check if translation was manipulated
                    if (report?.TargetValue is not null && 
                        tree.Get(path) != report?.TargetValue)
                    {
                        report = new Report.ReportRecord(path);
                        ReportStatus.Add(report);
                        tree.Visit(path);
                        break;
                    }
                    // get a suitable translator
                    var prio = originalTranslator is null 
                        ? null // originalTranslator is null if it never was translated
                        : Priority.GetPriority(originalTranslator);
                    var translator = Priority.GetTranslator(text, prio);
                    // translate the text if a suitable translator is found
                    if (translator is not null || report?.SourceValue != text)
                        await TranslateAsync(path, tree, text, report is null, null, prio)
                            .ConfigureAwait(false);
                    else
                    {
                        // no translation can be created. mark this as missing translation
                        if (report is null)
                            ReportStatus.Add(new Report.ReportRecord(path, text));
                    }
                    tree.Visit(path);
                }
                break;
            }
            return true;
        }

        private async Task TranslateAsync(Path path, Tree tree, string value, bool mark,
            int? minBound, int? maxBound)
        {
            var translator = Priority.GetTranslator(value, maxBound, minBound);
            if (translator is null)
            {
                if (mark)
                    ReportStatus.Add(new Report.ReportRecord(path, value));
                return;
            }
            Log.Debug("Translate ({translator}): {path}", translator.Key, path);
            var result = await translator.GetTranslationAsync(Source, Target, value).ConfigureAwait(false);
            if (result is null)
            {
                // try to use a different translator
                var newMin = Priority.GetPriority(translator.Key);
                if (newMin is not null)
                {
                    await TranslateAsync(path, tree, value, mark, newMin, maxBound)
                        .ConfigureAwait(false);
                    return;
                }
                // no translator can be used. Report it as untranslated
                if (mark)
                    ReportStatus.Add(new Report.ReportRecord(path, value));
                return;
            }
            tree.Set(path, result, true);
            ReportStatus.Add(new Report.ReportRecord(path, value, translator.Key, result));
        }
    
        private async Task WriteAsync(Tree tree)
        {
            using var file = new FileStream($"{BasePath}/{Target}.json", 
                mode: FileMode.OpenOrCreate,
                access: FileAccess.Write,
                share: FileShare.ReadWrite
            );
            var writer = new Utf8JsonWriter(file, new JsonWriterOptions
            {
                Indented = true,
            });
            tree.Write(writer);
            await writer.FlushAsync().ConfigureAwait(false);
            await file.FlushAsync().ConfigureAwait(false); 
            file.SetLength(file.Position);
        }
    }
}