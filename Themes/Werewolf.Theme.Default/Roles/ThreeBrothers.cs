namespace Werewolf.Theme.Default.Roles;

public class ThreeBrothers : VillagerBase
{
    public ThreeBrothers(GameMode theme) : base(theme)
    {
    }

    public bool HasSeenPartner { get; set; }

    public override Role ViewRole(Role viewer)
    {
        return viewer is ThreeBrothers && HasSeenPartner ? this : base.ViewRole(viewer);
    }

    public override string Name => "ThreeBrothers";

    public override Role CreateNew()
        => new ThreeBrothers(Theme);
}
