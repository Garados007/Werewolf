using System.Text.Json;
using Werewolf.Theme.Chats;
using Werewolf.User;

namespace Werewolf.Theme.Events
{
    public class AddParticipant : GameEvent
    {
        public UserInfo User { get; }

        public AddParticipant(UserInfo user)
            => User = user;

        public override ChatServiceMessage? GetLogMessage()
            => new Chats.AddParticipantLog(User.Id);

        public override bool CanSendTo(GameRoom game, UserInfo user)
            => true;

        public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
        {
            User.WriteContent(writer);
        }
    }
}