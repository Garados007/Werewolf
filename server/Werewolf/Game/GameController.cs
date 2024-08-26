using System.Collections.Concurrent;
using System.Security.Cryptography;
using Werewolf.Theme;
using Werewolf.User;

namespace Werewolf.Game;

public class GameController : IDisposable
{
    private struct GameRoomEntry(GameRoom room)
    {
        public GameRoom Room { get; } = room;

        public Pronto.ProntoJoinToken? JoinToken { get; set; } = null;
    }

    public static GameController Current { get; }
        = new GameController();

    public static UserFactory? UserFactory { get; set; }

    public static Pronto.Pronto? Pronto { get; set; }

    private RSA rsa;

    public HashSet<Type> GameModes { get; } = [];

    public Type? DefaultGameMode { get; }

    private GameController()
    {
        // setup rsa keys
        var rsa = this.rsa = RSA.Create();
        if (System.IO.File.Exists("keys/game-controller.xml"))
        {
            rsa.FromXmlString(System.IO.File.ReadAllText("keys/game-controller.xml"));
        }
        else
        {
            if (!System.IO.Directory.Exists("keys"))
                System.IO.Directory.CreateDirectory("keys");
            System.IO.File.WriteAllText("keys/game-controller.xml", rsa.ToXmlString(true));
        }
        // collect game modes
        var baseType = typeof(GameMode);
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Serilog.Log.Debug("Game Controller: Search in assembly {name}", assembly.FullName);
            foreach (var type in assembly.GetTypes())
                if (!type.IsAbstract && type.IsAssignableTo(baseType))
                {
                    _ = GameModes.Add(type);
                    Serilog.Log.Information("Game Controller: Add game mode {mode}", type.FullName);
                }
        }
        var scores = new List<(string part, int score)>
        {
            ("werewolf", 1),
            ("base", 2),
            ("basic", 2),
            ("default", 3),
        };
        DefaultGameMode = GameModes
            .Select(x => (x, scores
                .Where(y => x.FullName?.Contains(y.part, StringComparison.OrdinalIgnoreCase) ?? false)
                .Sum(y => y.score))
            )
            .OrderByDescending(x => x.Item2)
            .Select(x => x.x)
            .FirstOrDefault(defaultValue: null);
        Serilog.Log.Information("Game Controller: Default game mode is {mode}", DefaultGameMode?.FullName);
    }

    private readonly ConcurrentDictionary<int, GameRoomEntry> rooms
        = new ConcurrentDictionary<int, GameRoomEntry>();

    private readonly HashSet<GameWebSocketConnection> wsConnections
        = new HashSet<GameWebSocketConnection>();
    private readonly ReaderWriterLockSlim lockWsConnections
        = new ReaderWriterLockSlim();

    public Pronto.ProntoJoinToken? GetJoinToken(int groupId)
    {
        var token = rooms.TryGetValue(groupId, out GameRoomEntry room) ?
            room.JoinToken : null;
        if (token is not null && token.Invalid)
            return null;
        return token;
    }

    public async Task<Pronto.ProntoJoinToken?> GetJoinTokenAsync(int groupId)
    {
        if (!rooms.TryGetValue(groupId, out GameRoomEntry room))
            return null;
        if (room.JoinToken is not null && !room.JoinToken.Invalid)
            return room.JoinToken;
        if (Pronto is null)
            return null;
        room.JoinToken = await Pronto.CreateToken("werewolf", GetLobbyToken(room.Room)).CAF();
        return room.JoinToken;
    }

    public void UpdatePronto(Pronto.ProntoServer server, Pronto.ProntoGame game)
    {
        game.Rooms = rooms.Count;
        game.Clients = wsConnections.Count;
        server.Full =
            (server.MaxClients is not null && game.Clients >= server.MaxClients.Value) ||
            (game.MaxRooms is not null && game.Rooms >= game.MaxRooms.Value);
    }

    public int CreateGame(UserInfo leader)
    {
        if (UserFactory == null)
            throw new InvalidOperationException("user factory is not set");
        var r = new Random();
        int id;
        while (rooms.ContainsKey(id = r.Next())) ;
#if ROOM_ID_1
        // this is a magic value that results in a "Test_" url
        id = unchecked((int)0xfb_2d_eb_4d);
#endif
        var room = new GameRoom(id, leader)
        {
            LeaderIsPlayer = true,
            DeadCanSeeAllRoles = true,
            AutostartVotings = true,
            UseVotingTimeouts = true,
            AutoFinishRounds = true,
        };
        if (DefaultGameMode is not null)
            room.Theme = Activator.CreateInstance(DefaultGameMode, room, UserFactory) as GameMode;
        rooms.TryAdd(id, new GameRoomEntry(room));
        room.OnEvent += OnGameEvent;
        return id;
    }

    public bool RemoveGame(int id)
    {
        return rooms.Remove(id, out _);
    }

    public GameRoom? GetGame(int id)
    {
        return rooms.TryGetValue(id, out GameRoomEntry room)
            ? room.Room
            : null;
    }

    public string GetGuestIdToken(UserId id)
    {
        ReadOnlySpan<byte> b = id.Id.ToByteArray(); // 12 B

        return Base64UrlEncode(Sign(b).Span);
    }

    public UserId? GetGuestIdFromToken(string token)
    {
        var decodedBytes = Base64UrlDecode(token);
        if (decodedBytes == null)
            return null;
        var bytes = decodedBytes.Value.Span;

        if (!Verify(bytes, 12))
            return null;

        return new UserId(bytes[..12]);
    }

    public string GetUserToken(GameRoom game, GameUserEntry entry)
        => GetUserToken(game, entry.User);

    public string GetUserToken(GameRoom game, UserInfo user)
    {
        ReadOnlySpan<byte> b1 = BitConverter.GetBytes(game.Id); // 4 B
        ReadOnlySpan<byte> b2 = user.Id.Id.ToByteArray(); // 12 B
        Span<byte> rb = stackalloc byte[16];
        b1.CopyTo(rb[0..4]);
        b2.CopyTo(rb[4..16]);

        return Base64UrlEncode(Sign(rb).Span);
    }

    public (GameRoom game, GameUserEntry entry)? GetFromToken(string token)
    {
        var decodedBytes = Base64UrlDecode(token);
        if (decodedBytes == null)
            return null;
        var bytes = decodedBytes.Value.Span;

        // verify origin
        if (!Verify(bytes, 16))
            return null;

        int gameId = BitConverter.ToInt32(bytes[0..4]);
        var userId = new UserId(bytes[4..16]);
        var game = GetGame(gameId);
        return game == null || !game.Users.TryGetValue(userId, out GameUserEntry? entry)
            ? null
            : ((GameRoom game, GameUserEntry entry)?)(game, entry);
    }

    public string GetLobbyToken(GameRoom game)
    {
        ReadOnlySpan<byte> b1 = BitConverter.GetBytes(game.Id); // 4 B

        return Base64UrlEncode(Sign(b1).Span);
    }

    public GameRoom? GetLobbyFromToken(string token)
    {
        var decodedBytes = Base64UrlDecode(token);
        if (decodedBytes == null)
            return null;
        var bytes = decodedBytes.Value.Span;

        //verify origin
        if (!Verify(bytes, 4))
            return null;

        int gameId = BitConverter.ToInt32(bytes[0..4]);
        return GetGame(gameId);
    }

    /// <summary>
    /// Sign the data to verify the origin. The signature will be appended to the data. This
    /// will add 300-400 bytes to it.
    /// </summary>
    protected ReadOnlyMemory<byte> Sign(ReadOnlySpan<byte> data)
    {
        Span<byte> signature = rsa.SignData(
            data.ToArray(),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );
        Memory<byte> full = new byte[data.Length + signature.Length];
        data.CopyTo(full.Span[..data.Length]);
        signature.CopyTo(full.Span[data.Length..]);
        return full;
    }

    /// <summary>
    /// Verify if this instance was the author of the data and no manipulation was done. The
    /// original data length is required to extract the signature from it.
    /// </summary>
    protected bool Verify(ReadOnlySpan<byte> data, int dataLength)
    {
        return rsa.VerifyData(
            data[..dataLength],
            data[dataLength..],
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );
    }

    protected static string Base64UrlEncode(ReadOnlySpan<byte> buffer)
    {
        var sb = new System.Text.StringBuilder(Convert.ToBase64String(buffer));
        sb.Replace('+', '-');
        sb.Replace('/', '_');
        var right = sb.Length;
        for (; right > 0; right--)
            if (sb[right - 1] != '=')
                break;
        if (right < sb.Length)
            sb.Remove(right, sb.Length - right);
        return sb.ToString();
    }

    protected static ReadOnlyMemory<byte>? Base64UrlDecode(string source)
    {
        var sb = new System.Text.StringBuilder(source);
        sb.Replace('-', '+');
        sb.Replace('_', '/');
        var pad = 4 - (sb.Length & 0x03);
        if (pad < 4)
            sb.Append('=', pad);
        Memory<byte> buffer = new byte[(sb.Length >> 2) * 3];
        if (!Convert.TryFromBase64String(sb.ToString(), buffer.Span, out int bytesWritten))
            return null;
        return buffer[..bytesWritten];
    }

    private void OnGameEvent(object? sender, GameEvent @event)
    {
        lockWsConnections.EnterReadLock();
        foreach (var connection in wsConnections)
            if (connection.Game == sender && @event.CanSendTo(
                connection.Game,
                connection.UserEntry.User
            ))
            {
                _ = Task.Run(async () =>
                {
                    try { await connection.SendEvent(@event); }
                    catch (Exception e)
                    {
                        Serilog.Log.Error(e, "Cannot send message {message} ({type}) to {user}",
                            @event.GameEventType,
                            @event.GetType().FullName,
                            connection.UserEntry.User.Id
                        );
                    }
                });
            }
        lockWsConnections.ExitReadLock();
    }

    public void AddWsConnection(GameWebSocketConnection connection)
    {
        lockWsConnections.EnterWriteLock();
        _ = wsConnections.Add(connection);
        lockWsConnections.ExitWriteLock();
    }

    public bool RemoveWsConnection(GameWebSocketConnection connection)
    {
        lockWsConnections.EnterWriteLock();
        var removed = wsConnections.Remove(connection);
        var empty = wsConnections.Count == 0;
        lockWsConnections.ExitWriteLock();
        if (removed)
        {
            _ = Task.Run(async () =>
            {
                // time for reconnect
                await Task.Delay(TimeSpan.FromSeconds(30));
                // check if user was reconnected
                if (connection.UserEntry.IsOnline ||
                    DateTime.Now - connection.UserEntry.LastConnectionUpdate < TimeSpan.FromSeconds(20))
                    return;
                // submit leave message
                connection.Game.SendEvent(new Theme.Events.PlayerNotification(
                    "offline-player-left",
                    new[] { connection.UserEntry.User.Id }
                ));
                // check for new leader, old leader is removed
                if (connection.Game.Leader == connection.UserEntry.User.Id &&
                    connection.Game.Users.Count > 1)
                {
                    UserId? user = connection.Game.Users
                        .Where(x => x.Key != connection.UserEntry.User.Id)
                        .Select(x => x.Key)
                        .FirstOrDefault();
                    if (user is not null)
                        connection.Game.Leader = user.Value;
                }
                // remove user
                connection.Game.RemoveParticipant(connection.UserEntry.User);
                connection.UserFactory.RemoveCachedGuest(connection.UserEntry.User.Id);
                // remove game if empty
                if (connection.Game.Users.IsEmpty)
                {
                    RemoveGame(connection.Game.Id);
                }
            });
        }
        if (empty && Program.MaintenanceMode)
            Program.CloseServer();
        return removed;
    }

    public async Task CloseAllWsConnectionsBecauseOfRestart()
    {
        lockWsConnections.EnterReadLock();
        var tasks = Task.WhenAll(
            wsConnections
                .Select(x => x.Close((MaxLib.WebServer.WebSocket.CloseReason)1012))
        );
        lockWsConnections.ExitReadLock();
        await tasks.CAF();
    }

    public void BroadcastEvent(MaxLib.WebServer.WebSocket.EventBase @event)
    {
        lockWsConnections.EnterReadLock();
        foreach (var connection in wsConnections)
            _ = connection.SendEvent(@event);
        if (@event is Events.EnterMaintenance && wsConnections.Count == 0 && Program.MaintenanceMode)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(500).CAF(); // give the server some time to send response
                Program.CloseServer();
            });
        }
        lockWsConnections.ExitReadLock();
    }

    public void Dispose()
    {
        lockWsConnections.Dispose();
        GC.SuppressFinalize(this);
    }
}
