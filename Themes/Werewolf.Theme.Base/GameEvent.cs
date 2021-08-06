using System.Text.Json;
using Werewolf.User;

namespace Werewolf.Theme
{
    public abstract class GameEvent
    {
        public abstract bool CanSendTo(GameRoom game, UserInfo user);

        public virtual string GameEventType
            => GetType().Name;

        public void Write(Utf8JsonWriter writer, GameRoom game, UserInfo user)
        {
            writer.WriteStartObject();
            writer.WriteString("$type", GameEventType);
            WriteContent(writer, game, user);
            writer.WriteEndObject();
        }

        public abstract void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user);
    }
}