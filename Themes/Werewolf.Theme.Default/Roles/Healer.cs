namespace Werewolf.Theme.Default.Roles;

public class Healer : VillagerBase
{
    public Healer(GameMode theme) : base(theme)
    {
    }

    public override string Name => "Heiler";

    public override Character CreateNew()
    {
        return new Healer(Theme);
    }
}
