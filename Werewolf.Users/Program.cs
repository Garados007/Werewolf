using MaxLib.Ini;
using MaxLib.Ini.Parser;
using sRPC.TCP;
using System;
using System.Net;
using Serilog;
using Serilog.Events;

namespace Werewolf.Users
{
    internal class Program
    {
        private static void Main()
        {
            var config = new IniParser().Parse("config.ini");
            var group = config.GetGroup("user-db") ?? new IniGroup("user-db");
            using var db = new Database(group.GetString("db-path", "werewolf-user.litedb"));

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(LogEventLevel.Verbose,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Verbose("init api");
            TcpApiServer<Api.UserNotificationClient, ApiServer>? api = null;
            api = new TcpApiServer<Api.UserNotificationClient, ApiServer>(
                new IPEndPoint(
                    IPAddress.Any,
                    group.GetInt32("api-port", 30600)
                ),
                _ => { },
                server => server.Set(db, api!)
            );
            try
            {
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    Console.ReadLine();
                }
                else 
                {
                    while (Console.ReadKey().Key != ConsoleKey.Q) ;
                    Console.Write('\b');
                }
            }
            finally
            {
                api?.Dispose();
            }
        }
    }
}
