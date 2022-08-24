namespace Werewolf.Theme.Default.Roles;

[Docs.Role]
public class Villager : VillagerBase
{
    public Villager(Theme theme) : base(theme)
    {
    }

    public override Role CreateNew()
    {
        return new Villager(Theme);
    }
}
