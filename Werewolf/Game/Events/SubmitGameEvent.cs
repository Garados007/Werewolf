using System.Text.Json;
using MaxLib.WebServer.WebSocket;
using Werewolf.Theme;
using Werewolf.User;

namespace Werewolf.Game.Events
{
    public sealed class SubmitGameEvents : EventBase
    {
        public GameEvent GameEvent { get; }

        public GameRoom Room { get; }

        public UserInfo User { get; }

        public SubmitGameEvents(GameEvent gameEvent, GameRoom room, UserInfo user)
        {
            GameEvent = gameEvent;
            Room = room;
            User = user;
        }

        public override void WriteJson(Utf8JsonWriter writer)
        {
            GameEvent.Write(writer, Room, User);
        }

        protected override void WriteJsonContent(Utf8JsonWriter writer)
        {
        }

        public override void ReadJsonContent(JsonElement json)
        {
            throw new System.NotSupportedException();
        }
    }
}