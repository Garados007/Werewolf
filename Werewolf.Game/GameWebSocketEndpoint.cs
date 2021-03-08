using System.IO;
using MaxLib.WebServer;
using MaxLib.WebServer.WebSocket;

namespace Werewolf.Game
{
    public class GameWebSocketEndpoint : WebSocketEndpoint<GameWebSocketConnection>
    {
        public override string? Protocol => null;

        private readonly EventFactory factory = new EventFactory();

        public GameWebSocketEndpoint()
        {
            // fill the factory with the types
            factory.Add<Events.FetchRoles>();
            factory.Add<Events.SubmitRoles>();
        }

        protected override GameWebSocketConnection? CreateConnection(Stream stream, HttpRequestHeader header)
        {
            if (header.Location.DocumentPathTiles.Length != 2)
                return null;
            if (header.Location.DocumentPathTiles[0].ToLowerInvariant() != "ws")
                return null;
            var result = GameController.Current.GetFromToken(
                header.Location.DocumentPathTiles[1]
            );
            return result == null
                ? null
                : new GameWebSocketConnection(stream, factory, result.Value.game, result.Value.user);
        }
    }
}