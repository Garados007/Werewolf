using System.Collections.Generic;
using MaxLib.Ini;
using MaxLib.Ini.Parser;
using Serilog;
using Serilog.Events;
using System;
using System.Net;
using MaxLib.WebServer;
using MaxLib.WebServer.Services;

namespace Werewolf.Game
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new IniParser().Parse("config.ini");
            var group = GetGroup(config, args) ?? new IniGroup("multiplexer");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(LogEventLevel.Verbose,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
            WebServerLog.LogPreAdded += WebServerLog_LogPreAdded;

            var server = new Server(new WebServerSettings(group.GetInt32("webserver-port", 8000), 5000));
            server.AddWebService(new HttpRequestParser());
            server.AddWebService(new HttpHeaderSpecialAction());
            server.AddWebService(new Http404Service());
            server.AddWebService(new HttpResponseCreator());
            server.AddWebService(new HttpSender());
            server.AddWebService(new GameService());

            var searcher = new LocalIOMapper();
            if (System.Diagnostics.Debugger.IsAttached)
                searcher.AddFileMapping("content", "../../../../content/");
            else searcher.AddFileMapping("content", "content");
            server.AddWebService(searcher);

            server.Start();

            while (Console.ReadKey().Key != ConsoleKey.Q) ;

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

        private static IniGroup? GetGroup(IniFile file, string name)
        {
            foreach (var group in file.GetGroups("game-server"))
                if (group.GetString("name", "") == name)
                    return group;
            return null;
        }

        private static IniGroup? GetGroup(IniFile file, string[] args)
        {
            if (args.Length == 0)
                return file.GetGroup("game-server");
            else return GetGroup(file, args[0]);
        }
    }
}
