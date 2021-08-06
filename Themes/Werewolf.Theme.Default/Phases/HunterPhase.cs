using Werewolf.User;
using Werewolf.Theme.Phases;
using Werewolf.Theme.Default.Roles;
using Werewolf.Theme.Votings;
using System.Collections.Generic;
using System.Linq;

namespace Werewolf.Theme.Default.Phases
{
    public class HunterPhase : SeperateVotingPhase<HunterPhase.HunterKill, Hunter>
    {
        public class HunterKill : PlayerVotingBase
        {
            public Hunter Hunter { get; }

            public HunterKill(GameRoom game, Hunter hunter, IEnumerable<UserId>? participants = null)
                : base(game, participants)
            {
                Hunter = hunter;
            }

            public override bool CanView(Role viewer)
            {
                return viewer == Hunter;
            }

            public override bool CanVote(Role voter)
            {
                return voter == Hunter;
            }

            public override void Execute(GameRoom game, UserId id, Role role)
            {
                role.SetKill(game, new KillInfos.KilledByHunter());
                Hunter.HasKilled = true;
            }
        }

        public override bool CanExecute(GameRoom game)
        {
            return base.CanExecute(game) &&
                !game.Users
                    .Select(x => x.Value.Role)
                    .Where(x => x is OldMan oldMan && oldMan.WasKilledByVillager)
                    .Any();
        }

        protected override HunterKill Create(Hunter role, GameRoom game, IEnumerable<UserId>? ids = null)
            => new HunterKill(game, role, ids);

        protected override Hunter GetRole(HunterKill voting)
            => voting.Hunter;

        protected override bool FilterVoter(Hunter role)
            => !role.IsAlive && !role.HasKilled;

        public override bool CanMessage(GameRoom game, Role role)
        {
            return true;
        }
    }
}
