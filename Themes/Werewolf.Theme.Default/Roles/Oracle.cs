namespace Werewolf.Theme.Default.Roles;

public class Oracle : VillagerBase
{
    public Oracle(GameMode theme) : base(theme)
    {
    }

    public override string Name => "alte Seherin";

    public override Character CreateNew()
    {
        return new Oracle(Theme);
    }

    public override Character ViewRole(Character viewer)
    {
        return viewer is Oracle
            ? this
            : base.ViewRole(viewer);
    }
}
