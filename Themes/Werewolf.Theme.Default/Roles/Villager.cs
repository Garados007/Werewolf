namespace Werewolf.Theme.Default.Roles;

public class Villager : VillagerBase
{
    public Villager(GameMode theme) : base(theme)
    {
    }

    public override string Name => "Dorfbewohner";

    public override Character CreateNew()
    {
        return new Villager(Theme);
    }
}
