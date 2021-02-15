using System.Text.Json;
using Werewolf.Users.Api;

namespace Werewolf.Theme.Events
{
    public class AddVoting : GameEvent
    {
        public Voting Voting { get; }

        public AddVoting(Voting voting)
            => Voting = voting;

        public override bool CanSendTo(GameRoom game, UserInfo user)
        {
            return Voting.CanViewVoting(game, user, game.TryGetRole(user.Id), Voting);
        }

        public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
        {
            writer.WritePropertyName("voting");
            Voting.WriteToJson(writer, game, user);
        }
    }
}
