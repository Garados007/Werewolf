﻿using System.Linq;

namespace Werewolf.Theme.Default.Roles
{
    public class OldMan : VillagerBase
    {
        public bool WasKilledByWolvesOneTime { get; set; }

        public bool WasKilledByVillager { get; set; }

        public OldMan(Theme theme) : base(theme)
        {
            Effects.Add(new Default.Effects.BeforeKillAction.KillRevealedIdiots());
        }

        public override string Name => "Der Alte";

        public override Role CreateNew()
            => new OldMan(Theme);
    }
}
