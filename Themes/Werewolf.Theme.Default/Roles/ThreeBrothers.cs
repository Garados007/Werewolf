namespace Werewolf.Theme.Default.Roles;

[Docs.Role]
public class ThreeBrothers : VillagerBase
{
    public ThreeBrothers(Theme theme) : base(theme)
    {
    }

    public bool HasSeenPartner { get; set; }

    public override Role ViewRole(Role viewer)
    {
        if (viewer is ThreeBrothers && HasSeenPartner)
            return this;
        return base.ViewRole(viewer);
    }
    
    public override Role CreateNew()
        => new ThreeBrothers(Theme);
}
