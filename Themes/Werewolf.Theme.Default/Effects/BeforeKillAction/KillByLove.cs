using Werewolf.Theme.Effects;

namespace Werewolf.Theme.Default.Effects.BeforeKillAction;

public class KillByLove : BeforeKillActionEffect
{
    public override void Execute(GameRoom game, Role current)
    {
        var ownTarget = current.Effects.GetEffect<LovedEffect>()?.Target;
        foreach (var role in game.AliveRoles)
        {
            var effect = role.Effects.GetEffect<LovedEffect>();
            if (effect is null || effect.Target != current || ownTarget != role)
                continue;
            role.AddKillFlag(new KillInfos.KilledByLove());
        }
    }
}