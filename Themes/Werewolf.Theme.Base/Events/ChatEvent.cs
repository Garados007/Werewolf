using Werewolf.Users.Api;
using System.Text.Json;

namespace Werewolf.Theme.Events
{
    public class ChatEvent : GameEvent
    {
        public UserId Sender { get; }

        public string? Phase { get; }

        public string Message { get; }

        public bool CanSend { get; }

        public ChatEvent(UserId sender, string? phase, string message, bool canSend)
            => (Sender, Phase, Message, CanSend) = (sender, phase, message, canSend);

        public override bool CanSendTo(GameRoom game, UserInfo user)
        {
            if (user.Id == Sender)
                return true;
            if (!CanSend || game.Phase?.Current.LanguageId != Phase)
                return false;
            if (game.Phase == null)
                return true;
            var role = game.TryGetRole(user.Id);
            return role != null && game.Phase.Current.CanMessage(game, role);
        }

        public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
        {
            writer.WriteString("sender", Sender.ToId());
            writer.WriteString("phase", Phase);
            writer.WriteString("message", Message);
            writer.WriteBoolean("can-send", CanSend);
        }
    }
}
