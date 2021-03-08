using System.Linq;

namespace Werewolf.Theme.Phases
{
    public class KillTransitionToAboutToKillAction : ActionPhaseBase
    {
        public override void Execute(GameRoom game)
        {
            foreach (var role in game.Users.Select(x => x.Value.Role))
                if (role != null && role.KillState == KillState.MarkedKill)
                {
                    role.ChangeToAboutToKill(game);
                }
        }
    }
}
