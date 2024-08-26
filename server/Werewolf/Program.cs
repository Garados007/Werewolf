using System.Net;
using System.Text.RegularExpressions;
using MaxLib.Ini;
using MaxLib.Ini.Parser;
using MaxLib.WebServer;
using MaxLib.WebServer.Services;
using Serilog;
using Serilog.Events;
using Werewolf.Game;

namespace Werewolf;

internal partial class Program
{
    public static bool MaintenanceMode { get; set; }

    public static DateTime ForcedShutdown { get; set; }

    private static async Task Main(string[] args)
    {
        var config = new IniParser().Parse("config.ini");
        UseVarsFromEnv(config);
        var group = GetGroup(config, args) ?? new IniGroup("game-server");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console(LogEventLevel.Verbose,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
        WebServerLog.LogPreAdded += WebServerLog_LogPreAdded;

        LoadPlugins(config);

        using var db = new Database(config.GetGroup("db") ?? new IniGroup("db"));

        using var pronto = GameController.Pronto = new Pronto.Pronto(new Pronto.ProntoConfig(
            config.GetGroup("pronto") ?? new IniGroup("pronto")
        ));
        pronto.BeginEdit();
        var prontoServer = pronto.GetServer();
        prontoServer.Name = group.GetString("name", "default");
        prontoServer.Uri = group.GetString("domain", "http://localhost:8000/");
        var prontoGame = pronto.GetGame("werewolf");
        prontoGame.Uri = prontoServer.Uri;
        pronto.OnBeforeSendUpdate += _ =>
        {
            GameController.Current.UpdatePronto(prontoServer, prontoGame);
            prontoServer.Maintenance = MaintenanceMode;
        };
        pronto.EndEdit();

        var endPoint = await GetEndpointAsync(group.GetString("user-api", "127.0.0.1:30600"));
        if (endPoint == null)
        {
            Log.Error("invalid endpoint in {key} inside the config", "user-api");
            return;
        }
        var oAuthUserInfo = config.GetGroup("oauth")?
            .GetString("userinfo", null);
        if (oAuthUserInfo is null)
        {
            Log.Error("missing OAuth userinfo endpoint");
            return;
        }
        using var userController = new User.UserController(
            db,
            oAuthUserInfo
        );
        GameController.UserFactory = userController;

        var server = new Server(new WebServerSettings(group.GetInt32("webserver-port", 8000), 5000));
        server.AddWebService(new HttpRequestParser());
        server.AddWebService(new HttpHeaderSpecialAction());
        server.AddWebService(new Http404Service());
        server.AddWebService(new HttpResponseCreator());
        server.AddWebService(new HttpSender());
        server.AddWebService(new GameService());
        server.AddWebService(new GameRestApi().BuildService());
        server.AddWebService(new CorsService());

        var ws = new MaxLib.WebServer.WebSocket.WebSocketService();
        ws.Add(new GameWebSocketEndpoint(userController));
        ws.CloseEndpoint = new MaxLib.WebServer.WebSocket.WebSocketCloserEndpoint(
            MaxLib.WebServer.WebSocket.CloseReason.NormalClose,
            "lobby not found"
        );
        server.AddWebService(ws);

        var searcher = new LocalIOMapper();
        var target = System.Diagnostics.Debugger.IsAttached
            ? "../../../../content/"
            : "content";
        if (!System.IO.Directory.Exists(target))
            System.IO.Directory.CreateDirectory(target);
        searcher.AddFileMapping("content", target);
        server.AddWebService(searcher);

        server.Start();

        try
        {
            await Task.WhenAny(
                Task.Delay(-1, serverCloser.Token),
                Task.Run(async () =>
                {
                    if (System.IO.File.Exists("/.dockerenv"))
                    {
                        Console.WriteLine("Inside docker. Use docker to quit.");
                        await Task.Delay(-1).CAF();
                    }
                    else if (Console.IsInputRedirected)
                    {
                        Console.WriteLine("Enter to quit");
                        Console.ReadLine();
                    }
                    else
                    {
                        Console.WriteLine("Press Q Key to quit");
                        while (Console.ReadKey().Key != ConsoleKey.Q) ;
                        Console.Write('\b');
                    }
                })
            ).CAF();
        }
        catch (TaskCanceledException) { }
        serverCloser.Dispose();

        server.Stop();
    }

    private static System.Threading.CancellationTokenSource serverCloser
        = new System.Threading.CancellationTokenSource();

    public static void CloseServer()
    {
        serverCloser.Cancel();
    }

    private static readonly Regex urlRegex = CreateUrlRegex();

    [GeneratedRegex(@"^(?<domain>(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z0-9][a-z0-9-]{0,61}[a-z0-9]):(?<port>\d+)$", RegexOptions.Compiled)]
    private static partial Regex CreateUrlRegex();

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

    private static void UseVarsFromEnv(IniFile file)
    {
        var env = Environment.GetEnvironmentVariables();
        foreach (var group in file)
        {
            var prefix = group.IsRoot ? "" :
                $"{TransformName(group.Name)}_";
            foreach (var option in group.GetAll())
            {
                var name = $"{prefix}{TransformName(option.Name)}";
                if (env.Contains(name))
                {
                    var opt = env[name]?.ToString() ?? "";
                    if (option.ValueText.StartsWith('"'))
                        option.ValueText = $"\"{opt.Replace("\"", "\\\"")}\"";
                    else option.ValueText = opt;
                }
            }
        }
    }

    private static string TransformName(string name)
    {
        var sb = new System.Text.StringBuilder(name.Length);
        var skip = false;
        foreach (var @char in name)
        {
            if (char.IsLower(@char))
            {
                sb.Append(char.ToUpper(@char));
                skip = false;
                continue;
            }
            if (char.IsUpper(@char) || char.IsDigit(@char))
            {
                sb.Append(@char);
                skip = false;
                continue;
            }
            if (!skip)
            {
                sb.Append('_');
                skip = true;
            }
        }
        return sb.ToString();
    }

    private static void LoadPlugins(IniFile file)
    {
        LoadPlugins(file["game-server"].GetString("plugins", "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        if (File.Exists("plugins.txt"))
            LoadPlugins(
                File.ReadAllLines("plugins.txt")
                    .Where(x => x.Length > 0 && x[0] != '#')
            );
    }

    private static void LoadPlugins(IEnumerable<string> names)
    {
        var existing = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var name in names)
        {
            var rawName = Path.GetFileNameWithoutExtension(name);
            if (AppDomain.CurrentDomain.GetAssemblies().Any(x => x.GetName().Name == rawName))
                continue;
            var fileName = FindFile(name);
            if (fileName is null)
            {
                Log.Error("Plugin not found: {name}", name);
                continue;
            }
            Log.Information("Load Plugin {name} at {path}", name, fileName);
            // _ = AppDomain.CurrentDomain.Load(name);
            _ = System.Reflection.Assembly.LoadFile(fileName);
        }
    }

    private static string? FindFile(string name)
    {
        if (File.Exists(name))
            return Path.GetFullPath(name);
        var mainExec = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var dir = Path.GetDirectoryName(mainExec);
        if (dir is null)
            return null;
        var also = Path.Combine(dir, name);
        if (File.Exists(also))
            return Path.GetFullPath(also);
        return null;
    }
}
