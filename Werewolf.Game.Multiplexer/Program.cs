using System.Collections.Generic;
using MaxLib.Ini;
using MaxLib.Ini.Parser;
using Serilog;
using Serilog.Events;
using System;
using System.Net;
using MaxLib.WebServer;
using MaxLib.WebServer.Services;

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

            WebServerLog.LogPreAdded += WebServerLog_LogPreAdded;

            var server = new Server(new WebServerSettings(group.GetInt32("webserver-port", 8000), 5000));
            server.AddWebService(new HttpRequestParser());
            server.AddWebService(new HttpHeaderSpecialAction());
            server.AddWebService(new Http404Service());
            server.AddWebService(new HttpResponseCreator());
            server.AddWebService(new HttpSender());

            var searcher = new LocalIOMapper();
            if (System.Diagnostics.Debugger.IsAttached)
                searcher.AddFileMapping("content", "../../../../content/");
            else searcher.AddFileMapping("content", "content");
            server.AddWebService(searcher);

            server.Start();

            while (Console.ReadKey().Key != ConsoleKey.Q);

            server.Stop();
        }

        static readonly MessageTemplate serilogMessageTemplate =
            new Serilog.Parsing.MessageTemplateParser().Parse(
                "{infoType}: {info}"
            );

        private static void WebServerLog_LogPreAdded(ServerLogArgs e)
        {
            e.Discard = true;
            Log.Write(new LogEvent(
                e.LogItem.Date,
                e.LogItem.Type switch
                {
                    ServerLogType.Debug => LogEventLevel.Verbose,
                    ServerLogType.Information => LogEventLevel.Debug,
                    ServerLogType.Error => LogEventLevel.Error,
                    ServerLogType.FatalError => LogEventLevel.Fatal,
                    _ => LogEventLevel.Information,
                },
                null,
                serilogMessageTemplate,
                new[]
                {
                        new LogEventProperty("infoType", new ScalarValue(e.LogItem.InfoType)),
                        new LogEventProperty("info", new ScalarValue(e.LogItem.Information))
                }
            ));
        }

        static IEnumerable<IniOption> GetClients(IniGroup group)
        {
            foreach (var entry in group)
                if (entry is IniOption option && option.Name.StartsWith("client."))
                    yield return option;
        }
    }
}
