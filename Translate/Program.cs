using System;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;

namespace Translate
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(LogEventLevel.Verbose,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
            
            var translator = new Priority();
            translator.AddTranslator(10, new Bing.BingTranslator());
            translator.AddTranslator(15, new Libre.LibreTranslator());
            translator.AddTranslator(8, new GoogleFree.GoogleTranslator());
            translator.AddTranslator(5, new DeepLFree.DeepLTranslator(
                wait: !ContainsFlag(args, "--no-wait"),
                retryRateLimit: ContainsFlag(args, "--retry-ratelimit")
            ));
            
            // var report = new Report.ReportStatus("../content/lang-info/root/en.json");
            // var tr = new LangFileTranslator(
            //     translator,
            //     "de",
            //     "en",
            //     "../content/lang/root",
            //     report
            // );
            // await tr.TranslateAsync().ConfigureAwait(false);
            // await report.Save("../content/lang-info/root/en.json");
            using var reportGenerator = new Report.ReportGenerator(
                "../content/report/translation.html", 
                "Translation Status",
                ".."
            );
            foreach (var job in Job.GetJobs("jobs.json"))
                await job.Execute(translator, reportGenerator, ContainsFlag(args, "--report-only"))
                    .ConfigureAwait(false);
            reportGenerator.WriteFinalReport(translator);
        }

        static bool ContainsFlag(string[] args, string flag)
        {
            foreach (var arg in args)
                if (arg == flag)
                    return true;
            return false;
        }
    }
}
