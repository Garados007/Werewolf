using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MaxLib.WebServer.WebSocket;
using Werewolf.Theme;
using Werewolf.Users.Api;

namespace Werewolf.Game
{
    public class GameWebSocketConnection : EventConnection
    {
        public GameRoom Game { get; }

        public UserInfo User { get; }

        public GameWebSocketConnection(Stream networkStream, EventFactory factory, GameRoom game, UserInfo user)
            : base(networkStream, factory)
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

        public async Task SendEvent(GameEvent @event)
        {
            await SendFrame(new Events.SubmitGameEvents(@event, Game, User));
        }

        protected override Task ReceivedFrame(EventBase @event)
        {
            return Task.CompletedTask;
        }
    }
}