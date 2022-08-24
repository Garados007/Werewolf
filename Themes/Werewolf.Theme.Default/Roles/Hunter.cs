namespace Werewolf.Theme.Default.Roles;

[Docs.Role]
public class Hunter : VillagerBase
{
    public Hunter(Theme theme) : base(theme)
    {
    }

    public bool HasKilled { get; set; }

    public override Role CreateNew()
    {
        return new Hunter(Theme);
    }

    public override Role ViewRole(Role viewer)
    {
        return viewer is Hunter
            ? this
            : base.ViewRole(viewer);
    }
}
