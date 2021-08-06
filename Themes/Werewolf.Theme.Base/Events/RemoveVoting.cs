using System.Text.Json;
using Werewolf.User;

namespace Werewolf.Theme.Events
{
    public class RemoveVoting : GameEvent
    {
        public ulong Id { get; }

        public RemoveVoting(ulong id)
            => Id = id;

        public override bool CanSendTo(GameRoom game, UserInfo user)
            => true;

        private static readonly System.Globalization.NumberFormatInfo format =
            System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

        public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
        {
            writer.WriteString("id", Id.ToString(format));
        }
    }
}
