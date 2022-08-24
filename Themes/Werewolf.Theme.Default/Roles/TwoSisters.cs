namespace Werewolf.Theme.Default.Roles;

[Docs.Role]
public class TwoSisters : VillagerBase
{
    public TwoSisters(Theme theme) : base(theme)
    {
    }

    public bool HasSeenPartner { get; set; }

    public override Role ViewRole(Role viewer)
    {
        if (viewer is TwoSisters && HasSeenPartner)
            return this;
        return base.ViewRole(viewer);
    }

    public override Role CreateNew()
        => new TwoSisters(Theme);
}
