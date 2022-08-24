using Serilog;
using Serilog.Events;

namespace LangSubConfigGenerator;

public static class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Invalid number of arguments");
            Console.Error.WriteLine("Usage: <command> [path/to/project.csproj] [path/to/generated.json]");
            Environment.ExitCode = 1;
            return;
        }

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console(LogEventLevel.Debug,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        var crawler = new Crawler(args[0]);
        await crawler.Run();
        await crawler.Export(args[1]);
    }
}