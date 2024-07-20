namespace Werewolf.Theme.Labels;

public interface IVotingLabel : ILabel
{
    void OnAttachVoting(GameRoom game, Voting target);

    void OnDetachVoting(GameRoom game, Voting target);
}
