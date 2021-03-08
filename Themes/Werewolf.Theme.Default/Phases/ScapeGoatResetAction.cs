using System.Linq;
using Werewolf.Theme.Phases;

namespace Werewolf.Theme.Default.Phases
{
    public class ScapeGoatResetAction : ActionPhaseBase
    {
        public override void Execute(GameRoom game)
        {
            foreach (var role in game.Users.Select(x => x.Value.Role))
            {
                if (role is not BaseRole baseRole)
                    continue;
                baseRole.HasVotePermitFromScapeGoat = false;
                if (role is Roles.ScapeGoat scapeGoat && scapeGoat.WasKilledByVillage)
                {
                    // this phase is executed right after the scapegoat is killed.
                    // At this moment this role had never the chance to decide.
                    // therefore:
                    //  1) scape goat is killed but never decided
                    //     => HasDecided is toggled to true
                    //  2) scape goat has decided last time and now fullified its revenge
                    //     => HasRevenge is toggled to true
                    if (scapeGoat.HasDecided)
                        scapeGoat.HasRevenge = true;
                    else scapeGoat.HasDecided = true;
                }
            }
        }
    }
}
