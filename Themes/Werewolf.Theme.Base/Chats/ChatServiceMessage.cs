using System.Text.Json;
using Werewolf.User;

namespace Werewolf.Theme.Chats;

/// <summary>
/// A system message that can be sent to every participant in the current game room. The content are
/// autogenerated using the language system.
/// </summary>
public abstract class ChatServiceMessage : GameEvent
{
    /// <summary>
    /// If this is set to true this will create a banner message. If this is set to false this will
    /// only appear in the chat log.
    /// </summary>
    public abstract bool Epic { get; }

    public sealed override string GameEventType => nameof(ChatServiceMessage);

    public virtual string MessageKey => GetType().FullName
        ?? throw new NotSupportedException("unsupported message key, override this member to get a meaningfull key");

    public abstract IEnumerable<(string key, ChatVariable value)> GetArgs();

    public sealed override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
    {
        writer.WriteString("key", MessageKey);
        writer.WriteBoolean("epic", Epic);
        writer.WriteStartObject("args");
        foreach (var (key, value) in GetArgs())
        {
            writer.WriteStartArray(key);
            writer.WriteStringValue(value.Type.ToString());
            writer.WriteStringValue(value.Text);
            if (value.Data.Length > 0)
            {
                writer.WriteStartArray();
                foreach (var x in value.Data.Span)
                    writer.WriteStringValue(x);
                writer.WriteEndArray();
            }
            if (value.Args is not null)
            {
                writer.WriteStartObject();
                foreach (var (k, v) in value.Args)
                    writer.WriteString(k, v);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
        writer.WriteEndObject();
    }
}
