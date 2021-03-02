using System.IO;
using MaxLib.WebServer;
using MaxLib.WebServer.WebSocket;

namespace Werewolf.Game
{
    public class GameWebSocketEndpoint : WebSocketEndpoint<GameWebSocketConnection>
    {
        public override string? Protocol => null;

        protected override GameWebSocketConnection? CreateConnection(Stream stream, HttpRequestHeader header)
        {
            if (header.Location.DocumentPathTiles.Length != 2)
                return null;
            if (header.Location.DocumentPathTiles[0].ToLower() != "ws")
                return null;
            var result = GameController.Current.GetFromToken(
                header.Location.DocumentPathTiles[1]
            );
            if (result == null)
                return null;
            return new GameWebSocketConnection(stream, result.Value.game, result.Value.user);
        }
    }
}