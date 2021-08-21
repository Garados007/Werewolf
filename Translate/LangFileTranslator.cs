using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using Serilog;

namespace Translate
{
    class LangFileTranslator
    {
        public LangFileTranslator(DeepL deepL, string source, string target, string deepLSource, string deepLTarget, string basePath)
        {
            DeepL = deepL;
            Source = source;
            Target = target;
            DeepLSource = deepLSource;
            DeepLTarget = deepLTarget;
            BasePath = basePath;
        }

        public DeepL DeepL { get; }

        public string Source { get; }

        public string Target { get; }

        public string DeepLSource { get; }

        public string DeepLTarget { get; }

        public string BasePath { get; }

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
            Walk(new Path(), source.RootElement, tree);
            // reduce tree
            tree.ReduceToVisited();
            // add preflight comment
            tree.Set("#hint", $"This language file is automaticly translated from {Source} to {Target} using DeepL.", true);
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
    
        private bool Walk(Path path, JsonElement node, Tree tree)
        {
            switch (node.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var entry in node.EnumerateObject())
                    {
                        if (!Walk(path + entry.Name, entry.Value, tree))
                            return false;
                    }
                    break;
                case JsonValueKind.String:
                {
                    var text = node.GetString()!;
                    if (!tree.Contains(path))
                    {
                        Log.Debug("Translate {path}", path);
                        tree.Set(path, text, true);
                    }
                    else tree.Visit(path);
                }
                break;
            }
            return true;
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
        }
    }
}