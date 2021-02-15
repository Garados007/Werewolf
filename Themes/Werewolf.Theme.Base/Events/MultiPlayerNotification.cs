using Werewolf.Users.Api;
using System.Collections.Generic;
using System.Text.Json;

namespace Werewolf.Theme.Events
{
    public class MultiPlayerNotification : GameEvent
    {
        public Dictionary<string, HashSet<UserId>> Notifications { get; }

        public MultiPlayerNotification(Dictionary<string, HashSet<UserId>> notifications)
            => Notifications = notifications;

        public override bool CanSendTo(GameRoom game, UserInfo user)
            => true;

        public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
        {
            writer.WriteStartObject("notifications");
            foreach (var (key, players) in Notifications)
            {
                writer.WriteStartArray(key);
                foreach (var player in players)
                    writer.WriteStringValue(player.ToString());
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
        }
    }
}
