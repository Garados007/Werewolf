namespace Werewolf.Theme.Phases;

public class KillTransitionToKilledAction : AsyncActionPhaseBase
{
    public override async Task ExecuteAsync(GameRoom game)
    {
        foreach (var (id, entry) in game.Users)
            if (entry.Role != null && entry.Role.HasKillFlag)
            {
                entry.Role.ChangeToKilled();
                if (game.DeadCanSeeAllRoles)
                    foreach (var otherRole in game.Users.Select(x => x.Value.Role))
                        if (otherRole != null)
                            game.SendEvent(new Events.OnRoleInfoChanged(otherRole, target: id));
            }
        if (new WinCondition().Check(game, out ReadOnlyMemory<Role>? winner))
        {
            await game.StopGameAsync(winner);
        }
    }
}
