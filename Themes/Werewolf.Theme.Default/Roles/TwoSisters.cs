﻿namespace Werewolf.Theme.Default.Roles;

public class TwoSisters : VillagerBase
{
    public TwoSisters(GameMode theme) : base(theme)
    {
    }

    public bool HasSeenPartner { get; set; }

    public override Role ViewRole(Role viewer)
    {
        return viewer is TwoSisters && HasSeenPartner ? this : base.ViewRole(viewer);
    }

    public override string Name => "TwoSisters";

    public override Role CreateNew()
        => new TwoSisters(Theme);
}
