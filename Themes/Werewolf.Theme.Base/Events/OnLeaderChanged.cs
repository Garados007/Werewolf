using Werewolf.User;
using System.Text.Json;

namespace Werewolf.Theme.Events
{
    public class OnLeaderChanged : GameEvent
    {
        public UserId Leader { get; }

        public OnLeaderChanged(UserId leader)
            => Leader = leader;

        public override bool CanSendTo(GameRoom game, UserInfo user)
            => true;

        public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
        {
            writer.WriteString("leader", Leader);
        }
    }
}