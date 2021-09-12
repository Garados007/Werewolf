using System.Text.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Translate
{
    public abstract class Job
    {
        public string SourceLanguage { get; }

        public string TargetLanguage { get; }

        public string ReportRoot { get; }

        public string LanguageRoot { get; }

        protected Job(JsonElement json)
        {
            SourceLanguage = json.GetProperty("source").GetString() ?? throw new ArgumentNullException();
            TargetLanguage = json.GetProperty("target").GetString() ?? throw new ArgumentNullException();
            ReportRoot = json.GetProperty("report").GetString() ?? throw new ArgumentNullException();
            LanguageRoot = json.GetProperty("root").GetString() ?? throw new ArgumentNullException();
        }

        public abstract Task Execute(Priority priority, Report.ReportGenerator reportGenerator,
            bool reportOnly
        );

        public static Job Create(JsonElement json)
        {
            switch (json.GetProperty("type").GetString())
            {
                case "lang-file": return new LangFileJob(json);
                default: throw new InvalidOperationException(
                    $"type {json.GetProperty("type").GetString()} not defined"
                );
            }
        }

        public static IEnumerable<Job> GetJobs(string file)
        {
            if (!File.Exists(file))
                yield break;
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var doc = JsonDocument.Parse(stream, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });
            foreach (var node in doc.RootElement.EnumerateArray())
                yield return Create(node);
        }
    }
}