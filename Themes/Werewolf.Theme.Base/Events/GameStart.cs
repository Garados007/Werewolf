using System.Text.Json;
using Werewolf.Users.Api;

namespace Werewolf.Theme.Events
{
    public class GameStart : GameEvent
    {
        public override bool CanSendTo(GameRoom game, UserInfo user)
            => true;

        public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
        {
            var ownRole = game.TryGetRole(user.Id);
            writer.WriteStartObject("participants");
            foreach (var (id, participant) in game.Users.ToArray())
            {
                if (participant.Role == null)
                    writer.WriteNull(id.ToId());
                else
                {
                    var seenRole = Role.GetSeenRole(game, null, user,
                        id, participant.Role);

                    writer.WriteStartObject(id.ToId());
                    writer.WriteStartArray("tags");
                    foreach (var tag in Role.GetSeenTags(game, user, ownRole, participant.Role))
                        writer.WriteStringValue(tag);
                    writer.WriteEndArray();
                    writer.WriteString("role", seenRole?.GetType().Name);
                    writer.WriteEndObject();
                }
            }
            writer.WriteEndObject();
        }
    }
}
