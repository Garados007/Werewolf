using System.Text.Json;
using Werewolf.User;

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

        private static readonly System.Globalization.NumberFormatInfo format =
            System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

        public override void WriteContent(Utf8JsonWriter writer, GameRoom game, UserInfo user)
        {
            writer.WriteString("id", Voting.Id.ToString(format));
            if (Voting.Timeout == null)
                writer.WriteNull("timeout");
            else writer.WriteString("timeout", Voting.Timeout.Value);
        }
    }
}
