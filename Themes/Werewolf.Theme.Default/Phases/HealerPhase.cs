using Werewolf.User;
using Werewolf.Theme.Phases;
using Werewolf.Theme.Votings;

namespace Werewolf.Theme.Default.Phases;

public class HealerPhase : SingleVotingPhase<HealerPhase.HealerVote>, INightPhase<HealerPhase>
{
    public class HealerVote : PlayerVotingBase
    {
        public HealerVote(GameRoom game, IEnumerable<UserId>? participants = null)
            : base(game, participants ?? GetDefaultParticipants(game,
                role => role.Enabled
                    && role is BaseRole baseRole
                    && !baseRole.IsSelectedByHealer
            ))
        {
        }

        public override bool CanView(Character viewer)
            => viewer is Roles.Healer;

        protected override bool CanVoteBase(Character voter)
            => voter is Roles.Healer && voter.Enabled;

        public override void Execute(GameRoom game, UserId id, Character role)
        {
            foreach (var other in game.Users.Select(x => x.Value.Role))
                if (other is BaseRole otherBase)
                    otherBase.IsSelectedByHealer = false;
            if (role is BaseRole baseRole)
                baseRole.IsSelectedByHealer = true;
        }
    }

    public override bool CanExecute(GameRoom game)
    {
        return game.AliveRoles.Where(x => x is Roles.Healer).Any() &&
            !game.Users
                .Select(x => x.Value.Role)
                .Where(x => x is Roles.OldMan oldMan && oldMan.WasKilledByVillager)
                .Any();
    }

    protected override HealerVote Create(GameRoom game, IEnumerable<UserId>? ids = null)
        => new(game, ids);

    public override bool CanMessage(GameRoom game, Character role)
    {
        return role is Roles.Healer;
    }
}
