using System;
using MaxLib.Ini;
using MaxLib.Ini.Parser;
using sRPC;
using sRPC.TCP;
using System.Net;

namespace Werewolf.Users
{
    class Program
    {
        static void Main()
        {
            var config = new IniParser().Parse("config.ini");
            using var db = new Database(config[0].GetString("db-path", "werewolf-user.litedb"));

            using var api = new TcpApiServer<Api.UserNotificationClient, ApiServer>(
                new IPEndPoint(
                    IPAddress.Any,
                    config[0].GetInt32("api-port", 30600)
                ),
                _ => {},
                server => server.SetDatabase(db)
            );

            while (Console.ReadKey().Key != ConsoleKey.Q);
        }
    }
}
