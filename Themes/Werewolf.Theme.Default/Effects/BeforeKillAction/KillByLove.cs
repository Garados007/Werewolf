using Werewolf.Theme.Effects;

namespace Werewolf.Theme.Default.Effects.BeforeKillAction;

public class KillByLove : BeforeKillActionEffect
{
    public override void Execute(GameRoom game, Character current)
    {
        var ownTargets = current.Effects.GetEffects<LovedEffect>()
            .Select(x => x.Target)
            .ToArray();
        foreach (var role in game.AliveRoles)
        {
            var effect = role.Effects.GetEffect<LovedEffect>(x => x.Target == current);
            if (effect is null || !ownTargets.Contains(role))
                continue;
            role.AddKillFlag(new KillInfos.KilledByLove());
        }
    }
}
