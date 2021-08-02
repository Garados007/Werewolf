using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System;
using System.Collections.Concurrent;
using Werewolf.Theme;
using Werewolf.Users.Api;

namespace Werewolf.Game
{
    public class GameController : IDisposable
    {
        public static GameController Current { get; }
            = new GameController();

        public static UserFactory? UserFactory { get; set; }

        private GameController() { }

        private readonly ConcurrentDictionary<int, GameRoom> rooms
            = new ConcurrentDictionary<int, GameRoom>();

        private readonly HashSet<GameWebSocketConnection> wsConnections
            = new HashSet<GameWebSocketConnection>();
        private readonly ReaderWriterLockSlim lockWsConnections
            = new ReaderWriterLockSlim();

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
            var room = new GameRoom(id, leader);
            room.Theme = new Theme.Default.DefaultTheme(room, UserFactory);
            rooms.TryAdd(id, room);
            room.OnEvent += OnGameEvent;
            return id;
        }

        public bool RemoveGame(int id)
        {
            return rooms.Remove(id, out _);
        }

        public GameRoom? GetGame(int id)
        {
            return rooms.TryGetValue(id, out GameRoom? room)
                ? room
                : null;
        }

        public static string GetUserToken(GameRoom game, GameUserEntry entry)
            => GetUserToken(game, entry.User);

        public static string GetUserToken(GameRoom game, UserInfo user)
        {
            ReadOnlySpan<byte> b1 = BitConverter.GetBytes(game.Id); // 4 B
            ReadOnlySpan<byte> b2 = user.Id.Id.ToByteArray(); // 12 B
            Span<byte> rb = stackalloc byte[16];
            b1.CopyTo(rb[0..4]);
            b2.CopyTo(rb[4..16]);
            return Convert.ToBase64String(rb).Replace('/', '-').Replace('+', '_').TrimEnd('=');
        }

        public (GameRoom game, GameUserEntry entry)? GetFromToken(string token)
        {
            token = token.Replace('-', '/').Replace('_', '+') + "==";
            Span<byte> bytes = stackalloc byte[16];
            if (!Convert.TryFromBase64String(token, bytes, out int bytesWritten) || bytesWritten != 16)
                return null;

            int gameId = BitConverter.ToInt32(bytes[0..4]);
            UserId userId = new UserId
            {
                Id = Google.Protobuf.ByteString.CopyFrom(bytes[4..16]),
            };
            var game = GetGame(gameId);
            return game == null || !game.Users.TryGetValue(userId, out GameUserEntry? entry)
                ? null
                : ((GameRoom game, GameUserEntry entry)?)(game, entry);
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
                    _ = Task.Run(async () => await connection.SendEvent(@event));
                }
            lockWsConnections.ExitReadLock();
        }

        public void AddWsConnection(GameWebSocketConnection connection)
        {
            lockWsConnections.EnterWriteLock();
            _ = wsConnections.Add(connection);
            lockWsConnections.ExitWriteLock();
        }

        public void RemoveWsConnection(GameWebSocketConnection connection)
        {
            lockWsConnections.EnterWriteLock();
            _ = wsConnections.Remove(connection);
            lockWsConnections.ExitWriteLock();
        }

        public void Dispose()
        {
            lockWsConnections.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}