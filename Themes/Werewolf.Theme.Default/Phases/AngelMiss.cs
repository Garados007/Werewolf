namespace Werewolf.Theme.Default.Phases
{
    public class AngelMiss : Werewolf.Theme.Phases.ActionPhaseBase
    {
        public override void Execute(GameRoom game)
        {
            foreach (var role in game.Participants.Values)
                if (role is Roles.Angel angel)
                    angel.MissedFirstRound = true;
        }
    }
}
