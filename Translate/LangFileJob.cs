using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace Translate
{
    public class LangFileJob : Job
    {
        public LangFileJob(JsonElement json) : base(json)
        {
        }

        public override async Task Execute(Priority priority, Report.ReportGenerator reportGenerator)
        {
            await Execute(
                priority,
                new DirectoryInfo(LanguageRoot),
                ReportRoot,
                reportGenerator
            ).ConfigureAwait(false);
        }

        private async Task Execute(Priority priority, DirectoryInfo workDir, string reportDir, 
            Report.ReportGenerator reportGenerator)
        {
            foreach (var subDir in workDir.EnumerateDirectories())
            {
                await Execute(priority, subDir, System.IO.Path.Combine(reportDir, subDir.Name),
                    reportGenerator)
                    .ConfigureAwait(false);
            }
            var sourcePath = System.IO.Path.Combine(workDir.FullName, $"{SourceLanguage}.json");
            if (!File.Exists(sourcePath))
                return;
            var reportPath = System.IO.Path.Combine(reportDir, $"{TargetLanguage}.json");

            var report = new Report.ReportStatus(reportPath);
            var translator = new LangFileTranslator(
                priority, 
                SourceLanguage, 
                TargetLanguage,
                workDir.FullName,
                report
            );
            await translator.TranslateAsync().ConfigureAwait(false);
            await report.Save(reportPath).ConfigureAwait(false);
            reportGenerator.AddReport(report, System.IO.Path.Combine(workDir.FullName, $"{TargetLanguage}.json"));
        }
    }
}