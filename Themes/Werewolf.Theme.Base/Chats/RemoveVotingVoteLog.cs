using Werewolf.User;

namespace Werewolf.Theme.Chats;

public class RemoveVotingVoteLog : ChatServiceMessage
{
    public Events.RemoveVotingVote Msg { get; }

    public RemoveVotingVoteLog(Events.RemoveVotingVote msg)
        => Msg = msg;

    public override bool Epic => false;

    public override bool CanSendTo(GameRoom game, UserInfo user)
        => Msg.CanSendTo(game, user);

    public override IEnumerable<(string key, ChatVariable value)> GetArgs()
    {
        yield return ("voting", Msg.Voting);
        yield return ("voter", Msg.Voter);
        foreach (var (id, option) in Msg.Voting.Options)
        {
            if (id != Msg.Option)
                continue;
            yield return ("option", (Msg.Voting, option));
            yield break;
        }
        yield return ("option", $"<{Msg.Option}>");
    }
}