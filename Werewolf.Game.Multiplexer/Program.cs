using System.Collections.Generic;
using MaxLib.Ini;
using MaxLib.Ini.Parser;
using Serilog;
using Serilog.Events;
using System;
using System.Net;

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

            using var connector = new ClientConnector(
                group.GetInt32("api-port", 30700),
                group.GetInt32("game-server-timeout", 5000)
            );
            
            foreach (var option in GetClients(group))
            {
                if (!IPEndPoint.TryParse(option.String, out IPEndPoint? endPoint))
                {
                    Log.Error("Invalid IP endpoint {endpoint} for {key}", option.String, option.Name);
                    continue;
                }
                connector.AddConnection(endPoint, group.GetInt32("api-timeout", 5000));
            }

            while (Console.ReadKey().Key != ConsoleKey.Q);
        }

        static IEnumerable<IniOption> GetClients(IniGroup group)
        {
            foreach (var entry in group)
                if (entry is IniOption option && option.Name.StartsWith("client."))
                    yield return option;
        }
    }
}
