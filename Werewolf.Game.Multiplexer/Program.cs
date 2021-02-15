using MaxLib.Ini;
using MaxLib.Ini.Parser;
using Serilog;
using Serilog.Events;
using System;

namespace Werewolf.Game.Multiplexer
{
    class Program
    {
        static void Main()
        {
            var config = new IniParser().Parse("config.ini");
            var group = config.GetGroup("user-db") ?? new IniGroup("multiplexer");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(LogEventLevel.Verbose,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }
    }
}
