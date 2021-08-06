using System.Text.Json;
using Werewolf.User;

namespace Werewolf.Theme.Events
{
    public class NextPhase : GameEvent
    {
        public Phase? Phase { get; }

        public NextPhase(Phase? phase)
            => Phase = phase;

        public override bool CanSendTo(GameRoom game, UserInfo user)
            => true;

        public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
        {
            if (Phase == null)
                writer.WriteNull("phase");
            else
            {
                writer.WriteStartObject("phase"); // phase
                writer.WriteString("lang-id", Phase.LanguageId);
                writer.WriteEndObject();
            }
        }
    }
}