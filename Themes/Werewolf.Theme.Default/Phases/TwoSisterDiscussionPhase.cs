using Werewolf.Theme.Default.Roles;
using Werewolf.Theme.Phases;

namespace Werewolf.Theme.Default.Phases;

public class TwoSisterDiscussionPhase : DiscussionPhase
{
    public override bool CanExecute(GameRoom game)
    {
        return game.Users.Any(x => x.Value.Role is OldMan oldman && oldman.WasKilledByVillager) ? false : base.CanExecute(game);
    }

    protected override void Init(GameRoom game)
    {
        base.Init(game);
        foreach (TwoSisters user in game.AliveRoles.Where(x => x is TwoSisters))
            user.HasSeenPartner = true;
    }

    public override bool CanMessage(GameRoom game, Character role)
        => CanVote(role);

    protected override bool CanView(Character viewer)
    {
        return viewer is Roles.TwoSisters;
    }

    protected override bool CanVote(Character voter)
    {
        return voter is Roles.TwoSisters && voter.Enabled;
    }
}
