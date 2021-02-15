namespace Werewolf.Theme.Phases
{
    public class KillTransitionToAboutToKillAction : ActionPhaseBase
    {
        public override void Execute(GameRoom game)
        {
            foreach (var role in game.Participants.Values)
                if (role != null && role.KillState == KillState.MarkedKill)
                {
                    role.ChangeToAboutToKill(game);
                }
        }
    }
}
