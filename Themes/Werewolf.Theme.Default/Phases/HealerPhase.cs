using Werewolf.Users.Api;
using Werewolf.Theme.Phases;
using Werewolf.Theme.Votings;
using System.Collections.Generic;
using System.Linq;

namespace Werewolf.Theme.Default.Phases
{
    public class HealerPhase : SingleVotingPhase<HealerPhase.HealerVote>, INightPhase<HealerPhase>
    {
        public class HealerVote : PlayerVotingBase
        {
            public HealerVote(GameRoom game, IEnumerable<UserId>? participants = null)
                : base(game, participants)
            {
            }

            protected override bool DefaultParticipantSelector(Role role)
                => role.IsAlive && role is BaseRole baseRole && !baseRole.IsSelectedByHealer;

            public override bool CanView(Role viewer)
                => viewer is Roles.Healer;

            public override bool CanVote(Role voter)
                => voter is Roles.Healer && voter.IsAlive;

            public override void Execute(GameRoom game, UserId id, Role role)
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
            => new HealerVote(game, ids);

        public override bool CanMessage(GameRoom game, Role role)
        {
            return role is Roles.Healer;
        }
    }
}
