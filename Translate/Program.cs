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
            
            var tr = new LangFileTranslator(
                new DeepL(""),
                "de",
                "en",
                "DE",
                "EN-GB",
                "../content/lang/root"
            );
            await tr.TranslateAsync().ConfigureAwait(false);
        }
    }
}
