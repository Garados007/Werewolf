using MaxLib.Ini;
using MaxLib.Ini.Parser;
using MaxLib.WebServer;
using MaxLib.WebServer.Services;
using Serilog;
using Serilog.Events;
using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Werewolf.Game
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var config = new IniParser().Parse("config.ini");
            var group = GetGroup(config, args) ?? new IniGroup("multiplexer");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(LogEventLevel.Verbose,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
            WebServerLog.LogPreAdded += WebServerLog_LogPreAdded;

            var endPoint = await GetEndpointAsync(group.GetString("user-api", "127.0.0.1:30600"));
            if (endPoint == null)
            {
                Log.Error("invalid endpoint in {key} inside the config", "user-api");
                return;
            }
            using var userController = new UserController(endPoint);
            GameController.UserFactory = userController;

            var server = new Server(new WebServerSettings(group.GetInt32("webserver-port", 8000), 5000));
            server.AddWebService(new HttpRequestParser());
            server.AddWebService(new HttpHeaderSpecialAction());
            server.AddWebService(new Http404Service());
            server.AddWebService(new HttpResponseCreator());
            server.AddWebService(new HttpSender());
            server.AddWebService(new GameService());

            var ws = new MaxLib.WebServer.WebSocket.WebSocketService();
            ws.Add(new GameWebSocketEndpoint(userController));
            server.AddWebService(ws);

            var searcher = new LocalIOMapper();
            if (System.Diagnostics.Debugger.IsAttached)
                searcher.AddFileMapping("content", "../../../../content/");
            else searcher.AddFileMapping("content", "content");
            server.AddWebService(searcher);

            server.Start();

            while (Console.ReadKey().Key != ConsoleKey.Q) ;

            server.Stop();
        }

        private static readonly Regex urlRegex = new Regex(
            @"^(?<domain>(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z0-9][a-z0-9-]{0,61}[a-z0-9]):(?<port>\d+)$",
            RegexOptions.Compiled
        );

        private static async Task<IPEndPoint?> GetEndpointAsync(string value)
        {
            if (IPEndPoint.TryParse(value, out IPEndPoint? result))
                return result;
            var match = urlRegex.Match(value);
            if (!match.Success)
                return null;
            var ips = await Dns.GetHostAddressesAsync(match.Groups["domain"].Value);
            return ips.Length == 0 || !ushort.TryParse(match.Groups["port"].Value, out ushort port)
                ? null
                : new IPEndPoint(ips[0], port);
        }

        private static readonly MessageTemplate serilogMessageTemplate =
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
            return args.Length == 0
                ? file.GetGroup("game-server")
                : GetGroup(file, args[0]);
        }
    }
}
