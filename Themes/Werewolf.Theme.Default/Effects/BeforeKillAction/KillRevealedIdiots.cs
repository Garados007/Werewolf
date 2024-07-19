using Werewolf.Theme.Default.Roles;
using Werewolf.Theme.Effects;

namespace Werewolf.Theme.Default.Effects.BeforeKillAction;

public class KillRevealedIdiots : BeforeKillActionEffect
{
    public override void Execute(GameRoom game, Role current)
    {
        var idiots = game.AliveRoles
            .Where(x => x is Idiot idiot && idiot.IsRevealed);
        foreach (var idiot in idiots)
            idiot.AddKillFlag(new Effects.KillInfos.OldManKillsIdiot());
    }
}
