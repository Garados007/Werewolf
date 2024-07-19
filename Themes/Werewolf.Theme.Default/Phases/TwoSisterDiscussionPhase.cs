using System.Linq;
using Werewolf.Theme.Default.Roles;
using Werewolf.Theme.Phases;

namespace Werewolf.Theme.Default.Phases
{
    public class TwoSisterDiscussionPhase : DiscussionPhase
    {
        public override bool CanExecute(GameRoom game)
        {
            if (game.Users.Any(x => x.Value.Role is OldMan oldman && oldman.WasKilledByVillager))
                return false;
            return base.CanExecute(game);
        }

        protected override void Init(GameRoom game)
        {
            base.Init(game);
            foreach (TwoSisters user in game.AliveRoles.Where(x => x is TwoSisters))
                user.HasSeenPartner = true;
        }

        public override bool CanMessage(GameRoom game, Role role)
            => CanVote(role);

        protected override bool CanView(Role viewer)
        {
            return viewer is Roles.TwoSisters;
        }

        protected override bool CanVote(Role voter)
        {
            return voter is Roles.TwoSisters && voter.Enabled;
        }
    }
}
