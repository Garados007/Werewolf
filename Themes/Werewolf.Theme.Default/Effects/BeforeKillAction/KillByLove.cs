using Werewolf.Theme.Effects;

namespace Werewolf.Theme.Default.Effects.BeforeKillAction;

public class KillByLove : BeforeKillActionEffect
{
    public override void Execute(GameRoom game, Role current)
    {
        foreach (var role in game.Users.Select(x => x.Value.Role))
            if (role is BaseRole baseRole && role != current && baseRole.IsLoved)
                role.AddKillFlag(new KillInfos.KilledByLove());
    }
}