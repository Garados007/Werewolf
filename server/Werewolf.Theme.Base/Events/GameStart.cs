using System.Text.Json;
using Werewolf.Theme.Chats;
using Werewolf.User;

namespace Werewolf.Theme.Events;

public class GameStart : GameEvent
{
    public override ChatServiceMessage? GetLogMessage()
        => new Chats.GameStartLog();

    public override bool CanSendTo(GameRoom game, UserInfo user)
        => true;

    public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
    {
        var ownRole = game.TryGetRole(user.Id);
        writer.WriteStartObject("participants");
        foreach (var (id, participant) in game.Users.ToArray())
        {
            if (participant.Character == null)
                writer.WriteNull(id);
            else
            {
                var seenRole = Character.GetSeenRole(game, null, user,
                    id, participant.Character);

                writer.WriteStartObject(id);
                writer.WriteStartArray("tags");
                foreach (var tag in Character.GetSeenTags(game, user, ownRole, participant.Character))
                    writer.WriteStringValue(tag);
                writer.WriteEndArray();
                writer.WriteString("role", seenRole is null ? null : game.Theme?.GetCharacterName(seenRole));
                writer.WriteBoolean("enabled", participant.Character.Enabled);
                writer.WriteEndObject();
            }
        }
        writer.WriteEndObject();
    }
}
