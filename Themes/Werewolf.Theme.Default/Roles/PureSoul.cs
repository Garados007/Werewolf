namespace Werewolf.Theme.Default.Roles;

[Docs.Role]
public class PureSoul : VillagerBase
{
    public PureSoul(Theme theme) : base(theme)
    {
    }

    public override Role ViewRole(Role viewer)
    {
        return this;
    }

    public override Role CreateNew()
        => new PureSoul(Theme);
}
