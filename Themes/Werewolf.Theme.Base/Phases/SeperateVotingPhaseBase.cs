namespace Werewolf.Theme.Phases;

public abstract class SeperateVotingPhaseBase<TVoting, TRole> : Scene
    where TVoting : Voting
    where TRole : Character
{
    protected abstract TVoting Create(TRole role, GameRoom game);

    protected abstract TRole GetRole(TVoting voting);

    protected abstract bool FilterVoter(TRole role);

    public override bool CanExecute(GameRoom game)
    {
        return game.Users
            .Select(x => x.Value.Role)
            .Where(x => x is TRole role && FilterVoter(role))
            .Any();
    }

    protected override void Init(GameRoom game)
    {
        base.Init(game);
        foreach (var role in game.Users.Select(x => x.Value.Role))
            if (role is TRole trole && FilterVoter(trole))
                AddVoting(Create(trole, game));
    }
}
