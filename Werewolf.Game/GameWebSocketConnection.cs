using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MaxLib.WebServer.WebSocket;
using Werewolf.Theme;
using Werewolf.Users.Api;

namespace Werewolf.Game
{
    public class GameWebSocketConnection : WebSocketConnection
    {
        public GameRoom Game { get; }

        public UserInfo User { get; }

        public GameWebSocketConnection(Stream networkStream, GameRoom game, UserInfo user) 
            : base(networkStream)
        {
            Game = game;
            User = user;
            GameController.Current.AddWsConnection(this);
            Closed += (_, __) => 
            {
                GameController.Current.RemoveWsConnection(this);
            };
        }

        protected override Task ReceiveClose(CloseReason? reason, string? info)
        {
            return Task.CompletedTask;
        }

        protected override Task ReceivedFrame(Frame frame)
        {
            return Task.CompletedTask;
        }

        public async Task SendEvent(GameEvent @event)
        {
            var m = new MemoryStream();
            var writer = new Utf8JsonWriter(m);
            @event.Write(writer, Game, User);
            await writer.FlushAsync();
            
            var frame = new Frame
            {
                OpCode = OpCode.Text,
                Payload = m.ToArray()
            };
            await SendFrame(frame);
        }
    }
}