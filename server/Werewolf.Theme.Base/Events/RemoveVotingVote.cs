using System.Text.Json;
using Werewolf.Theme.Chats;
using Werewolf.User;

namespace Werewolf.Theme.Events;

public class RemoveVotingVote : GameEvent
{
    public RemoveVotingVote(Voting voting, int option, UserId voter)
    {
        Voting = voting;
        Option = option;
        Voter = voter;
    }

    public Voting Voting { get; }

    public int Option { get; }

    public UserId Voter { get; }

    public override ChatServiceMessage? GetLogMessage()
        => new RemoveVotingVoteLog(this);

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
        writer.WriteString("voter", Voter);
    }
}
