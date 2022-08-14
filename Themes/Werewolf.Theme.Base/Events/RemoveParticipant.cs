using Werewolf.User;
using System.Text.Json;
using Werewolf.Theme.Chats;

namespace Werewolf.Theme.Events
{
    public class RemoveParticipant : GameEvent
    {
        public UserId UserId { get; }

        public RemoveParticipant(UserId userId)
            => UserId = userId;

        public override ChatServiceMessage? GetLogMessage()
            => new Chats.RemoveParticipantLog(UserId);

        public override bool CanSendTo(GameRoom game, UserInfo user)
            => true;

        public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
        {
            writer.WriteString("id", UserId);
        }
    }
}