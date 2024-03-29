﻿using System.Linq;
using Werewolf.Theme.Phases;

namespace Werewolf.Theme.Default.Phases
{
    public class KillFlagWerwolfVictimAction : ActionPhaseBase
    {
        public override void Execute(GameRoom game)
        {
            foreach (var role in game.Users.Select(x => x.Value.Role))
            {
                if (role is not BaseRole baseRole ||
                    baseRole.Effects.GetEffect<Effects.KillInfos.KilledByWerwolf>() is null)
                    continue;
                if (baseRole.IsSelectedByHealer)
                {
                    role.RemoveKillFlag();
                    continue;
                }
                if (baseRole is Roles.OldMan oldMan && !oldMan.WasKilledByWolvesOneTime)
                {
                    oldMan.WasKilledByWolvesOneTime = true;
                    role.RemoveKillFlag();
                    continue;
                }
            }
        }
    }
}
