namespace Werewolf.Theme.Default.Roles;

public class PureSoul : VillagerBase
{
    public PureSoul(GameMode theme) : base(theme)
    {
    }

    public override Character ViewRole(Character viewer)
    {
        return this;
    }

    public override string Name => "pure-soul";

    public override Character CreateNew()
        => new PureSoul(Theme);
}
