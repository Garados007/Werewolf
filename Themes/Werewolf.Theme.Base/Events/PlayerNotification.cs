using Werewolf.Users.Api;
using System;
using System.Text.Json;

namespace Werewolf.Theme.Events
{
    public class PlayerNotification : GameEvent
    {
        public string NotificationId { get; }

        public ReadOnlyMemory<UserId> Player { get; }

        public PlayerNotification(string notificationId, ReadOnlyMemory<UserId> player)
            => (NotificationId, Player) = (notificationId, player);

        public override bool CanSendTo(GameRoom game, UserInfo user)
            => true;

        public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
        {
            writer.WriteString("text-id", NotificationId);
            writer.WriteStartArray("player");
            foreach (var id in Player.Span)
                writer.WriteStringValue(id.ToId());
            writer.WriteEndArray();
        }
    }
}
