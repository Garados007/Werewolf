using System.Linq;
using Werewolf.Theme.Default.Roles;
using Werewolf.Theme.Phases;

namespace Werewolf.Theme.Default.Phases
{
    public class ThreeBrotherDiscussionPhase : DiscussionPhase
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
            foreach (ThreeBrothers user in game.AliveRoles.Where(x => x is ThreeBrothers))
                user.HasSeenPartner = true;
        }
        
        public override bool CanMessage(GameRoom game, Role role)
            => CanVote(role);

        protected override bool CanView(Role viewer)
        {
            return viewer is Roles.ThreeBrothers;
        }

        protected override bool CanVote(Role voter)
        {
            return voter is Roles.ThreeBrothers && voter.Enabled;
        }
    }
}
