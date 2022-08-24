﻿namespace Werewolf.Theme.Default.Roles;

[Docs.Role]
public class Idiot : VillagerBase
{
    private bool isRevealed;
    public bool IsRevealed
    {
        get => isRevealed;
        set
        {
            isRevealed = value;
            SendRoleInfoChanged();
        }
    }

    public bool WasMajor { get; set; }

    public Idiot(Theme theme) : base(theme)
    {
    }

    public override Role ViewRole(Role viewer)
    {
        return IsRevealed
            ? this
            : base.ViewRole(viewer);
    }

    public override Role CreateNew()
    {
        return new Idiot(Theme);
    }
}
