using System.Text.Json;
using Werewolf.User;

namespace Werewolf.Theme
{
    /// <summary>
    /// The base class for all event messages that can be sent to the user
    /// </summary>
    public abstract class GameEvent
    {
        /// <summary>
        /// Get the log message for this event message. If this event should be logged in the 
        /// message logs of the user the result of this method is null.
        /// </summary>
        /// <returns>The message to log.</returns>
        public virtual Chats.ChatServiceMessage? GetLogMessage()
            => null;

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