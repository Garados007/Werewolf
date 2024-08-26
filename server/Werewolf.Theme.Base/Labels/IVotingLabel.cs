namespace Werewolf.Theme.Labels;

public interface IVotingLabel : ILabel
{
    void OnAttachVoting(GameRoom game, IVotingLabel label, Voting target);

    void OnDetachVoting(GameRoom game, IVotingLabel label, Voting target);
}
