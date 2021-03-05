using Werewolf.Users.Api;
using System.Text.Json;

namespace Werewolf.Theme.Events
{
    public class SetVotingVote : GameEvent
    {
        public SetVotingVote(Voting voting, int option, UserId voter)
        {
            Voting = voting;
            Option = option;
            Voter = voter;
        }

        public Voting Voting { get; }

        public int Option { get; }

        public UserId Voter { get; }

        public override bool CanSendTo(GameRoom game, UserInfo user)
        {
            return Voting.CanViewVoting(game, user, game.TryGetRole(user.Id), Voting);
        }

        private static readonly System.Globalization.NumberFormatInfo format =
            System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

        public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
        {
            writer.WriteString("voting", Voting.Id.ToString(format));
            writer.WriteString("option", Option.ToString(format));
            writer.WriteString("voter", Voter.ToString());
        }
    }
}
