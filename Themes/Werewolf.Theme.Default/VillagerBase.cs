﻿namespace Werewolf.Theme.Default;

public abstract class VillagerBase : BaseRole
{
    protected VillagerBase(GameMode theme) : base(theme)
    {
    }

    public override bool? IsSameFaction(Role other)
    {
        return other is VillagerBase
            ? true
            : (bool?)null;
    }
}
