namespace Werewolf.Theme.Default.Roles;

public class Hunter : VillagerBase
{
    public Hunter(GameMode theme) : base(theme)
    {
    }

    public bool HasKilled { get; set; }

    public override string Name => "Jäger";

    public override Character CreateNew()
    {
        return new Hunter(Theme);
    }

    public override Character ViewRole(Character viewer)
    {
        return viewer is Hunter
            ? this
            : base.ViewRole(viewer);
    }
}
