using System.Text.Json;
using Werewolf.Users.Api;

namespace Werewolf.Theme.Events
{
    public class SetVotingTimeout : GameEvent
    {
        public Voting Voting { get; }

        public SetVotingTimeout(Voting voting)
            => Voting = voting;

        public override bool CanSendTo(GameRoom game, UserInfo user)
        {
            return Voting.CanViewVoting(game, user, game.TryGetRole(user.Id), Voting);
        }

        public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
        {
            writer.WriteString("id", Voting.Id.ToString());
            if (Voting.Timeout == null)
                writer.WriteNull("timeout");
            else writer.WriteString("timeout", Voting.Timeout.Value);
        }
    }
}
