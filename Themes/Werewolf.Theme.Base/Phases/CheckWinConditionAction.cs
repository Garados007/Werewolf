namespace Werewolf.Theme.Phases;

public class CheckWinConditionAction : AsyncActionPhaseBase
{
    public override async Task ExecuteAsync(GameRoom game)
    {
        if (new WinCondition().Check(game, out ReadOnlyMemory<Role>? winner))
        {
            await game.StopGameAsync(winner);
        }
    }
}
